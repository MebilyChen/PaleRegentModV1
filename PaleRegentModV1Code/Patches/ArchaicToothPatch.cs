using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 为原版“古老牙齿”补充苍白君主的定向先古化：
/// VoidStrike（虚空打击） -> DarkTide（黑暗潮汐）。
/// </summary>
public static class ArchaicToothPatch
{
    /// <summary>
    /// 原版只会在内置映射中查找起始卡。本补丁在原版未找到时，
    /// 让“虚空打击”成为古老牙齿可识别的起始卡。
    /// </summary>
    [HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceStarterCard")]
    [HarmonyPostfix]
    private static void FindVoidStrikePostfix(Player player, ref CardModel? __result)
    {
        if (__result != null)
        {
            return;
        }

        __result = player.Deck.Cards.FirstOrDefault(card => card is VoidStrike);
    }

    /// <summary>
    /// 将被古老牙齿选中的虚空打击替换为黑暗潮汐，
    /// 并与原版行为一致地保留强化等级和附魔。
    /// </summary>
    [HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceTransformedCard")]
    [HarmonyPostfix]
    private static void TransformVoidStrikePostfix(
        CardModel __0,
        ref CardModel __result)
    {
        CardModel starterCard = __0;

        if (starterCard is not VoidStrike)
        {
            return;
        }

        CardModel darkTide = ModelDb.Card<DarkTide>();

        if (darkTide == null)
        {
            Log.Error(
                "[PaleRegentModV1] DarkTide 尚未注册，" +
                "无法完成古老牙齿先古化。"
            );
            return;
        }

        CardModel transformedCard = starterCard.Owner.RunState.CreateCard(
            darkTide,
            starterCard.Owner);

        if (starterCard.IsUpgraded)
        {
            CardCmd.Upgrade(transformedCard);
        }

        if (starterCard.Enchantment != null)
        {
            EnchantmentModel enchantment =
                (EnchantmentModel)starterCard.Enchantment.MutableClone();
            CardCmd.Enchant(enchantment, transformedCard, enchantment.Amount);
        }

        __result = transformedCard;

        Log.Info(
            "[PaleRegentModV1] 古老牙齿：" +
            "VoidStrike -> DarkTide"
        );
    }

    /// <summary>
    /// 将黑暗潮汐登记为“古老牙齿的定向先古化目标”。
    /// 原版 DustyTome 会排除 ArchaicTooth.TranscendenceCards 中的卡，
    /// 因此黑暗潮汐不会再被尘封魔典作为普通先古牌随机获得。
    /// </summary>
    [HarmonyPatch(typeof(ArchaicTooth), "TranscendenceCards", MethodType.Getter)]
    [HarmonyPostfix]
    private static void AddDarkTideToTranscendenceCardsPostfix(
        ref List<CardModel> __result)
    {
        CardModel darkTide = ModelDb.Card<DarkTide>();

        if (darkTide == null)
        {
            Log.Error(
                "[PaleRegentModV1] DarkTide 尚未注册，" +
                "无法将其加入古老牙齿先古化目标列表。"
            );
            return;
        }

        if (__result.Any(card => card.Id == darkTide.Id))
        {
            return;
        }

        __result.Add(darkTide);
    }
}
