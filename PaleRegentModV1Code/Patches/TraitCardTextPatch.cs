using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 【失心】【苍白】的牌面显示与悬停词条（20260729 需求）。
///
/// 需求："失心""苍白"要显示在牌面上，并添加 HoverTips
///       （哪怕消耗、虚无、重放本身就会加在牌面）。
///
/// 实现说明（给未来的你/维护者）：
/// 失心/苍白不是原版 CardKeyword 枚举成员，无法走原版关键词的自动上牌面流程，
/// 所以用两个 Harmony Postfix 补齐：
/// 1. 牌面文字：CardModel.GetDescriptionForPile(PileType, Creature?) 是所有牌面
///    描述的最终出口（NCard 渲染手牌/牌堆/查看界面都调它），Postfix 在返回的
///    描述文本前面插入"[gold]失心[/gold]。"/"[gold]苍白[/gold]。"一行，
///    与原版关键词行（GetCardText = [gold]标题[/gold]+句号）完全同款式。
///    升级预览走的 GetDescriptionForUpgradePreview 也一并补上。
/// 2. 悬停词条：CardModel.HoverTips 属性 getter 是 UI（NCardHolder /
///    NPreviewCardHolder）读取词条的唯一入口，Postfix 往返回序列尾部追加
///    ModHoverTips.Lost / Pale 词条，文案在 static_hover_tips.json。
///
/// 刷新时机说明：ApplyLost/ApplyPale 都会调用 AddKeyword/RemoveKeyword
/// （消耗/虚无），会触发 KeywordsChanged → NCard 重新渲染描述，
/// 所以附加特质后牌面文字通常能即时刷新；个别不动关键词的路径
/// （如给已带消耗的牌上失心）也会因费用变化事件触发重绘。
///
/// 修改指南：
/// - 想改牌面显示的字样/颜色：改下面的 LostCardText / PaleCardText 常量。
/// - 想改词条文案：改 localization/zhs/static_hover_tips.json 的
///   PALEREGENTMODV1-LOST / PALEREGENTMODV1-PALE 条目。
/// </summary>
[HarmonyPatch(typeof(CardModel))]
internal static class TraitCardTextPatch
{
    /// <summary>牌面上的"失心"行（与原版关键词行同款式：金色 + 句号）。</summary>
    private const string LostCardText = "[gold]失心[/gold]。";

    /// <summary>牌面上的"苍白"行。</summary>
    private const string PaleCardText = "[gold]苍白[/gold]。";

    /// <summary>
    /// 把失心/苍白行插到描述文本最前面（与原版 beforeDescription 关键词
    /// 如"消耗/虚无"排在描述前的习惯一致；两者都有时失心在前——
    /// 实际机制上二者互斥，同时出现说明有 bug，正好能看出来）。
    /// </summary>
    private static string PrependTraitLines(CardModel card, string text)
    {
        List<string> lines = new List<string>();
        if (CardTraits.IsLost(card)) lines.Add(LostCardText);
        if (CardTraits.IsPale(card)) lines.Add(PaleCardText);
        if (lines.Count == 0) return text;
        if (!string.IsNullOrEmpty(text)) lines.Add(text);
        return string.Join('\n', lines);
    }

    /// <summary>牌面描述出口 1：手牌/牌堆/查看界面。</summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CardModel.GetDescriptionForPile),
        typeof(PileType), typeof(Creature))]
    private static void AppendToPileDescription(
        CardModel __instance, ref string __result)
    {
        __result = PrependTraitLines(__instance, __result);
    }

    /// <summary>牌面描述出口 2：升级预览。</summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CardModel.GetDescriptionForUpgradePreview))]
    private static void AppendToUpgradePreview(
        CardModel __instance, ref string __result)
    {
        __result = PrependTraitLines(__instance, __result);
    }

    /// <summary>
    /// 悬停词条出口：带失心/苍白的牌，词条列表末尾追加对应词条。
    /// 哪怕消耗/虚无/重放的词条已经在列表里，也照常追加失心/苍白（需求明确要求）。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CardModel.HoverTips), MethodType.Getter)]
    private static void AppendTraitHoverTips(
        CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        bool isLost = CardTraits.IsLost(__instance);
        bool isPale = CardTraits.IsPale(__instance);
        if (!isLost && !isPale) return;

        List<IHoverTip> tips = __result.ToList();
        if (isLost) tips.Add(ModHoverTips.Lost);
        if (isPale) tips.Add(ModHoverTips.Pale);
        __result = tips;
    }
}
