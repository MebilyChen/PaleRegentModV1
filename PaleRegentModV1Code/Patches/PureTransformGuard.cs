using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 【纯粹】通用防变化拦截（机制文档·名词表：纯粹，20260729 变更）。
///
/// 需求："战斗中无法被变化，例如被变化为其他牌
///        （免疫"感染"转化实现为不会被随机选中）。"
///
/// 实现说明（给未来的你/维护者）：
/// - 原版所有"把 A 牌变成 B 牌"的操作，最终都汇聚到
///   CardCmd.Transform(IEnumerable&lt;CardTransformation&gt;, Rng?, CardPreviewStyle) 这一个总入口
///   （TransformTo&lt;T&gt;、TransformToRandom、单张 Transform 都是它的包装）。
///   因此只需要 Harmony Prefix 这一个方法，把"战斗中的纯粹牌"的变化项
///   从 transformations 列表里剔除，就能全局生效——
///   包括本 mod 的回收（Recycle）、王室重铸（RoyalRecasting）、
///   虚空赐予的专注（VoidGivenFocus），以及原版/其他 mod 的任何战斗内变化。
/// - "感染"的随机转化在 Infection.OnTurnEndInHand 里已用 IsPure 过滤
///   （即需求括号里的"不会被随机选中"），本 patch 是它之上的第二道保险。
/// - 只拦截"战斗中"的变化：营地事件、奖励界面等地图上的卡组变化不在
///   "战斗中无法被变化"范围内，不拦截（CardTransformation.IsInCombat 判定，
///   该值在构造时就捕获了 Original.CombatState != null，安全可靠）。
/// - 为什么不 patch CardModel.IsTransformable：它是非虚属性且被卡组界面、
///   营地铁匠等大量地图逻辑共用，改它会把 Pure 牌从地图上的变化列表里也剔除，
///   超出"战斗中"这一需求范围。
///
/// 修改指南：
/// - 想让纯粹在地图上也无法被变化：把下面的 t.IsInCombat 条件去掉即可。
/// </summary>
[HarmonyPatch(typeof(CardCmd))]
internal static class PureTransformGuard
{
    /// <summary>
    /// Prefix：在变化执行前过滤掉"战斗中的纯粹牌"。
    /// 注意 Transform 是 async 方法，但 Harmony patch 的是编译器生成的外壳方法，
    /// 在状态机启动前修改参数值（ref transformations）依然有效。
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(CardCmd.Transform),
        typeof(IEnumerable<CardTransformation>), typeof(Rng), typeof(CardPreviewStyle))]
    private static void FilterPureCards(ref IEnumerable<CardTransformation> transformations)
    {
        // ToList 物化一次，避免延迟枚举被重复求值产生副作用
        List<CardTransformation> filtered = transformations
            .Where(t => !(t.IsInCombat && CardTraits.IsPure(t.Original)))
            .ToList();
        transformations = filtered;
        // 过滤后为空时，原方法自身会走 transformationsArr.Length == 0 分支
        // 返回空结果，无需额外处理。
    }
}
