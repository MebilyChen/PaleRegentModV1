using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 自定义 Trait 的牌面显示补丁。
///
/// Pure 的特殊点：图鉴列表可能直接使用 canonical CardModel。
/// canonical 不安全 AddKeyword，因此这里不修改 canonical，
/// 而是在 CardModel 描述层 + NCard 最终显示层各补一次兜底。
/// </summary>
internal static class TraitCardTextPatch
{
    // =====================================================================
    //  失心 Hover
    // =====================================================================

    [HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))]
    private static class LostHoverPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(CardKeyword keyword, ref IHoverTip __result)
        {
            if (keyword != TraitKeywords.Lost)
                return;

            __result = ModHoverTips.Lost;
        }
    }

    // =====================================================================
    //  CardModel 描述层
    // =====================================================================

    [HarmonyPatch(typeof(CardModel))]
    private static class KeywordColorPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(
            nameof(CardModel.GetDescriptionForPile),
            typeof(PileType),
            typeof(Creature))]
        private static void GetDescriptionForPilePostfix(
            CardModel __instance,
            ref string __result)
        {
            __result = EnsureInnatePureText(__instance, __result);
            __result = ReplaceTraitKeywordColors(__result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(CardModel.GetDescriptionForUpgradePreview))]
        private static void GetDescriptionForUpgradePreviewPostfix(
            CardModel __instance,
            ref string __result)
        {
            __result = EnsureInnatePureText(__instance, __result);
            __result = ReplaceTraitKeywordColors(__result);
        }
    }

    // =====================================================================
    //  ★ 图鉴列表最终显示层兜底
    //
    //  STS2 的 NCard.UpdateVisuals() 会自己拿 CardModel 描述，
    //  然后写入 %DescriptionLabel。
    //
    //  图鉴缩略卡这条路径和战斗 / 大图实例不同，因此这里在 UpdateVisuals
    //  完成后直接重写一次描述标签。只处理天生 Pure 卡。
    // =====================================================================

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static class NCardUpdateVisualsPurePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            NCard __instance,
            PileType pileType,
            CardPreviewMode previewMode)
        {
            if (__instance == null || !GodotObject.IsInstanceValid(__instance))
                return;

            CardModel? card = __instance.Model;
            if (card == null)
                return;

            if (card is not PaleRegentModV1.PaleRegentModV1Code.Cards.PaleRegentModV1Card paleCard)
                return;

            if (!paleCard.IsPure)
                return;

            // 不覆盖“未见 / 锁定”卡牌的隐藏文案。
            if (!string.Equals(__instance.Visibility.ToString(), "Visible", System.StringComparison.Ordinal))
                return;

            Creature? target = card.CurrentTarget;

            string text = previewMode != CardPreviewMode.Upgrade
                ? card.GetDescriptionForPile(pileType, target)
                : card.GetDescriptionForUpgradePreview();

            // 即使 CardModel 层的 patch 因调用链差异没生效，这里也再兜底一次。
            text = EnsureInnatePureText(card, text);
            text = ReplaceTraitKeywordColors(text);

            MegaRichTextLabel? descriptionLabel =
                __instance.GetNodeOrNull<MegaRichTextLabel>("%DescriptionLabel");

            if (descriptionLabel == null)
                return;

            descriptionLabel.SetTextAutoSize("[center]" + text + "[/center]");
        }
    }

    // =====================================================================
    //  Pure 文案兜底
    // =====================================================================

    private static string EnsureInnatePureText(CardModel card, string text)
    {
        if (card == null)
            return text;

        if (card is not PaleRegentModV1.PaleRegentModV1Code.Cards.PaleRegentModV1Card paleCard)
            return text;

        if (!paleCard.IsPure)
            return text;

        text ??= string.Empty;

        // 已经有 Pure 文案就不重复。
        if (ContainsPureText(text))
            return text;

        // mutable 实例如果已经有真实 keyword，通常 RitsuLib 已经负责显示。
        // 但图鉴路径可能存在“keyword 已有、最终字符串没插入”的情况，
        // 所以这里不再因为 Keywords.Contains(Pure) 而提前 return。
        //
        // 这是相对上一版最重要的修改之一。

        if (string.IsNullOrEmpty(text))
            return "[gold]纯粹[/gold]。";

        return "[gold]纯粹[/gold]。\n" + text;
    }

    private static bool ContainsPureText(string text)
    {
        return text.Contains("[gold]纯粹[/gold]") ||
               text.Contains("[aqua]纯粹[/aqua]") ||
               text.Contains("[gold]Pure[/gold]") ||
               text.Contains("[aqua]Pure[/aqua]");
    }

    // =====================================================================
    //  Trait keyword 颜色
    // =====================================================================

    private static string ReplaceTraitKeywordColors(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("[gold]纯粹[/gold]", "[aqua]纯粹[/aqua]")
            .Replace("[gold]失心[/gold]", "[purple]失心[/purple]")
            .Replace("[gold]苍白[/gold]", "[blue]苍白[/blue]")
            .Replace("[gold]Pure[/gold]", "[aqua]Pure[/aqua]")
            .Replace("[gold]Lost[/gold]", "[purple]Lost[/purple]")
            .Replace("[gold]Pale[/gold]", "[blue]Pale[/blue]");
    }
}
