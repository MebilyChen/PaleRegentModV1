using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 从现有 VoidPowerListener 的资源变化事件中，
/// 为灵魂双刃记录“对应玩家实际获得了多少虚空”。
///
/// 不修改 VoidPowerListener 原来的统计与 UI 同步逻辑。
/// </summary>
[HarmonyPatch(
    typeof(VoidPowerListener),
    nameof(VoidPowerListener.AfterSecondaryResourceChanged))]
internal static class SoulBladesVoidGainPatch
{
    [HarmonyPrefix]
    private static void Prefix(
        SecondaryResourceChangeContext context)
    {
        // 只关心虚空资源。
        if (context.Definition.Id != VoidResource.Id)
        {
            return;
        }

        Player? player = context.Player;

        if (player == null)
        {
            return;
        }

        // 花费 / 丢失不影响累计获得量。
        if (context.NewAmount <= context.OldAmount)
        {
            return;
        }

        int gained =
            (int)(context.NewAmount - context.OldAmount);

        if (gained <= 0)
        {
            return;
        }

        SoulBladesEnergyTracker.AddVoid(
            player,
            gained);
    }
}