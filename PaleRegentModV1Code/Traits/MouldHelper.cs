using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using PaleRegentModV1.PaleRegentModV1Code.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Relics;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 【模具】体系（名词表 N#9，20260725 新增）。
///
/// 规则（表格名词 N#9 / 遗物 R#2 原文）：
/// 1. 带"模具"标记的牌（IsMould，目前：国王佣卫 / 有翼佣卫 / 虚空化模）
///    在本场战斗中每被【消耗】1 次，记 1 点计数；
///    升级牌与未升级牌同名合并计数（按牌类型聚合，天然满足）。
/// 2. 战斗结束时，每种模具牌按（本场消耗数 / 100）的概率获得对应的
///    "模具·［卡牌名］"遗物；若已拥有则延长其剩余战斗场数 +1，不重复获得。
/// 3. 模具遗物效果：你的每回合开始时，生成并打出［卡牌名］（去除 Harness
///    临时效果）；遗物在 1 场战斗后失效（由各 Mould* 遗物自身管理剩余场数）。
///
/// 实现说明：
/// - 计数入口：PaleRegentModV1Power 没有全局常驻实例，因此计数挂在
///   苍白信物（PaleToken.AfterCardExhausted 若存在）或 Harmony 补丁上；
///   当前采用 PlagueBladePatch 同款思路——直接在 CardPile.Exhaust 数据上统计
///   不可行（跨牌种），故由 MouldExhaustListener（Harmony Postfix CardCmd.Exhaust）调用 NoteExhaust。
/// - 概率随机源：Owner.RunState.Rng.CombatTargets（战斗随机流，不影响地图种子）。
/// </summary>
public static class MouldHelper
{
    /// <summary>本场战斗中各模具牌的消耗计数（key = 模具牌类型）。</summary>
    private static readonly Dictionary<Type, int> ExhaustCounts = new();

    /// <summary>模具遗物持续场数（表格 R#2：1 场战斗后失效）。</summary>
    public const int RelicCombats = 1;

    /// <summary>每消耗 1 张的成功概率（百分比）。</summary>
    public const int ChancePerExhaustPercent = 1;

    /// <summary>记录一次模具牌消耗（由 MouldExhaustListener 调用）。</summary>
    public static void NoteExhaust(CardModel card)
    {
        if (card is not PaleRegentModV1Card { IsMould: true })
        {
            return;
        }
        Type key = card.GetType();
        ExhaustCounts.TryGetValue(key, out int n);
        ExhaustCounts[key] = n + 1;
    }

    /// <summary>战斗开始时清零计数（防上一场残留）。</summary>
    public static void ResetCounts()
    {
        ExhaustCounts.Clear();
    }

    /// <summary>
    /// 战斗结束时结算模具遗物（由 PaleToken.AfterCombatEnd 调用）。
    /// 每种模具牌独立判定：概率 = 消耗数 x 1%。
    /// </summary>
    public static async Task RollMouldRelics(Player player, CombatRoom room)
    {
        foreach (KeyValuePair<Type, int> pair in ExhaustCounts)
        {
            int chance = pair.Value * ChancePerExhaustPercent;
            if (chance <= 0)
            {
                continue;
            }
            // 随机源：Random.Shared（modstudy HornetAudio 同款用法）；
            // 不走存档随机流，对地图/卡牌种子无影响。
            int roll = Random.Shared.Next(100);
            if (roll >= chance)
            {
                continue;
            }
            await GrantMouldRelic(player, pair.Key);
        }
        ExhaustCounts.Clear();
    }

    /// <summary>发放（或刷新）对应的模具遗物。</summary>
    private static async Task GrantMouldRelic(Player player, Type mouldCardType)
    {
        // 已拥有 → 刷新剩余场数
        foreach (RelicModel owned in player.Relics)
        {
            if (owned is MouldRelic existing && existing.MouldCardType == mouldCardType)
            {
                // 已拥有 → 延长战斗场数 +1（表格 N#9）
                existing.ExtendCombats(1);
                return;
            }
        }

        RelicModel? relic = null;
        if (mouldCardType == typeof(KingsRetainer))
        {
            relic = ModelDb.Relic<MouldKingsRetainer>().ToMutable();
        }
        else if (mouldCardType == typeof(WingedRetainerCard))
        {
            relic = ModelDb.Relic<MouldWingedRetainer>().ToMutable();
        }
        else if (mouldCardType == typeof(VoidGivenMould))
        {
            relic = ModelDb.Relic<MouldVoidGivenMould>().ToMutable();
        }
        if (relic == null)
        {
            return;
        }
        // 获得遗物：RelicCmd.Obtain(RelicModel, Player, Int32 ...) —— BaseLib 日志中确认存在该签名；
        // 若编译不匹配请改用 RelicCmd.Obtain(relic, player) 重试（备注：待游戏内验证）。
        await RelicCmd.Obtain(relic, player, 0);
    }
}
