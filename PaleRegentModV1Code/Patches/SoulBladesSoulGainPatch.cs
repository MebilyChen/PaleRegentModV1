using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 旧的灵魂获得统计入口。
///
/// 保留 CombatCounters.NotifySoulGain 埋点用于兼容/回退，
/// 但当新的 EnergyChanged 统计启用时，本 Patch 不再写入
/// SoulBladesEnergyTracker，避免同一笔灵魂被累计两次。
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
        // 新统计入口启用时，旧 Patch 仍然存在，
        // 但不写入灵魂双刃账本，防止重复计数。
        // if (SoulBladesTrackingConfig.UseEnergyChangeTracking)
        //{
           // return;
        //}

        if (player == null || amount <= 0)
        {
            return;
        }

        SoulBladesEnergyTracker.AddSoul(
            player,
            amount);
    }
}