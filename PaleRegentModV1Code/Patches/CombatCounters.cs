using MegaCrit.Sts2.Core.Entities.Players;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 本场战斗全局计数器（20260727 批次新增）。
///
/// 为以下卡牌提供统计口径：
/// - 【灵魂双刃 SoulBlades C#57】：本场战斗中灵魂及虚空能量的"获得次数"总和；
/// - 【虚空回声 VoidEcho C#64】：本场战斗中生成过的虚空"总量"；
/// - 【共鸣一击 ResonantStrike C#63】：本回合生成的虚空/灵魂点数。
///
/// 统计口径说明（已在条目后备注，不改表格原文）：
/// - 虚空侧：由 VoidPowerListener.AfterSecondaryResourceChanged 统一埋点，
///   任何正增量记 1 次"获得次数"并累计总量，覆盖所有获取途径；
/// - 灵魂侧：引擎没有全局"获得能量"钩子，回合开始的常规能量重置不计入
///   （否则每回合白+1 次），只统计卡牌/能力主动 GainEnergy 的部分，
///   由本 mod 代码在调用 PlayerCmd.GainEnergy 处调用 NotifySoulGain 埋点。
///   如需把回合重置也算作"获得"，可在 AfterEnergyReset 里补一次埋点。
///
/// 生命周期：所有计数在战斗结束（虚空资源 Reset 回调）时清零，
/// 回合级计数由 PaleToken.AfterEnergyReset → ResetTurn() 清零。
/// </summary>
public static class CombatCounters
{
    /// <summary>本场战斗中虚空能量的获得次数（每次正增量记 1 次）。</summary>
    public static int VoidGainCountThisCombat { get; private set; }

    /// <summary>本场战斗中生成过的虚空总量（只统计正增量）。</summary>
    public static int VoidGainedThisCombat { get; private set; }

    /// <summary>本场战斗中灵魂能量的获得次数（mod 内 GainEnergy 埋点）。</summary>
    public static int SoulGainCountThisCombat { get; private set; }

    /// <summary>本回合生成的灵魂点数（mod 内 GainEnergy 埋点，供共鸣一击）。</summary>
    public static int SoulGainedThisTurn { get; private set; }

    /// <summary>灵魂+虚空获得次数总和（灵魂双刃 C#57 的伤害基数）。</summary>
    public static int TotalEnergyGainCount => VoidGainCountThisCombat + SoulGainCountThisCombat;

    /// <summary>本场战斗生成过的【感染】张数（病态辐射 C#70），由 Infection.NotifyGenerated 埋点。</summary>
    public static int InfectionGeneratedThisCombat { get; private set; }

    /// <summary>
    /// 当前感染计数所属的战斗对象。
    /// 用引用身份区分不同 CombatState，避免静态计数跨战斗残留。
    /// </summary>
    private static object? _infectionCombatIdentity;

    /// <summary>
    /// 确保感染计数属于指定战斗；战斗对象变化时自动清零。
    /// </summary>
    public static void EnsureInfectionCombat(object? combatIdentity)
    {
        if (combatIdentity is null)
        {
            return;
        }

        if (ReferenceEquals(_infectionCombatIdentity, combatIdentity))
        {
            return;
        }

        _infectionCombatIdentity = combatIdentity;
        InfectionGeneratedThisCombat = 0;
    }

    /// <summary>
    /// 感染生成埋点。必须传入生成发生时所属的 CombatState。
    /// </summary>
    public static void NotifyInfectionGenerated(object? combatIdentity,int count)
    {
        EnsureInfectionCombat(combatIdentity);

        if (count > 0)
        {
            InfectionGeneratedThisCombat += count;
        }
    }

    /// <summary>
    /// 兼容旧调用；建议所有 Infection.NotifyGenerated 调用迁移到带 combatIdentity 的重载。
    /// </summary>
    public static void NotifyInfectionGenerated(int count)
    {
        if (count > 0)
        {
            InfectionGeneratedThisCombat += count;
        }
    }

    /// <summary>虚空正增量埋点（由 VoidPowerListener 调用）。</summary>
    public static void NotifyVoidGain(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        VoidGainCountThisCombat++;
        VoidGainedThisCombat += amount;
    }

    /// <summary>
    /// 灵魂获得埋点：mod 内所有"获得灵魂（能量）"的卡牌/能力，
    /// 在调用 PlayerCmd.GainEnergy 后调用一次本方法。
    /// </summary>
    public static void NotifySoulGain(Player? player, int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        SoulGainCountThisCombat++;
        SoulGainedThisTurn += amount;
    }

    /// <summary>回合级计数清零（玩家回合开始时由 PaleToken.AfterEnergyReset 调用）。</summary>
    public static void ResetTurn()
    {
        SoulGainedThisTurn = 0;
    }

    /// <summary>战斗级计数清零（战斗结束时由 VoidPowerListener 的 Reset 回调调用）。</summary>
    public static void ResetCombat()
    {
        VoidGainCountThisCombat = 0;
        VoidGainedThisCombat = 0;
        SoulGainCountThisCombat = 0;
        SoulGainedThisTurn = 0;
        InfectionGeneratedThisCombat = 0;
        _infectionCombatIdentity = null;
    }
}
