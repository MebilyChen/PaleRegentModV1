using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;
using PaleRegentModV1.PaleRegentModV1Code.Patches;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 苍白信物（Pale Token）—— 初始遗物。
/// 效果：
/// 1. 灵魂（能量）上限 +1。通过 ModifyMaxEnergy 钩子实现，参考原版 Bread。
/// 2. 你的每回合开始时，只能恢复［灵魂上限 - 虚空］数量的灵魂。
///    实现方式：回合开始时引擎先把能量重置为上限（ResetEnergy），
///    然后在 AfterEnergyReset 钩子里按当前虚空数量扣除等量灵魂，
///    等效于"只恢复了［灵魂-虚空］点"。虚空数量以 VoidResource 副资源为准。
///
/// 注意：回合开始扣灵魂的逻辑此前临时写在 VoidPower.AfterEnergyReset 里，
/// 本次已把该逻辑移交给遗物（苍白信物/国王之魂），VoidPower 只作为层数展示，
/// 避免遗物和 Power 双重扣除。
/// </summary>
public class PaleToken : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    /// <summary>灵魂上限加成：苍白信物 +1。</summary>
    protected virtual int MaxEnergyBonus => 1;

    /// <summary>
    /// 恢复补偿：国王之魂在扣虚空时少扣 1 点（恢复[灵魂-虚空+1]），苍白信物为 0。
    /// </summary>
    protected virtual int RecoveryBonus => 0;

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != Owner)
        {
            return amount;
        }
        return amount + MaxEnergyBonus;
    }

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner)
        {
            return;
        }

        // 回合级计数器：异色与共鸣一击仍按原规则在每回合开始清零。
        VoidPowerListener.ResetTurnGain();
        CombatCounters.ResetTurn();

        // 引擎已经先执行 ResetEnergy：此时能量为本回合重置后的灵魂上限。
        // 再按照当前虚空量扣除，得到本回合实际可用/恢复的灵魂。
        int reduction = VoidResource.Get(player) - RecoveryBonus;
        if (reduction > 0)
        {
            Flash();
            await PlayerCmd.LoseEnergy(reduction, player);
        }

        // 关键新增：在扣除虚空限制后，记录本回合实际恢复到的灵魂点数。
        // PlayerCombatState.Energy 是当前回合实际可用的灵魂数；取正数避免异常值入账。
        int recoveredSoul = player.PlayerCombatState?.Energy ?? 0;
        SoulBladesEnergyTracker.AddSoul(player, recoveredSoul);
    }

    /// <summary>
    /// 20260725：战斗结束时结算【模具】遗物（名词表 N#9）。
    /// 挂在初始遗物上是因为苍白信物/国王之魂必存在且二选一，
    /// 不会重复结算。
    /// </summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await MouldHelper.RollMouldRelics(Owner, room);
    }
}
