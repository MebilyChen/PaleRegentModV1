using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 每场新战斗开始时清空灵魂双刃专用统计。
///
/// 多人只清一次整个账本，
/// 随后每个 Player 分别重新累计。
/// </summary>
[HarmonyPatch(
    typeof(Hook),
    nameof(Hook.BeforeCombatStart))]
internal static class SoulBladesCombatResetPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        SoulBladesEnergyTracker.ResetAll();
    }
}