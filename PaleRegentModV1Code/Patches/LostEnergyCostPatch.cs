using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI; // ModelVisibility
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 【失心】灵魂费归零的"读取端裁决"补丁。//20260801
///
/// ============ 为什么要用补丁，而不是直接改费用 ============
/// 旧实现是在 CardTraits.ApplyLost 里调用
/// <c>card.EnergyCost.SetCustomBaseCost(0)</c> 把灵魂费写成 0，
/// 苍白时再 <c>SetCustomBaseCost(原灵魂费)</c> 写回去。这带来两个无法回避的缺陷：
///
/// 1. X 费牌的灵魂费改不掉。
///    原版 <see cref="CardEnergyCost.CostsX"/> 是构造函数里定死的 **只读** 属性，
///    并且 <see cref="CardEnergyCost.GetWithModifiers"/> 在 CostsX 为真时会
///    直接 return _base、完全跳过所有修正。所以对"灵魂 X"的牌：
///      - SetCustomBaseCost(0) 毫无效果（本来 Canonical 就是 0）；
///      - 卡面 <see cref="NCard"/> 永远走 <c>CostsX → 显示 "X"</c> 分支；
///      - 结果就是"灵魂X虚空0 附加失心后显示成 灵魂X虚空X"，
///        而不是需求要求的"灵魂0虚空X"。
///
/// 2. 能量会变化的牌，费用进度会被抹掉。
///    原版「王者之踢」的"每次抽到降低1点能量"实现为
///    <c>EnergyCost.AddThisCombat(-1)</c>，也就是往 _localModifiers 里
///    追加一层 LocalCostModifier，_base 恒为纸面的 4。
///    而旧实现的费用快照取的是 <c>GetWithModifiers(CostModifiers.None)</c>，
///    None 既不含 Local 也不含 Global，拿到的就是 _base = 4（纸面最大值）。
///    于是"失心 → 苍白"这一轮结束后灵魂费被写回 4，
///    玩家此前攒下的所有减费进度全部作废。
///
/// ============ 本补丁的做法 ============
/// 失心不再触碰 _base，也不再触碰 _localModifiers，卡牌的原始费用体系
/// **保持完全原样**；"灵魂费为 0"这件事改由本补丁在所有读取入口统一裁决：
///
///   - <see cref="CardEnergyCost.GetWithModifiers"/>  → 卡面显示、各种费用查询
///   - <see cref="CardEnergyCost.GetAmountToSpend"/>  → 实际扣多少灵魂
///   - <see cref="CardEnergyCost.GetResolved"/>       → 打出后回看费用的效果
///   - <see cref="NCard"/>.UpdateEnergyCostVisuals    → 强制把卡面的 "X" 改写成 "0"
///   - <see cref="CardModel.ResolveEnergyXValue"/>    → 失心前是灵魂X时，X 取"本次虚空支付量"
///
/// 这样一来：
///   - 苍白只需要把 IsLost 标记清掉，费用自然回到"原始 _base + 全部 local 修正"，
///     王者之踢累积的 -1 一层都不会丢（修复问题 2）；
///   - 灵魂X 牌失心后卡面显示 0 灵魂、X 虚空，效果的 X 也取虚空支付量（修复问题 1）。
///
/// ============ 维护提示 ============
/// - CardEnergyCost 没有公开的"反查所属卡牌"入口，只有私有字段 _card，
///   所以这里用 AccessTools 缓存一个 FieldInfo 反射读取；
///   若日后原版重命名该字段，_cardField 会为 null，补丁会安全地整体失效
///   （只是失心的灵魂费不再归零，不会崩游戏）。
/// - 想改"失心后灵魂费变成几"，只改 <see cref="LostEnergyCost"/> 常量即可。
/// </summary>
internal static class LostEnergyCostPatch
{
    /// <summary>失心之后这张牌的灵魂费固定为多少（需求：取消灵魂耗能 → 0）。</summary>
    private const int LostEnergyCost = 0;

    /// <summary>CardEnergyCost 私有字段 _card，用于从费用对象反查所属卡牌。</summary>
    private static readonly FieldInfo? CardField =
        AccessTools.Field(typeof(CardEnergyCost), "_card");

    /// <summary>NCard 私有字段 _energyLabel，用于改写卡面灵魂费文字。</summary>
    private static readonly FieldInfo? EnergyLabelField =
        AccessTools.Field(typeof(NCard), "_energyLabel");

    /// <summary>
    /// 从 CardEnergyCost 反查所属 CardModel，并判断它当前是否带【失心】。
    /// 拿不到字段（原版改名）或不是失心牌都返回 false，让原版逻辑原样通过。
    /// </summary>
    private static bool IsLostCost(CardEnergyCost cost, out CardModel? card)
    {
        card = CardField?.GetValue(cost) as CardModel;
        return card != null && CardTraits.IsLost(card);
    }

    // ---------------- 费用读取端 ----------------

    /// <summary>
    /// 卡面显示与各类费用查询的统一入口。
    /// 失心牌一律汇报 0 灵魂费（含 X 费牌，X 费牌原版这里会直接 return _base）。
    /// </summary>
    [HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.GetWithModifiers))]
    [HarmonyPostfix]
    private static void GetWithModifiersPostfix(CardEnergyCost __instance, ref int __result)
    {
        if (IsLostCost(__instance, out _))
        {
            __result = LostEnergyCost;
        }
    }

    /// <summary>
    /// 实际支付灵魂时用的值。
    /// 失心牌返回 0，因此：
    ///   1. 不会扣灵魂（需求：取消灵魂耗能）；
    ///   2. 对原本是灵魂X的牌，原版 SpendEnergy 会把 CapturedXValue 记为 0，
    ///      避免"X 传的还是灵魂的值"——真正的 X 由 ResolveEnergyXValue 补丁给出。
    /// </summary>
    [HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.GetAmountToSpend))]
    [HarmonyPostfix]
    private static void GetAmountToSpendPostfix(CardEnergyCost __instance, ref int __result)
    {
        if (IsLostCost(__instance, out _))
        {
            __result = LostEnergyCost;
        }
    }

    /// <summary>
    /// 打出之后"回看这张牌花了多少费"的入口（如威吓头盔一类效果）。
    /// 失心牌花的是虚空、不是灵魂，所以灵魂侧一律汇报 0。
    /// </summary>
    [HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.GetResolved))]
    [HarmonyPostfix]
    private static void GetResolvedPostfix(CardEnergyCost __instance, ref int __result)
    {
        if (IsLostCost(__instance, out _))
        {
            __result = LostEnergyCost;
        }
    }

    // ---------------- X 值解析 ----------------

    /// <summary>
    /// 【失心】把"灵魂X"整张牌转成"0灵魂 / X虚空"之后，
    /// 卡牌效果里的 X 必须取 **本次实际支付的虚空量**，
    /// 而不是原版的 <c>EnergyCost.CapturedXValue</c>（那是灵魂侧的值，现在恒为 0）。
    ///
    /// 虚空支付量由 <see cref="VoidPowerListener"/> 在
    /// AfterSecondaryResourceSpent 回调里记入 CardTraits，见
    /// <see cref="CardTraits.GetLastVoidSpent"/>。
    /// </summary>
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ResolveEnergyXValue))]
    [HarmonyPostfix]
    private static void ResolveEnergyXValuePostfix(CardModel __instance, ref int __result)
    {
        if (CardTraits.IsLostEnergyX(__instance))
        {
            __result = CardTraits.GetLastVoidSpent(__instance);
        }
    }

    // ---------------- 卡面文字 ----------------

    /// <summary>
    /// 卡面灵魂费文字的兜底改写。
    ///
    /// 原版 <c>NCard.UpdateEnergyCostVisuals</c> 对 CostsX 的牌是硬编码
    /// <c>_energyLabel.SetTextAutoSize("X")</c>，根本不会去读
    /// GetWithModifiers，所以上面的 GetWithModifiers 补丁救不到卡面。
    /// 这里在原版画完之后，把失心牌的灵魂费文字强行改回 "0"。
    ///
    /// 注：非 X 费的失心牌走的是 GetWithModifiers 分支，已经显示 0，
    /// 这里重复写一次 "0" 也是幂等的，不额外判断以保持逻辑简单。
    /// </summary>
    [HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
    [HarmonyPostfix]
    private static void UpdateEnergyCostVisualsPostfix(NCard __instance)
    {
        CardModel? model = __instance.Model;
        if (model == null || !CardTraits.IsLost(model))
        {
            return;
        }

        // 卡背/未知牌（Visibility != Visible）时原版显示 "?"，不要覆盖它
        if (__instance.Visibility != ModelVisibility.Visible)
        {
            return;
        }

        if (EnergyLabelField?.GetValue(__instance) is MegaLabel label)
        {
            label.SetTextAutoSize(LostEnergyCost.ToString());
        }
    }
}
