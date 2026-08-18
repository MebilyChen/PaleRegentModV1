using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 将【失心】提供的临时重放加成限制在当前战斗。
/// 每场战斗结束后恢复为失心的基础重放 2。
/// </summary>
[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.AfterCombatEnd))]
public static class LostReplayCombatResetPatch
{
    [HarmonyPostfix]
    private static void ResetLostReplayCountAfterCombat()
    {
        CardTraits.ResetLostReplayCount();
    }
}
