using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Utils;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 【失心】【苍白】【纯粹】自定义卡牌特质的实现。
///
/// ============ 机制说明（对应设计文档，20260729 需求变更） ============
/// 【失心】（Lost）：
///   - 取消这张牌的灵魂耗能，把灵魂费 1:1 转换并入虚空费。
///     例：1灵魂0虚空 → 0灵魂1虚空；2灵魂2虚空 → 0灵魂4虚空。
///   - x统一计为x（x灵魂1虚空，转化为0灵魂x虚空)。优化前：对 X 费牌无效（X 费牌无法附加失心）。
///   - 自动获得【重放1】（BaseReplayCount = 1，打出后额外重放一次）。
///   - 与【苍白】互斥：附加失心会移除苍白。
/// 【苍白】（Pale）：（20260729 变更：不再是"同时添加消耗和虚无"）
///   1. 取消【失心】，以及受失心影响而添加的"灵魂并入虚空消费"和【消耗】。
///      注意：如果这张牌没有失心就有【消耗】（自带/其他来源），不移除消耗。
///   2. 取消卡牌的虚空消耗（移除虚空费条目）。
///   3. 添加【虚无】。
/// 【纯粹】（Pure）：（20260729 变更：从"免疫感染转化"扩展为通用规则）
///   - 战斗中无法被变化，例如被变化为其他牌。
///     （免疫"感染"转化实现为不会被随机选中，见 Infection.OnTurnEndInHand 的过滤；
///      通用"无法被变化"由 Patches/PureTransformGuard 拦截 CardCmd.Transform 实现。）
///
/// ============ 实现思路（给未来的你/维护者） ============
/// STS2 原版的 CardKeyword 是枚举，mod 无法往里加新枚举值，
/// 所以"失心/苍白"不是真正的 CardKeyword，而是：
///   1. 用 RitsuLib 的 AttachedState 给"卡牌实例"挂一份附加数据（TraitState），
///      记录：是否失心 / 是否苍白 / 附加前的原始灵魂费和虚空费（用于还原）。
///      AttachedState 类似 C# 的 ConditionalWeakTable，数据跟着卡牌实例走，
///      卡牌实例销毁后数据自动回收，不会内存泄漏。
///   2. 费用修改：
///      - 灵魂费用 CardModel.EnergyCost.SetCustomBaseCost(int)（永久基础费）。
///      - 虚空费用 RitsuLib 的 card.SecondaryCosts().Set(VoidResource.Id, n)。
///   3. 关键词（重放/虚无/消耗）走原版 API：BaseReplayCount、AddKeyword/RemoveKeyword。
///   4. 牌面显示与 HoverTips（20260729 新增）：
///      失心/苍白会以"[gold]失心[/gold]。/[gold]苍白[/gold]。"的形式显示在牌面描述里，
///      并在悬停词条中追加失心/苍白词条（哪怕消耗/虚无/重放本身也会显示）。
///      实现见 Patches/TraitCardTextPatch（Harmony patch CardModel.GetDescriptionForPile
///      与 CardModel.HoverTips getter）。
///
/// ============ 修改指南 ============
/// - 想改"失心的灵魂→虚空换算比例"：改 ApplyLost 里的 newVoidCost 计算。
/// - 想改"失心送的重放层数"：改 LostReplayCount 常量。
/// - 想改牌面显示文字/词条：见 Patches/TraitCardTextPatch 与 static_hover_tips.json。
/// </summary>
public static class CardTraits
{
    /// <summary>失心自动附带的重放层数（重放1）。</summary>
    private const int LostReplayCount = 1;

    /// <summary>挂在卡牌实例上的特质数据。</summary>
    private sealed class TraitState
    {
        public bool IsLost;              // 是否有【失心】
        public bool IsPale;              // 是否有【苍白】
        public bool IsPureApplied;       // 是否被战斗中附加了【纯粹】（灵魂护佑等）
        public int OriginalEnergyCost;   // 附加特质前的灵魂费（用于苍白还原）
        public int OriginalVoidCost;     // 附加特质前的虚空费（用于还原）
        public bool OriginalHasVoidCost; // 附加特质前是否登记过虚空费条目（区分“虚空0”与“无虚空费”）//20260726
        public bool OriginalVoidCostsX;  // 附加特质前虚空费是否为 X 费（苍白还原时保留 X 属性）//20260726
        public bool CostSnapshotTaken;   // 是否已经记录过原始费用快照
        public bool ExhaustAddedByLost;  // 【消耗】是否是失心流程加上去的（20260729：苍白只移除失心加的消耗）
        public bool OriginalEnergyCostsX; // 附加特质前灵魂费是否为 X 费 //20260801
        public bool LostConvertedToVoidX; // 失心是否把这张牌转成了“0灵魂 / X虚空” //20260801
        public int LastVoidSpent;         // 最近一次打出时实际支付的虚空量（失心X牌的 X 取值）//20260801
        public bool VoidCostClearedByPale; // 虚空费是否已被苍白清除（用于抵御 Permanent 层回灌）//20260801
    }

    /// <summary>卡牌实例 → 特质数据 的弱引用表。</summary>
    private static readonly AttachedState<CardModel, TraitState> States =
        new(() => new TraitState());

    // ---------------- 查询 ----------------

    /// <summary>这张牌当前是否有【失心】。</summary>
    public static bool IsLost(CardModel card) =>
        States.TryGetValue(card, out TraitState? s) && s!.IsLost;

    /// <summary>这张牌当前是否有【苍白】。</summary>
    public static bool IsPale(CardModel card) =>
        States.TryGetValue(card, out TraitState? s) && s!.IsPale;

    /// <summary>
    /// 这张牌是否处于“失心，且失心把它转成了 0灵魂 / X虚空”的状态。//20260801
    ///
    /// 用途：这类牌卡牌效果里的 X 不能再取原版的
    /// <c>EnergyCost.CapturedXValue</c>（那是灵魂侧的值，失心后恒为 0），
    /// 必须取“本次实际支付的虚空量”。
    /// 由 Patches/LostEnergyCostPatch 在 CardModel.ResolveEnergyXValue 的
    /// Postfix 中调用，把 X 改写为 <see cref="GetLastVoidSpent"/>。
    /// </summary>
    public static bool IsLostEnergyX(CardModel card) =>
        States.TryGetValue(card, out TraitState? s) && s!.IsLost && s.LostConvertedToVoidX;

    /// <summary>
    /// 记录“这张牌本次打出实际支付了多少虚空”。//20260801
    /// 由 Patches/VoidPowerListener 在 RitsuLib 的副资源支付回调里调用。
    /// 只对已经建立过特质数据的牌记录，避免给普通牌白建状态对象。
    /// </summary>
    public static void RecordVoidSpent(CardModel card, int amount)
    {
        if (card == null) return;
        if (!States.TryGetValue(card, out TraitState? s)) return;
        s!.LastVoidSpent = amount;
    }

    /// <summary>读取最近一次打出时支付的虚空量（没有记录则为 0）。//20260801</summary>
    public static int GetLastVoidSpent(CardModel card) =>
        States.TryGetValue(card, out TraitState? s) ? s!.LastVoidSpent : 0;

    /// <summary>
    /// 这张牌当前是否有【纯粹】（机制文档：名词表·纯粹）。
    /// 两个来源：1) 卡牌类自带（PaleRegentModV1Card.IsPure 重写为 true，如虚空化形/化神/纯粹容器）；
    ///          2) 战斗中被附加（灵魂护佑等，走 ApplyPure 的 TraitState）。
    /// 判定用途（20260729 变更）：带纯粹的牌"战斗中无法被变化"——
    ///   1) 感染的随机转化不会选中它（Infection.OnTurnEndInHand 过滤）；
    ///   2) 任何 CardCmd.Transform / TransformTo / TransformToRandom 都无法把它变为其他牌
    ///      （Patches/PureTransformGuard 全局拦截）。
    /// </summary>
    public static bool IsPure(CardModel card) =>
        (card as Cards.PaleRegentModV1Card)?.IsPure == true ||
        (States.TryGetValue(card, out TraitState? s) && s!.IsPureApplied);

    /// <summary>给一张牌附加【纯粹】（战斗内，跟随卡牌实例）。</summary>
    public static void ApplyPure(CardModel card)
    {
        States.GetOrCreate(card).IsPureApplied = true;
        CardTraitUi.Refresh(card);
    }

    /// <summary>读取这张牌当前的虚空费（没有则为 0；X 费返回 0，请另行判断 CostsX）。</summary>
    public static int GetVoidCost(CardModel card)
    {
        SecondaryResourceCost? cost = card.SecondaryCosts().Get(VoidResource.Id);
        if (cost == null || cost.CostsX) return 0;
        return cost.Amount;
    }

    /// <summary>
    /// 这张牌是否“登记过虚空费条目”（即卡面上会显示虚空费，哪怕是 0 或 X）。//20260726
    /// 用于区分两类牌：
    ///   - 普通牌：从未登记虚空费，SecondaryCosts().Get 返回 null，卡面无虚空费显示；
    ///   - 虚空牌：登记过虚空费（含虚空 0 / 虚空 X），卡面显示虚空费。
    /// 规则“虚空费大于等于 0 的卡牌自动获得【消耗】”中的“虚空费≥ 0”
    /// 就是指本方法返回 true 的牌（登记过就算，无虚空费条目的普通牌不算）。
    /// </summary>
    public static bool HasVoidCost(CardModel card) =>
        card.SecondaryCosts().Get(VoidResource.Id) != null;

    /// <summary>这张牌能否附加【失心】：X 费牌（灵魂X或虚空X）无效。</summary>
    /*public static bool CanApplyLost(CardModel card)
    {
        if (card.EnergyCost.CostsX) return false;
        SecondaryResourceCost? vc = card.SecondaryCosts().Get(VoidResource.Id);
        if (vc != null && vc.CostsX) return false;
        return true;
    }*/
    /// <summary>这张牌能否附加【失心】：包括固定费和 X 费牌。</summary>
    public static bool CanApplyLost(CardModel card)
    {
        return !card.IsCanonical;
    }

    /// <summary>
    /// 这张牌能否附加【苍白】。//20260801
    ///
    /// 需求澄清：虚空 X 费的牌 **也应该能被加苍白**。
    /// 苍白的效果是“取消虚空消耗 + 添加虚无”，而取消虚空消耗走的是
    /// <c>SecondaryCosts().Clear(VoidResource.Id)</c>，它对固定费和 X 费都成立，
    /// 因此虚空 X 牌在机制上没有任何需要排除的理由。
    ///
    /// 唯一真正不能动的是 canonical（规范）实例：修改它会抛
    /// CanonicalModelException 导致游戏崩溃（参见 ApplyLost 头部注释）。
    ///
    /// 提供本方法的目的是给“施加苍白的卡”做选牌过滤用，
    /// 让过滤条件有唯一的可维护来源，避免各张牌各自写不一致的判定。
    /// </summary>
    public static bool CanApplyPale(CardModel card)
    {
        if (card == null) return false;
        if (card.IsCanonical) return false;
        // 已经是苍白的牌再加一次没有意义，不作为合法选择目标
        if (IsPale(card)) return false;
        return true;
    }

    // ---------------- 附加/移除 ----------------

    /// <summary>
    /// 给一张牌附加【失心】。
    /// 效果：灵魂费清零并 1:1 并入虚空费；获得重放1；取消苍白。
    /// 返回 false 表示这张牌不能附加（X 费牌）。
    /// </summary>
    public static bool ApplyLost(CardModel card)
    {
        // 防御：canonical（规范/不可变）实例禁止修改费用/关键词，否则游戏启动时
        // ModelDb 注册卡牌会抛 CanonicalModelException 直接崩溃（20260728 启动崩溃修复）。
        // "自带失心"请勿在构造器里调用本方法，改在 PaleRegentModV1Card.HasInnateLost 声明。
        if (card.IsCanonical) return false;
        if (!CanApplyLost(card)) return false;

        TraitState s = States.GetOrCreate(card);
        if (s.IsLost) return true; // 已经失心，无需重复处理

        // 若带苍白，先按"取消苍白"还原（苍白会清虚空费，需要先恢复）
        if (s.IsPale) RemovePale(card, s);

        EnsureCostSnapshot(card, s);

        /*// 换算：新虚空费 = 原虚空费 + 原灵魂费（1:1 转换）
        int currentEnergy = card.EnergyCost.GetWithModifiers(CostModifiers.None);
        int currentVoid = GetVoidCost(card);
        int newVoidCost = currentVoid + currentEnergy;

        card.EnergyCost.SetCustomBaseCost(0);                       // 灵魂费清零
        card.SecondaryCosts().Set(VoidResource.Id, newVoidCost);    // 并入虚空费（即使 0 也登记条目，成为虚空牌）//20260726*/
        // ============ 换算规则 ============
        // 1. 固定灵魂费 + 固定虚空费：正常相加。
        // 2. 灵魂费或虚空费只要任意一项为 X，统一转化为 0 灵魂、X 虚空。
        //    例如：X灵魂1虚空 → 0灵魂X虚空；2灵魂X虚空 → 0灵魂X虚空；
        //          X灵魂X虚空 → 0灵魂X虚空。
        //
        // 【重要】2026-08-01 修正：这里读取当前灵魂费必须用 CostModifiers.All，
        // 不能用 CostModifiers.None。
        //   - None 只拿到 _base（纸面基础费），不含任何修正；
        //   - 而「王者之踢」这类“每次抽到降低1点能量”的牌，减费是
        //     EnergyCost.AddThisCombat(-1) 存在 _localModifiers 层，_base 恒为纸面 4。
        // 用 None 会把 4 费当作当前费用并入虚空，玩家攒下的减费进度全部作废。
        // All = Local | Global，对应的就是卡面上玩家看到的那个数字。
        int currentEnergy = card.EnergyCost.GetWithModifiers(CostModifiers.All);

        SecondaryResourceCost? currentVoidCost =
            card.SecondaryCosts().Get(VoidResource.Id);

        bool energyCostsX = card.EnergyCost.CostsX;
        bool voidCostsX = currentVoidCost?.CostsX == true;
        bool convertsToVoidX = energyCostsX || voidCostsX;

        int currentVoid = GetVoidCost(card);

        // 【重要】2026-08-01 修正：这里 **不再** 调用 SetCustomBaseCost(0)。
        //
        // 旧实现用 SetCustomBaseCost(0) 直接改写 _base 来“取消灵魂耗能”，有两个致命缺陷：
        //   1. 对“灵魂 X”的牌根本无效。CardEnergyCost.CostsX 是构造时定死的只读属性，
        //      改 _base 改不了它；卡面 NCard 永远走 CostsX → 显示 "X" 分支，
        //      就会出现“灵魂X虚空0 附加失心后显示成 灵魂X虚空X”的 bug。
        //   2. 改写 _base 会和 local modifier 体系相互干扰（见上方 currentEnergy 注释），
        //      苍白还原时必然把费用写回纸面数值，抛弃玩家的减费进度。
        //
        // 现在的做法：失心完全不碰 _base、也不碰 _localModifiers，
        // 卡牌原本的费用体系保持原样；“灵魂费为 0”这件事由
        // Patches/LostEnergyCostPatch 在所有读取入口（GetWithModifiers /
        // GetAmountToSpend / GetResolved / 卡面文字）统一裁决。
        // 这样苍白只需把 IsLost 标记清掉，费用自然回到“原 _base + 全部 local 修正”。

        if (convertsToVoidX)
        {
            // X 统一计为 X，不叠加旁边的固定费用
            card.SecondaryCosts().Set(
                VoidResource.Id,
                SecondaryResourceCost.X(1));
            // 标记：本牌现在是“0灵魂 / X虚空”，牌效里的 X 要取虚空支付量
            // （而不是灵魂侧的 CapturedXValue）。见 IsLostEnergyX / GetLastVoidSpent。//20260801
            s.LostConvertedToVoidX = true;
        }
        else
        {
            // 普通固定费用仍然按照 1:1 相加
            int newVoidCost = currentVoid + currentEnergy;
            card.SecondaryCosts().Set(VoidResource.Id, newVoidCost);
            s.LostConvertedToVoidX = false;
        }

        card.BaseReplayCount = Math.Max(card.BaseReplayCount, LostReplayCount); // 重放1

        s.IsLost = true;
        CardTraitUi.Refresh(card);
      
        SyncExhaustKeyword(card, s); // 虚空费≥0（登记过虚空费条目）自动获得【消耗】 //20260726
        return true;
        
    }

    /// <summary>
    /// 给一张牌附加【苍白】。（20260729 需求变更）
    /// 效果：
    ///   1. 取消【失心】，及受其影响而添加的"灵魂并入虚空消费"和【消耗】；
    ///      如果这张牌没有失心就有【消耗】（牌自带/其他来源），不移除消耗。
    ///   2. 取消卡牌的虚空消耗（移除虚空费条目）。
    ///   3. 添加【虚无】。
    /// </summary>
    public static void ApplyPale(CardModel card)
    {
        // 防御：同 ApplyLost，canonical 实例不可修改（20260728 启动崩溃修复）
        if (card.IsCanonical) return;
        TraitState s = States.GetOrCreate(card);
        if (s.IsPale) return;

        EnsureCostSnapshot(card, s);

        // 1) 取消失心：恢复灵魂费、收回重放，并移除"失心加上去的消耗"
        if (s.IsLost)
        {
            // 【重要】2026-08-01 修正：这里 **不再** 调用
            // SetCustomBaseCost(s.OriginalEnergyCost) 来“还原灵魂费”。
            //
            // 旧实现把快照里的数字写回 _base，而快照取的是
            // GetWithModifiers(CostModifiers.None) = 纸面基础费。
            // 对「王者之踢」这类“每次抽到降低1点能量”的牌（减费存在
            // _localModifiers 层，_base 恒为纸面 4），还原结果就是回到 4 费，
            // 玩家攒下的减费进度全部作废——这就是“失心后能量变回最大值”的根因。
            //
            // 现在失心全程不会修改 _base（见 ApplyLost 里的说明），
            // 灵魂费为 0 只是 Patches/LostEnergyCostPatch 在读取端的裁决结果。
            // 所以只要把 IsLost 标记清掉，补丁自然不再介入，
            // 费用立即回到“原 _base + 全部 local/global 修正”，
            // 王者之踢累积的每一层 -1 都一字不差地保留。
            card.BaseReplayCount = 0; // 收回失心送的重放
            s.IsLost = false;
            s.LostConvertedToVoidX = false; // X 转化标记一并清掉 //20260801
        }
        // 只移除"失心流程加上去的消耗"；牌本来就有的消耗保持不动（20260729 变更）
        // 注：即使这张牌当前没有失心（例如失心曾被其他途径取消），只要消耗是失心加的也一并清理。
        if (s.ExhaustAddedByLost)
        {
            card.RemoveKeyword(CardKeyword.Exhaust);
            s.ExhaustAddedByLost = false;
        }

        // 2) 取消卡牌的虚空消耗
        // 注意：这里用 Clear 而不是 Set(0)，把虚空费条目整个移除，
        // 卡面不再显示虚空费 → 不再命中“虚空费≥0 自动消耗”规则 //20260726
        //
        // 20260801：虚空 X 费的牌（虚空必杀 / 回溯 / 虚空实验）把 X 费写在
        // **构造器**里（CardTraits.SetVoidCostX），属于 RitsuLib 的 Permanent 层。
        // RitsuLib 在卡牌降级/克隆/重建实例时会用 ResetPermanentLayersFrom
        // 把 canonical 的 Permanent 层重新灌回来，所以单靠这一次 Clear 会被回灌推翻，
        // 玩家看到的现象就是“虚空X 牌加不上苍白”。
        // 因此这里额外打上 VoidCostClearedByPale 标记，
        // 由 EnforcePaleVoidCostCleared 在后续读取/刷新时反复保证它保持被清除。
        card.SecondaryCosts().Clear(VoidResource.Id);
        s.VoidCostClearedByPale = true;

        // 3) 添加【虚无】
        card.AddKeyword(CardKeyword.Ethereal);

        s.IsPale = true;
        CardTraitUi.Refresh(card);
        // 20260729 变更：苍白不再"顺带添加消耗"，也不做旧版 SyncExhaustKeyword 的
        // 保守增删——消耗的移除已在上面按 ExhaustAddedByLost 精确处理。
    }

    /// <summary>内部：移除苍白状态（供 ApplyLost 里互斥切换时调用）。</summary>
    private static void RemovePale(CardModel card, TraitState s)
    {
        card.RemoveKeyword(CardKeyword.Ethereal);
        // 先除“苍白已清除虚空费”标记，否则下面刚还原的虚空费
        // 会立即被 EnforcePaleVoidCostCleared 又清掉 //20260801
        s.VoidCostClearedByPale = false;
        // 恢复原虚空费条目（苍白 Clear 掉的部分）：
        // 只要原本登记过虚空费条目就恢复（含虚空 0 / 虚空 X），保持“虚空牌”身份 //20260726
        if (s.OriginalHasVoidCost)
        {
            if (s.OriginalVoidCostsX)
                card.SecondaryCosts().Set(VoidResource.Id, SecondaryResourceCost.X(1)); // 还原 X 费 //20260726
            else
                card.SecondaryCosts().Set(VoidResource.Id, s.OriginalVoidCost);
            SyncExhaustKeyword(card, s); // 重新成为虚空牌 → 自动【消耗】
        }
        else
        {
            // 原本没有虚空费条目，苍白 Clear 后也无需恢复 //20260801
            s.LostConvertedToVoidX = false;
        }
        s.IsPale = false;
    }

    // ---------------- 内部工具 ----------------

    /// <summary>
    /// 第一次修改费用前，把原始费用记下来。
    ///
    /// 【重要】2026-08-01 修正：OriginalEnergyCost 改用 CostModifiers.All。
    /// 旧实现用 None（只拿 _base 纸面基础费），导致「王者之踢」这类
    /// 把减费放在 _localModifiers 层的牌快照恒为纸面最大值。
    ///
    /// 注意：修正后本字段 **已不再用于还原灵魂费**（失心/苍白都不再写 _base），
    /// 保留它仅供调试、日志与可能的展示需求使用。
    /// </summary>
    private static void EnsureCostSnapshot(CardModel card, TraitState s)
    {
        if (s.CostSnapshotTaken) return;
        s.OriginalEnergyCost = card.EnergyCost.GetWithModifiers(CostModifiers.All);
        s.OriginalEnergyCostsX = card.EnergyCost.CostsX; //20260801 记录原灵魂费是否为 X 费
        s.OriginalVoidCost = GetVoidCost(card);
        s.OriginalHasVoidCost = HasVoidCost(card); //20260726 记录原本是否登记过虚空费条目
        SecondaryResourceCost? vc = card.SecondaryCosts().Get(VoidResource.Id);
        s.OriginalVoidCostsX = vc != null && vc.CostsX; //20260726 记录原虚空费是否为 X 费
        s.CostSnapshotTaken = true;
    }

    /// <summary>
    /// 规则："虚空费大于等于 0 的卡牌自动获得【消耗】。" //20260726 规则由“大于0”改为“大于等于0”
    /// 这里的“虚空费 ≥ 0”指的是“卡面上登记/显示了虚空费条目”的牌（HasVoidCost），
    /// 即：虚空 0、虚空 X 也算虚空牌，自动带【消耗】；
    /// 而“卡面上根本没有虚空费”的普通牌（Get 返回 null）不受本规则影响。
    /// 20260729 变更：加消耗时记录 ExhaustAddedByLost（此前牌上没有消耗才算我们加的），
    /// 供苍白按"消耗是否失心所加"精确移除；不再在这里做移除。
    /// </summary>
    private static void SyncExhaustKeyword(CardModel card, TraitState s)
    {
        if (!HasVoidCost(card)) return;
        // 登记过虚空费条目（含虚空 0 / 虚空 X）→ 自动【消耗】 //20260726
        if (!card.Keywords.Contains(CardKeyword.Exhaust))
        {
            card.AddKeyword(CardKeyword.Exhaust);
            s.ExhaustAddedByLost = true; // 这次消耗是失心/虚空费流程加的（20260729）
        }
        // 牌上已有消耗（自带或其他来源）→ 不动，也不标记为失心所加
    }

    /// <summary>
    /// 给"有固定虚空费"的卡在构造时声明费用的统一入口。
    /// 卡牌构造器里调用：CardTraits.SetVoidCost(this, 7);
    /// 依赖 VoidResource.Register() 已在 MainFile.Initialize 里先执行
    /// （已确认注册早于 ModelDb 懒加载，构造期使用安全）。
    /// </summary>
    public static void SetVoidCost(CardModel card, int amount)
    {
        card.SecondaryCosts().Set(VoidResource.Id, amount);
    }

    /// <summary>
    /// 确保【苍白】牌的虚空费保持被清除。//20260801
    ///
    /// ============ 为什么需要反复清除 ============
    /// 虚空 X 费的牌（虚空必杀 / 回溯 / 虚空实验）把 X 费写在 **构造器** 里：
    ///   <c>CardTraits.SetVoidCostX(this, 1)</c>
    /// 在 RitsuLib 里这属于 Permanent（永久）层。而 RitsuLib 内部会在
    /// 卡牌降级、克隆、重建实例等时机调用 <c>ResetPermanentLayersFrom</c>，
    /// 把 canonical（规范实例）的 Permanent 层重新灌回到当前实例上。
    ///
    /// 因此苍白里那一句 <c>SecondaryCosts().Clear(VoidResource.Id)</c>
    /// 只能管住一时：一旦发生回灌，X 虚空费就又回来了。
    /// 而卡面虚空费是由 RitsuLib 的 NSecondaryResourceCardCostUi
    /// **自动读取 SecondaryCosts 渲染** 的（见 Resources/VoidResource.cs），
    /// 所以回灌之后卡面会重新显示“虚空 X”，
    /// 玩家的直观感受就是“虚空X 的牌无法被加上苍白”。
    ///
    /// ============ 本方法的做法 ============
    /// 不去和构造器/回灌机制抢“谁先写”，而是把“苍白已经清除过虚空费”
    /// 这个事实记在 <c>VoidCostClearedByPale</c> 标记里（标记挂在卡牌实例的
    /// 特质数据上，不会被 RitsuLib 的费用层回灌影响），
    /// 然后在每一个“可能看到回灌结果”的时机调用本方法把它再清一次：
    ///   1. Patches/VoidPowerListener.ModifySecondaryResourceCostLate（费用裁决）
    ///   2. Traits/CardTraitUi.Refresh（卡面显示）
    ///
    /// 这样无论回灌发生多少次，玩家看到的和实际结算的都是“无虚空费”。
    ///
    /// 注：本方法幂等，且对非苍白牌 / canonical 牌 / 本来就没虚空费的牌
    /// 都会直接返回，可以安心在高频路径（卡面刷新/费用查询）里调用。
    /// </summary>
    public static void EnforcePaleVoidCostCleared(CardModel? card)
    {
        if (card == null) return;
        // canonical 实例不得修改，否则抛 CanonicalModelException
        if (card.IsCanonical) return;
        // 没建过特质数据的牌直接跳过，不给普通牌白建状态对象
        if (!States.TryGetValue(card, out TraitState? s)) return;
        if (!s!.IsPale || !s.VoidCostClearedByPale) return;
        // 只有确实又出现了虚空费条目才重新 Clear，避开无意义的写入与事件颤动
        if (card.SecondaryCosts().Get(VoidResource.Id) == null) return;
        card.SecondaryCosts().Clear(VoidResource.Id);
    }

    /// <summary>给"虚空 X 费"的卡声明费用（打出时消耗全部虚空作为 X）。</summary>
    public static void SetVoidCostX(CardModel card, int multiplier = 1)
    {
        card.SecondaryCosts().Set(VoidResource.Id, SecondaryResourceCost.X(multiplier));
    }
}
