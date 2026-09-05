using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 【灵魂双刃】灵魂统计入口。
///
/// 默认使用 ActualEnergyChange：
/// - 旧 SoulBladesSoulGainPatch 可以继续保留；
/// - 旧 Patch 调用 AddSoul(...) 时不会写入；
/// - 只有 EnergyChanged 入口调用 AddSoulFromEnergyChange(...) 会写入。
///
/// 如果以后需要回退旧逻辑，只改 SoulTrackingMode 即可。
/// </summary>
internal static class SoulBladesEnergyTracker
{
    internal enum SoulTrackingMode
    {
        LegacyNotifySoulGain,
        ActualEnergyChange
    }

    /// <summary>
    /// 默认：按玩家实际 Energy 增加统计。
    /// 改为 LegacyNotifySoulGain 即可恢复旧 NotifySoulGain 口径。
    /// </summary>
    public static SoulTrackingMode TrackingMode { get; set; } =
        SoulTrackingMode.ActualEnergyChange;

    private sealed class PlayerCounter
    {
        public int SoulGained;
        public int VoidGained;

        public int Total =>
            SoulGained + VoidGained;
    }

    private static readonly object Gate = new();

    private static ConditionalWeakTable<Player, PlayerCounter> _states =
        new();

    /// <summary>
    /// 旧入口：CombatCounters.NotifySoulGain 使用。
    ///
    /// 代码保留，但默认 ActualEnergyChange 模式下不会写账本，
    /// 从根源避免“旧 Notify + 新 EnergyChanged”重复累计。
    /// </summary>
    public static void AddSoul(
        Player? player,
        int amount)
    {
        if (TrackingMode != SoulTrackingMode.LegacyNotifySoulGain)
        {
            return;
        }

        AddSoulCore(player, amount);
    }

    /// <summary>
    /// 新入口：实际 EnergyChanged 使用。
    /// 只在 ActualEnergyChange 模式下写账本。
    /// </summary>
    public static void AddSoulFromEnergyChange(
        Player? player,
        int amount)
    {
        if (TrackingMode != SoulTrackingMode.ActualEnergyChange)
        {
            return;
        }

        AddSoulCore(player, amount);
    }

    private static void AddSoulCore(
        Player? player,
        int amount)
    {
        if (player == null || amount <= 0)
        {
            return;
        }

        lock (Gate)
        {
            PlayerCounter counter =
                _states.GetValue(
                    player,
                    _ => new PlayerCounter());

            counter.SoulGained += amount;
        }
    }

    /// <summary>
    /// 虚空入口保持原逻辑。
    /// </summary>
    public static void AddVoid(
        Player? player,
        int amount)
    {
        if (player == null || amount <= 0)
        {
            return;
        }

        lock (Gate)
        {
            PlayerCounter counter =
                _states.GetValue(
                    player,
                    _ => new PlayerCounter());

            counter.VoidGained += amount;
        }
    }

    public static int GetSoul(Player? player)
    {
        if (player == null)
        {
            return 0;
        }

        lock (Gate)
        {
            return _states.TryGetValue(
                player,
                out PlayerCounter? counter)
                    ? counter.SoulGained
                    : 0;
        }
    }

    public static int GetVoid(Player? player)
    {
        if (player == null)
        {
            return 0;
        }

        lock (Gate)
        {
            return _states.TryGetValue(
                player,
                out PlayerCounter? counter)
                    ? counter.VoidGained
                    : 0;
        }
    }

    public static int GetTotal(Player? player)
    {
        if (player == null)
        {
            return 0;
        }

        lock (Gate)
        {
            return _states.TryGetValue(
                player,
                out PlayerCounter? counter)
                    ? counter.Total
                    : 0;
        }
    }

    /// <summary>
    /// 新战斗开始时清空所有玩家统计。
    /// TrackingMode 不重置，方便全局选择统计策略。
    /// </summary>
    public static void ResetAll()
    {
        lock (Gate)
        {
            _states =
                new ConditionalWeakTable<Player, PlayerCounter>();
        }
    }
}
