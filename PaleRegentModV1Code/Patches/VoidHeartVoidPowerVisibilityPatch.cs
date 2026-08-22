using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using PaleRegentModV1.PaleRegentModV1Code.Relics;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 虚空之心持有者仍可累积 VoidResource，但不再创建或刷新 VoidPower 展示图标。
/// </summary>
[HarmonyPatch(typeof(VoidResource), nameof(VoidResource.SyncPower))]
public static class VoidHeartVoidPowerVisibilityPatch
{
    /// <summary>
    /// SyncPower 的第二个参数是目标玩家。对于返回 Task 的异步方法，
    /// 跳过原方法时必须显式提供已完成任务；否则调用方 await null 会中断战斗回合。
    /// </summary>
    [HarmonyPrefix]
    private static bool Prefix(Player __1, ref Task __result)
    {
        if (!__1.Relics.Any(relic => relic is VoidHeart))
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}