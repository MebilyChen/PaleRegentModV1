using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

//这里游戏自己的回合后结束还在手牌的结算会让该牌触发后自动进入弃牌堆，不好打补丁，故暂时不处理了

/*[HarmonyPatch(typeof(CardPileCmd))]
public static class InfectionTurnEndReturnPatch
{
    [HarmonyPatch(
        nameof(CardPileCmd.Add),
        new[]
        {
            typeof(CardModel),
            typeof(PileType),
            typeof(CardPilePosition),
            typeof(AbstractModel),
            typeof(bool)
        })]
    [HarmonyPrefix]
    private static void CardPileAddPrefix(
        CardModel __0,
        ref PileType __1)
    {
        if (__0 is not Infection)
            return;

        Console.WriteLine(
            $"[InfectionReturn] Add called: pile={__1}");

        if (__1 != PileType.Discard)
            return;

        if (!Infection.TryConsumeTurnEndReturn(__0))
        {
            Console.WriteLine(
                "[InfectionReturn] Infection had no pending return mark.");
            return;
        }

        Console.WriteLine(
            "[InfectionReturn] DISCARD -> HAND");

        __1 = PileType.Hand;
    }
}*/