using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 【失心重击】多人兼容兜底。
///
/// 背景：
/// HeartlessBlow 本身真实灵魂费仍然是 1，
/// 正常情况下依赖 CardTraits.IsLost -> LostEnergyCostPatch
/// 在读取/支付时把灵魂费裁决为 0。
///
/// 多人模式中如果 AttachedState 的 IsLost 临时丢失，
/// 但失心转换后真正写入 CardModel 的虚空费仍然存在，
/// 原来的 LostEnergyCostShared.IsLostCost 会错误返回 false，
/// 从而使真实的 1 灵魂费重新暴露。
///
/// 本补丁不修改 CardTraits、不重新 ApplyLost、不修改任何费用基础值。
/// 只在 LostEnergyCostShared 已经判定“不是失心”之后，
/// 对 HeartlessBlow 做一个非常窄的兜底：
///
///     HeartlessBlow + 仍有虚空费条目
///         => 灵魂支付仍按失心的 0 处理。
///
/// 苍白会 Clear 虚空费，所以苍白后的 HeartlessBlow 不会命中本兜底。
/// </summary>
[HarmonyPatch(
    typeof(LostEnergyCostShared),
    nameof(LostEnergyCostShared.IsLostCost))]
internal static class MultiplayerHeartlessBlowSafetyPatch
{
    /// <summary>
    /// CardEnergyCost 没有公开所属卡牌，
    /// 与 LostEnergyCostPatch 原实现一样反射读取 _card。
    /// </summary>
    private static readonly FieldInfo? CardField =
        AccessTools.Field(typeof(CardEnergyCost), "_card");

    [HarmonyPostfix]
    private static void Postfix(
        CardEnergyCost __0,
        ref bool __result)
    {
        // 原来的失心系统正常工作时，完全不介入。
        if (__result)
            return;

        CardModel? card =
            CardField?.GetValue(__0) as CardModel;

        // 极窄兜底：现在只救失心重击。
        if (card is not HeartlessBlow)
            return;

        // HeartlessBlow 正常失心后会有虚空费条目。
        //
        // 如果已经被苍白：
        // ApplyPale 会 Clear(VoidResource.Id)，
        // 所以这里为 false，不会错误地继续强制 0 灵魂。
        if (!CardTraits.HasVoidCost(card))
            return;

        // 判定为“失心费用”。
        //
        // 接下来现有 LostEnergyCostPatch 会自然让：
        // GetWithModifiers  = 0
        // GetAmountToSpend  = 0
        // GetResolved       = 0
        __result = true;
    }
}
