using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Events;
using PaleRegentModV1.PaleRegentModV1Code.Character;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

[HarmonyPatch(typeof(ColorfulPhilosophers), "GenerateInitialOptions")]
internal static class ColorfulPhilosophersPatch
{
    private static readonly MethodInfo OfferRewardsMethod =
        AccessTools.Method(
            typeof(ColorfulPhilosophers),
            "OfferRewards",
            new[] { typeof(CardPoolModel) }
        );

    [HarmonyPrefix]
    private static bool GenerateInitialOptionsPrefix(
        ColorfulPhilosophers __instance,
        ref IReadOnlyList<EventOption> __result)
    {
        var initialOptions = new List<EventOption>();

        CharacterModel character = __instance.Owner.Character;
        List<CardPoolModel> unlockedPools =
            __instance.Owner.UnlockState.CharacterCardPools.ToList();

        // 五个原版颜色，完全照原版顺序。
        CardPoolModel[] vanillaPools =
        {
            ModelDb.CardPool<NecrobinderCardPool>(),
            ModelDb.CardPool<IroncladCardPool>(),
            ModelDb.CardPool<RegentCardPool>(),
            ModelDb.CardPool<SilentCardPool>(),
            ModelDb.CardPool<DefectCardPool>()
        };

        foreach (CardPoolModel pool in vanillaPools)
        {
            if (character.CardPool == pool || !unlockedPools.Contains(pool))
                continue;

            CardPoolModel capturedPool = pool;

            initialOptions.Add(
                new EventOption(
                    __instance,
                    () => OfferRewards(__instance, capturedPool),
                    "COLORFUL_PHILOSOPHERS.pages.INITIAL.options."
                    + capturedPool.EnergyColorName.ToUpperInvariant(),
                    Array.Empty<IHoverTip>()
                )
            );
        }

        // Pale Regent 作为第六个候选加入同一个列表。
        CardPoolModel palePool =
            ModelDb.CardPool<PaleRegentModV1CardPool>();

        if (character.CardPool != palePool &&
            unlockedPools.Contains(palePool))
        {
            initialOptions.Add(
                new EventOption(
                    __instance,
                    () => OfferRewards(__instance, palePool),
                    "COLORFUL_PHILOSOPHERS.pages.INITIAL.options.PALEREGENT",
                    Array.Empty<IHoverTip>()
                )
            );
        }

        // 完全照原版：随机删到最多 3 个。
        int targetCount = Mathf.Min(3, initialOptions.Count);

        while (initialOptions.Count > targetCount)
        {
            initialOptions.RemoveAt(
                __instance.Rng.NextInt(initialOptions.Count)
            );
        }

        __result = initialOptions;

        // 不再执行原版 GenerateInitialOptions。
        return false;
    }

    private static Task OfferRewards(
        ColorfulPhilosophers instance,
        CardPoolModel pool)
    {
        return (Task)OfferRewardsMethod.Invoke(
            instance,
            new object[] { pool }
        )!;
    }
}
