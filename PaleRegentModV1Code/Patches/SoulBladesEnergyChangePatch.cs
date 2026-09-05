using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 【灵魂双刃】监听玩家实际 Energy 变化。
///
/// 只统计实际增加：newAmount - oldAmount > 0。
/// 消费/丢失 Energy 不累计。
///
/// 使用 ConditionalWeakTable 做订阅去重：
/// 即使 PatchAll 被重复执行、构造器 Postfix 意外注册多次，
/// 同一个 PlayerCombatState 也只会挂一个 EnergyChanged 监听器。
/// </summary>
[HarmonyPatch(
    typeof(PlayerCombatState),
    MethodType.Constructor,
    new Type[] { typeof(Player) })]
internal static class SoulBladesEnergyChangePatch
{
    private sealed class SubscriptionMarker
    {
    }

    private static readonly object SubscriptionGate = new();

    private static readonly ConditionalWeakTable<
        PlayerCombatState,
        SubscriptionMarker> SubscribedStates = new();

    [HarmonyPostfix]
    private static void Postfix(
        PlayerCombatState __instance,
        Player __0)
    {
        lock (SubscriptionGate)
        {
            // 防止同一个 PlayerCombatState 重复订阅。
            if (SubscribedStates.TryGetValue(
                    __instance,
                    out _))
            {
                return;
            }

            SubscribedStates.Add(
                __instance,
                new SubscriptionMarker());
        }

        Player player = __0;

        __instance.EnergyChanged +=
            (oldAmount, newAmount) =>
            {
                int gained =
                    newAmount - oldAmount;

                if (gained <= 0)
                {
                    return;
                }

                SoulBladesEnergyTracker
                    .AddSoulFromEnergyChange(
                        player,
                        gained);
            };
    }
}