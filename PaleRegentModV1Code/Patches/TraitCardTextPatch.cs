using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 自定义 Trait 的牌面显示补丁。
///
/// 1. 【失心】关键词仍由 TraitKeywords.Lost / RitsuLib 原生显示，
///    这里只在 HoverTipFactory.FromKeyword 完成后，把 Lost Hover
///    最终替换成 ModHoverTips.Lost，以支持动态 {LostReplayCount}。
///
/// 2. RitsuLib 会把 BeforeCardDescription 的关键词标题统一包成 [gold]。
///    这里不重新插入关键词，只在最终牌面描述字符串中替换颜色：
///      纯粹 -> aqua
///      失心 -> purple
///      苍白 -> blue
///
/// 这样不会影响原版关键词、Trait keyword 注册、费用逻辑或 Trait 状态。
/// </summary>
internal static class TraitCardTextPatch
{
    // =====================================================================
    //  失心 Hover：最终覆盖 RitsuLib 自动生成的静态 Hover
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
    //  牌面关键词颜色
    // =====================================================================

    [HarmonyPatch(typeof(CardModel))]
    private static class KeywordColorPatch
    {
        /// <summary>
        /// 手牌、牌堆、查看界面的最终牌面描述。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(
            nameof(CardModel.GetDescriptionForPile),
            typeof(PileType),
            typeof(Creature))]
        private static void GetDescriptionForPilePostfix(ref string __result)
        {
            __result = ReplaceTraitKeywordColors(__result);
        }

        /// <summary>
        /// 升级预览也同步换色。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CardModel.GetDescriptionForUpgradePreview))]
        private static void GetDescriptionForUpgradePreviewPostfix(ref string __result)
        {
            __result = ReplaceTraitKeywordColors(__result);
        }
    }

    private static string ReplaceTraitKeywordColors(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            // 中文
            .Replace("[gold]纯粹[/gold]", "[aqua]纯粹[/aqua]")
            .Replace("[gold]失心[/gold]", "[purple]失心[/purple]")
            .Replace("[gold]苍白[/gold]", "[blue]苍白[/blue]")

            // 英文 localization 兜底
            .Replace("[gold]Pure[/gold]", "[aqua]Pure[/aqua]")
            .Replace("[gold]Lost[/gold]", "[purple]Lost[/purple]")
            .Replace("[gold]Pale[/gold]", "[blue]Pale[/blue]");
    }
}
