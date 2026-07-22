using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.RechargeEnergy))]
internal static class VoidEnergyPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance.Character is Character.PaleRegentModV1)
        {
            int currentVoid = VoidResource.Get(__instance);
            if (currentVoid > 0)
            {
                __instance.Energy -= currentVoid;
                if (__instance.Energy < 0)
                {
                    __instance.Energy = 0;
                }
            }
        }
    }
}
