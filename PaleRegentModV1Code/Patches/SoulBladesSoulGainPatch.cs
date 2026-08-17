using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 复用现有 CombatCounters.NotifySoulGain 的埋点，
/// 但额外为灵魂双刃记录：
///
///     谁获得的 + 获得了多少点。
///
/// 不修改 CombatCounters 原行为。
/// </summary>
[HarmonyPatch(
    typeof(CombatCounters),
    nameof(CombatCounters.NotifySoulGain))]
internal static class SoulBladesSoulGainPatch
{
    [HarmonyPrefix]
    private static void Prefix(
        Player? player,
        int amount)
    {
        if (player == null || amount <= 0)
        {
            return;
        }

        SoulBladesEnergyTracker.AddSoul(
            player,
            amount);
    }
}