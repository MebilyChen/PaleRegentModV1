using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【苦痛之路】debuff。
///
/// Amount 表示本回合还需要造成多少点伤害。
///
/// 例如初始为 5 层：
/// 造成 3 点伤害后显示为 2 层；
/// 再造成 2 点伤害后立即移除。
///
/// 如果本回合造成过伤害，但回合结束时仍未清空层数，
/// 持有者受到等同于当前生命值的伤害。
///
/// 如果效果仍存在，则在持有者下一回合开始时，
/// 恢复到本回合开始前的层数。
///
/// UI：本 Power 不提供血条长度预览；存在时仅由
/// PathOfPainHealthBarSystem 在血条正中央显示 ⚠️ 图标。
/// </summary>
public class PathOfPainPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 本回合已经从 Amount 中扣除的层数。
    /// 下一回合开始时用于恢复。
    /// </summary>
    private int _amountReducedThisTurn;

    /// <summary>
    /// 持有者本回合是否造成过实际伤害。
    /// </summary>
    private bool _attackedThisTurn;

    /// <summary>
    /// 持有者造成伤害后，减少对应的显示层数。
    /// </summary>
    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        // 只处理 Power 持有者对其他目标造成的伤害事件。
        if (dealer != Owner || target == Owner)
        {
            return;
        }

        /*
         * 只要进入了伤害结算，就视为本回合攻击过。
         *
         * 即使最终伤害为 0，例如：
         * - 被格挡完全抵消；
         * - 被某些效果减伤到 0；
         *
         * 回合结束时仍然会触发苦痛之路的惩罚。
         */
        _attackedThisTurn = true;

        int damage = decimal.ToInt32(result.TotalDamage);

        /*
         * 0 点伤害算攻击过，但不减少层数。
         */
        if (damage <= 0)
        {
            return;
        }

        // 本次伤害足以清空剩余层数，立即移除。
        if (damage >= Amount)
        {
            Flash();
            await PowerCmd.Remove(this);
            return;
        }

        // 记录本回合减少的层数，下回合开始时恢复。
        _amountReducedThisTurn += damage;

        Flash();
        SetAmount(Amount - damage);
    }

    /// <summary>
    /// 持有者一方回合结束。
    ///
    /// 如果本回合造成过伤害，但 Power 仍未被清除，
    /// 则受到等同于当前生命值的伤害。
    /// </summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 只处理持有者所在阵营的回合结束。
        if (side != Owner.Side || !participants.Contains(Owner))
        {
            return;
        }

        if (_attackedThisTurn && Owner.IsAlive)
        {
            Flash();

            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                Owner.CurrentHp,
                ValueProp.Unpowered,
                null,
                cardPlay: null
            );
        }

        /*
         * 此处只重置攻击标记。
         *
         * 不要清空 _amountReducedThisTurn，
         * 因为下一回合开始时还需要用它恢复层数。
         */
        _attackedThisTurn = false;
    }

    /// <summary>
    /// 持有者下一回合开始时，恢复上一回合减少的层数。
    /// </summary>
    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        // 只处理持有者所在阵营的回合开始。
        if (side != Owner.Side || !participants.Contains(Owner))
        {
            return Task.CompletedTask;
        }

        if (_amountReducedThisTurn > 0)
        {
            /*
             * 例如：
             *
             * 初始为 5 层；
             * 上回合造成 3 点伤害；
             * 当前 Amount 为 2；
             * _amountReducedThisTurn 为 3；
             *
             * 恢复后：2 + 3 = 5。
             */
            SetAmount(Amount + _amountReducedThisTurn);
        }

        // 开始新一回合的统计。
        _amountReducedThisTurn = 0;
        _attackedThisTurn = false;

        return Task.CompletedTask;
    }
}
