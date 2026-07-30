using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using PaleRegentModV1.PaleRegentModV1Code.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Relics;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 【模具】体系（名词表 N#9 / 遗物 R#2）。
///
/// 规则：
/// 1. 带模具标记的牌（IsMould：国王佣卫 / 有翼佣卫 / 虚空化模）在本场战斗中
///    每被【消耗】1 次记 1 点；升级与未升级按牌类型聚合，天然合并计数。
/// 2. 战斗结束时，每种模具牌按（本场消耗数 × 1%）的概率获得对应的
///    "模具·［卡牌名］"遗物；若已拥有则延长剩余战斗场数 +1，不重复获得。
/// 3. 模具遗物 1 场战斗后失效（由各 Mould* 遗物自身管理剩余场数）。
///
/// ============ 本版做了什么（相对你手上那两份） ============
/// - MouldHelper2（原版）：直接 RelicCmd.Obtain，遗物凭空塞进背包，没有"掉落"表现。
/// - MouldHelper（你改的）：把遗物存进 PendingRelicRewards，但【没有任何地方消费它】，
///   所以结算完什么都不会发生——这就是"改得面目全非却没效果"的原因。
///
/// 本版把发放流程收敛成一条链，三级兜底，每级都有日志：
///     ① 已拥有同类模具遗物  → ExtendCombats(+1)，不重复发
///     ② 尝试塞进战斗房间的奖励列表（战后奖励界面里出现，这才是"掉落"）
///     ③ ②失败 → 直接发到背包（保底，至少不会丢东西）
///
/// ★ 关于②：STS2 的奖励 API（RelicReward 的类名与构造签名、CombatRoom 上
///   奖励集合的属性名）我无法在编译期确定，硬写死很容易编译不过或运行时报错。
///   所以这里走【反射】：能找到就用，找不到就退到③，并且会把它在
///   CombatRoom 上找到的所有奖励相关成员打印一次到日志。
///   你把那段 "[Mould][API]" 开头的日志贴给我，我就能把反射换成写死的直调。
///   同理，RelicCmd.Obtain 的重载也用反射调用，避免出现
///   "Obtain(relic, player) 还是 Obtain(relic, player, 0)" 这种编译期猜谜。
/// </summary>
public static class MouldHelper
{
    // =====================================================================
    //  配置
    // =====================================================================

    /// <summary>模具遗物持续场数（表格 R#2：1 场战斗后失效）。</summary>
    public const int RelicCombats = 1;

    /// <summary>每消耗 1 张的成功概率（百分比）。</summary>
    public const int ChancePerExhaustPercent = 1;

    /// <summary>
    /// 调试用：true 时只要本场消耗过该模具牌就必定掉落（概率视为 100%）。
    /// 1% × 消耗数 的真实概率在测试时几乎永远触发不了，
    /// 想验证"掉落流程通不通"就先打开这个，验证完记得关。
    /// </summary>
    public static bool DebugForceDrop = false;

    /// <summary>详细日志（计数、掷骰结果、走了哪条发放路径）。</summary>
    public static bool VerboseLog = true;

    // =====================================================================
    //  状态
    // =====================================================================

    /// <summary>本场战斗中各模具牌的消耗计数（key = 模具牌类型）。</summary>
    private static readonly Dictionary<Type, int> ExhaustCounts = new();

    /// <summary>
    /// 等待战后结算的遗物（保留旧 API，避免其他文件调用处编译失败）。
    /// 正常流程下 RollMouldRelics 会自己把它清空并发放，这个表通常是空的。
    /// </summary>
    private static readonly Dictionary<Player, List<RelicModel>> PendingRelicRewards = new();

    private static bool _apiDumped;

    // =====================================================================
    //  计数
    // =====================================================================

    /// <summary>记录一次模具牌消耗（由 MouldExhaustListener 调用）。</summary>
    public static void NoteExhaust(CardModel card)
    {
        if (card is not PaleRegentModV1Card { IsMould: true }) return;

        Type key = card.GetType();
        ExhaustCounts.TryGetValue(key, out int n);
        ExhaustCounts[key] = n + 1;

        if (VerboseLog)
            Godot.GD.Print($"[Mould] 计数 {key.Name} = {n + 1}");
    }

    /// <summary>战斗开始时清零计数（防上一场残留）。务必确认这个方法真的被调到了。</summary>
    public static void ResetCounts()
    {
        if (VerboseLog && ExhaustCounts.Count > 0)
            Godot.GD.Print($"[Mould] 清空上一场计数（{ExhaustCounts.Count} 种）");

        ExhaustCounts.Clear();
    }

    /// <summary>手动查看当前计数，排查"到底有没有在计数"用。</summary>
    public static void DumpCounts()
    {
        Godot.GD.Print($"[Mould] 当前计数种类数 = {ExhaustCounts.Count}");
        foreach (KeyValuePair<Type, int> pair in ExhaustCounts)
            Godot.GD.Print($"[Mould]   {pair.Key.Name} × {pair.Value}");
    }

    // =====================================================================
    //  战后结算
    // =====================================================================

    /// <summary>
    /// 战斗结束时结算模具遗物（由 PaleToken.AfterCombatEnd 调用）。
    /// 每种模具牌独立判定：概率 = 消耗数 × 1%。
    /// </summary>
    public static async Task RollMouldRelics(Player player, CombatRoom room)
    {
        if (player == null)
        {
            Godot.GD.PushWarning("[Mould] RollMouldRelics: player 为 null，跳过结算。");
            return;
        }

        if (VerboseLog) DumpCounts();

        // 先把要发的算出来再发，避免边遍历边改字典。
        List<Type> winners = new();

        foreach (KeyValuePair<Type, int> pair in ExhaustCounts)
        {
            int chance = Math.Min(100, pair.Value * ChancePerExhaustPercent);
            if (chance <= 0) continue;

            if (DebugForceDrop)
            {
                if (VerboseLog)
                    Godot.GD.Print($"[Mould] {pair.Key.Name}：DebugForceDrop 开启，强制掉落。");
                winners.Add(pair.Key);
                continue;
            }

            // 随机源用 Random.Shared：不走存档随机流，对地图 / 卡牌种子没有影响。
            int roll = Random.Shared.Next(100);

            if (VerboseLog)
                Godot.GD.Print($"[Mould] {pair.Key.Name}：需要 < {chance}，掷出 {roll}");

            if (roll < chance) winners.Add(pair.Key);
        }

        ExhaustCounts.Clear();

        foreach (Type mouldCardType in winners)
            await GrantMouldRelic(player, room, mouldCardType);
    }

    /// <summary>
    /// 取出并清除指定玩家等待结算的遗物（保留旧 API）。
    /// 正常流程用不到；如果你在别处自己写了奖励界面注入，可以用它取货。
    /// </summary>
    public static List<RelicModel> ConsumePendingRelicRewards(Player player)
    {
        if (!PendingRelicRewards.Remove(player, out List<RelicModel>? rewards))
            return new List<RelicModel>();

        return rewards;
    }

    // =====================================================================
    //  发放：三级兜底
    // =====================================================================

    private static async Task GrantMouldRelic(Player player, CombatRoom room, Type mouldCardType)
    {
        // ① 已拥有同类模具遗物 → 延长场数，不重复发。
        foreach (RelicModel owned in player.Relics)
        {
            if (owned is MouldRelic existing && existing.MouldCardType == mouldCardType)
            {
                existing.ExtendCombats(1);
                Godot.GD.Print($"[Mould] 已拥有 {mouldCardType.Name} 的模具遗物，延长 1 场。");
                return;
            }
        }

        RelicModel? relic = CreateMouldRelic(mouldCardType);
        if (relic == null)
        {
            Godot.GD.PushWarning($"[Mould] 找不到 {mouldCardType.Name} 对应的模具遗物，跳过。");
            return;
        }

        // ② 尝试作为"战后奖励"出现在结算界面（这才是掉落感）。
        if (TryAddAsCombatReward(room, relic))
        {
            Godot.GD.Print($"[Mould] {mouldCardType.Name} → 已加入战后奖励列表。");
            return;
        }

        // ③ 兜底：直接进背包。宁可表现差一点，也不要"抽中了却什么都没发生"。
        Godot.GD.Print($"[Mould] {mouldCardType.Name} → 奖励列表不可用，改为直接获得。");
        await ObtainRelicDirect(relic, player);
    }

    private static RelicModel? CreateMouldRelic(Type mouldCardType)
    {
        if (mouldCardType == typeof(KingsRetainer))
            return ModelDb.Relic<MouldKingsRetainer>().ToMutable();

        if (mouldCardType == typeof(WingedRetainerCard))
            return ModelDb.Relic<MouldWingedRetainer>().ToMutable();

        if (mouldCardType == typeof(VoidGivenMould))
            return ModelDb.Relic<MouldVoidGivenMould>().ToMutable();

        return null;
    }

    // =====================================================================
    //  反射：奖励列表
    // =====================================================================

    /// <summary>
    /// 试着把遗物包成一个 Reward 对象塞进战斗房间的奖励集合。
    /// 全程反射，找不到就返回 false 交给兜底，不会编译不过也不会崩。
    /// </summary>
    private static bool TryAddAsCombatReward(CombatRoom room, RelicModel relic)
    {
        if (room == null) return false;

        try
        {
            DumpRewardApiOnce(room);

            object? reward = CreateRelicRewardObject(relic);
            if (reward == null) return false;

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // 先找属性，再找字段；名字里带 Reward 且是列表的就试。
            foreach (PropertyInfo p in room.GetType().GetProperties(flags))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.Name.IndexOf("Reward", StringComparison.OrdinalIgnoreCase) < 0) continue;

                if (p.GetValue(room) is IList list && TryAddTo(list, reward, p.Name))
                    return true;
            }

            foreach (FieldInfo f in room.GetType().GetFields(flags))
            {
                if (f.Name.IndexOf("Reward", StringComparison.OrdinalIgnoreCase) < 0) continue;

                if (f.GetValue(room) is IList list && TryAddTo(list, reward, f.Name))
                    return true;
            }
        }
        catch (Exception e)
        {
            Godot.GD.PushWarning($"[Mould] 加入奖励列表时异常：{e.Message}");
        }

        return false;
    }

    private static bool TryAddTo(IList list, object reward, string memberName)
    {
        try
        {
            list.Add(reward);
            Godot.GD.Print($"[Mould][API] 成功写入奖励集合成员：{memberName}");
            return true;
        }
        catch (Exception e)
        {
            Godot.GD.Print($"[Mould][API] 成员 {memberName} 拒绝写入：{e.Message}");
            return false;
        }
    }

    /// <summary>反射构造一个 RelicReward。找不到类型或合适的构造器就返回 null。</summary>
    private static object? CreateRelicRewardObject(RelicModel relic)
    {
        Assembly gameAsm = typeof(RelicModel).Assembly;

        Type? rewardType =
            gameAsm.GetType("MegaCrit.Sts2.Core.Rewards.RelicReward") ??
            FindTypeByName(gameAsm, "RelicReward");

        if (rewardType == null)
        {
            Godot.GD.Print("[Mould][API] 未找到 RelicReward 类型，无法走奖励列表。");
            return null;
        }

        foreach (ConstructorInfo ctor in rewardType.GetConstructors())
        {
            ParameterInfo[] ps = ctor.GetParameters();

            // 单参数且能接收 RelicModel
            if (ps.Length == 1 && ps[0].ParameterType.IsInstanceOfType(relic))
            {
                Godot.GD.Print($"[Mould][API] 使用构造器 {rewardType.Name}({ps[0].ParameterType.Name})");
                return ctor.Invoke(new object[] { relic });
            }

            // 第一个参数是 RelicModel，其余都有默认值
            if (ps.Length > 1 && ps[0].ParameterType.IsInstanceOfType(relic))
            {
                bool restOptional = true;
                for (int i = 1; i < ps.Length; i++)
                {
                    if (!ps[i].IsOptional) { restOptional = false; break; }
                }

                if (!restOptional) continue;

                object?[] args = new object?[ps.Length];
                args[0] = relic;
                for (int i = 1; i < ps.Length; i++) args[i] = ps[i].DefaultValue;

                Godot.GD.Print($"[Mould][API] 使用带默认值的构造器 {rewardType.Name}(...)，参数 {ps.Length} 个");
                return ctor.Invoke(args);
            }
        }

        Godot.GD.Print($"[Mould][API] {rewardType.FullName} 没有可用的构造器，无法走奖励列表。");
        return null;
    }

    private static Type? FindTypeByName(Assembly asm, string simpleName)
    {
        try
        {
            foreach (Type t in asm.GetTypes())
            {
                if (t.Name == simpleName) return t;
            }
        }
        catch
        {
            // GetTypes 可能因为个别类型加载失败抛异常，忽略即可。
        }

        return null;
    }

    /// <summary>
    /// 把 CombatRoom 上所有跟奖励相关的成员打印一次。
    /// 这段日志（"[Mould][API]" 开头）就是把反射换成写死直调所需要的全部信息。
    /// </summary>
    private static void DumpRewardApiOnce(CombatRoom room)
    {
        if (_apiDumped) return;
        _apiDumped = true;

        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        Godot.GD.Print($"[Mould][API] ===== CombatRoom = {room.GetType().FullName} =====");

        foreach (PropertyInfo p in room.GetType().GetProperties(flags))
        {
            if (p.Name.IndexOf("Reward", StringComparison.OrdinalIgnoreCase) < 0) continue;
            Godot.GD.Print($"[Mould][API] 属性 {p.Name} : {p.PropertyType.FullName}");
        }

        foreach (FieldInfo f in room.GetType().GetFields(flags))
        {
            if (f.Name.IndexOf("Reward", StringComparison.OrdinalIgnoreCase) < 0) continue;
            Godot.GD.Print($"[Mould][API] 字段 {f.Name} : {f.FieldType.FullName}");
        }

        foreach (MethodInfo m in room.GetType().GetMethods(flags))
        {
            if (m.Name.IndexOf("Reward", StringComparison.OrdinalIgnoreCase) < 0) continue;
            Godot.GD.Print($"[Mould][API] 方法 {m.Name}({m.GetParameters().Length} 参)");
        }

        Type? rewardType = FindTypeByName(typeof(RelicModel).Assembly, "RelicReward");
        if (rewardType == null)
        {
            Godot.GD.Print("[Mould][API] 程序集里没有名为 RelicReward 的类型。");
        }
        else
        {
            Godot.GD.Print($"[Mould][API] RelicReward = {rewardType.FullName}");
            foreach (ConstructorInfo c in rewardType.GetConstructors())
            {
                string sig = string.Join(", ", Array.ConvertAll(c.GetParameters(),
                    x => $"{x.ParameterType.Name} {x.Name}{(x.IsOptional ? " = 默认" : "")}"));
                Godot.GD.Print($"[Mould][API]   ctor({sig})");
            }
        }

        Godot.GD.Print("[Mould][API] ==========================================");
    }

    // =====================================================================
    //  反射：直接获得遗物（兜底）
    // =====================================================================

    /// <summary>
    /// 反射调用 RelicCmd.Obtain。
    /// 用反射的理由：这个方法在不同版本里可能是 Obtain(relic, player) 也可能是
    /// Obtain(relic, player, int)，写死任何一个都可能编译不过。
    /// 反射会自动挑一个参数能对上的重载，并 await 它返回的 Task。
    /// </summary>
    private static async Task ObtainRelicDirect(RelicModel relic, Player player)
    {
        try
        {
            Assembly gameAsm = typeof(RelicModel).Assembly;

            Type? cmdType =
                gameAsm.GetType("MegaCrit.Sts2.Core.Commands.RelicCmd") ??
                FindTypeByName(gameAsm, "RelicCmd");

            if (cmdType == null)
            {
                Godot.GD.PushWarning("[Mould] 找不到 RelicCmd 类型，遗物发放失败。");
                return;
            }

            foreach (MethodInfo m in cmdType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != "Obtain") continue;

                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length < 2) continue;
                if (!ps[0].ParameterType.IsInstanceOfType(relic)) continue;
                if (!ps[1].ParameterType.IsInstanceOfType(player)) continue;

                object?[] args = new object?[ps.Length];
                args[0] = relic;
                args[1] = player;

                bool ok = true;
                for (int i = 2; i < ps.Length; i++)
                {
                    if (ps[i].IsOptional) { args[i] = ps[i].DefaultValue; continue; }
                    if (ps[i].ParameterType == typeof(int)) { args[i] = 0; continue; }
                    ok = false;
                    break;
                }
                if (!ok) continue;

                object? result = m.Invoke(null, args);
                if (result is Task task) await task;

                Godot.GD.Print($"[Mould] 已直接获得遗物（RelicCmd.Obtain，{ps.Length} 参）。");
                return;
            }

            Godot.GD.PushWarning("[Mould] RelicCmd 上没有可用的 Obtain 重载，遗物发放失败。");
        }
        catch (Exception e)
        {
            Godot.GD.PushWarning($"[Mould] 直接发放遗物异常：{e.Message}");
        }
    }
}
