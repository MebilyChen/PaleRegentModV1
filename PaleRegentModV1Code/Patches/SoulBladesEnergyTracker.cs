using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 【灵魂双刃】专用战斗资源获得统计。
///
/// 和旧 CombatCounters 不同：
///
/// 1. 统计的是“点数”，不是“获得次数”；
/// 2. 按 Player 分开保存；
/// 3. A 玩家获得资源不会计入 B 玩家；
/// 4. 只服务 SoulBlades，不修改其他卡牌的旧统计口径。
/// </summary>
internal static class SoulBladesEnergyTracker
{
    private sealed class PlayerCounter
    {
        public int SoulGained;
        public int VoidGained;

        public int Total =>
            SoulGained + VoidGained;
    }

    private static readonly object Gate = new();

    /// <summary>
    /// 当前战斗中的玩家 → 灵魂双刃资源账本。
    ///
    /// 使用 ConditionalWeakTable 避免为了统计器长期持有 Player。
    /// 战斗开始时仍会主动 ResetAll。
    /// </summary>
    private static ConditionalWeakTable<Player, PlayerCounter> _states =
        new();

    /// <summary>
    /// 当前玩家主动获得灵魂。
    /// </summary>
    public static void AddSoul(
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
    /// 当前玩家获得虚空。
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
    /// 新战斗开始时清空所有玩家的灵魂双刃统计。
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
