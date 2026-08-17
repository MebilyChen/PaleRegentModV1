using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【蓄灵】buff（弃壳等卡施加，名词表 N#10）。
/// 效果：能力存在期间，玩家生命值不会降至 0；下回合开始时获得 [层数] 点灵魂
/// （能量），随后消失。
///
/// 供灵挂在 AfterEnergyResetLate：先由普通 AfterEnergyReset 完成能量重置、
/// 苍白信物/国王之魂按虚空扣减恢复量，再由蓄灵额外给予灵魂。因此虚空只影响
/// “回合开始恢复”的灵魂，不影响蓄灵额外给予的灵魂。
/// 20260727：持有【白沃姆摇篮】（WhiteWyrmCradlePower，C#97）时，
/// 供灵后不再移除自身（蓄灵不消失，每回合持续供灵，且持续提供濒死保护）。
/// </summary>
public class SoulNextTurnPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 在实际生命损失结算前封顶损失值，使蓄灵持有者至少保留 1 点生命。
    /// 此钩子接收的是经过格挡后的未格挡伤害／生命损失，因此同时覆盖普通伤害
    /// 与通过标准结算流程产生的直接失血。
    /// </summary>
    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m)
        {
            return amount;
        }

        // 已经只剩 1 点生命时，阻止后续任何生命损失；否则最多损失到 1 点生命。
        if (Owner.CurrentHp <= 1)
        {
            Flash();
            return 0m;
        }

        decimal maximumLoss = Owner.CurrentHp - 1;
        return amount > maximumLoss ? maximumLoss : amount;
    }

    /// <summary>
    /// 在所有普通 AfterEnergyReset 处理完成后供灵，避免被虚空恢复扣减影响。
    /// </summary>
    public override async Task AfterEnergyResetLate(Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        if (Amount > 0)
        {
            await PlayerCmd.GainEnergy(Amount, player);
        }

        // 20260727：白沃姆摇篮在场时蓄灵不消失（C#97）。
        // 因此濒死保护也会随蓄灵持续存在。
        if (Owner.HasPower<WhiteWyrmCradlePower>())
        {
            return;
        }

        await PowerCmd.Remove(this);
    }
}
