/*using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

[HarmonyPatch(typeof(NEnergyCounter), "_Ready")]
public static class logTest
{
    [HarmonyPostfix]
    public static void Postfix(NEnergyCounter __instance)
    {
        var back =
            __instance.GetNodeOrNull<NParticlesContainer>("EnergyVfxBack");

        var front =
            __instance.GetNodeOrNull<NParticlesContainer>("EnergyVfxFront");
        

        GD.Print(
            $"[EnergyVfxTest] back={back?.GetType().FullName}, " +
            $"front={front?.GetType().FullName}"
        );

        if (back != null)
        {
            GD.Print("[EnergyVfxTest] FORCE Restart BACK");
            back.Restart();
        }

        if (front != null)
        {
            GD.Print("[EnergyVfxTest] FORCE Restart FRONT");
            front.Restart();
        }
    }
}*/