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
/// 【苦痛之路】debuff（机制文档：新增负面效果，一般为 5 层）。
/// 效果（表格设计版）：本回合内持有者累计造成 ≥ [层数] 点伤害时立即解除；
/// 在“意图为攻击”的回合结束时（玩家 = 本回合造成过伤害），若效果仍存在，
/// 受到等同于自己当前生命值的伤害（可被格挡/缓冲）；
/// 未解除时效果持续到后续回合（不自动移除），伤害计数每回合重置。
///
/// 实现说明：
/// - AfterDamageGiven 累计持有者本回合实际造成的伤害，达标立即 Remove；
/// - AfterSideTurnEnd：本回合造成过伤害（= 攻击回合）且效果仍在 →
///   按当前生命值打一刀（不带 Unblockable，可被格挡），效果保留；
///   非攻击回合不触发自伤，效果同样保留，伤害计数清零。
/// </summary>
public class PathOfPainPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>本回合持有者累计造成的伤害。</summary>
    private decimal _damageDealtThisTurn;

    /// <summary>本回合持有者是否造成过伤害（判定“意图为攻击的回合”）。</summary>
    private bool _attackedThisTurn;

    /// <summary>累计持有者造成的伤害；达标立即解除。</summary>
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || target == Owner)
        {
            return;
        }
        _attackedThisTurn = true;
        _damageDealtThisTurn += result.TotalDamage;
        if (_damageDealtThisTurn >= Amount)
        {
            // 达标：立即解除苦痛之路
            Flash();
            await PowerCmd.Remove(this);
        }
    }

    /// <summary>持有者一方回合结束：攻击回合未达标 → 自伤；效果保留到下回合。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }
        if (_attackedThisTurn && _damageDealtThisTurn < Amount && Owner.IsAlive)
        {
            // 攻击了却未走完苦痛之路：受到等同当前生命值的伤害（可被格挡）
            Flash();
            await CreatureCmd.Damage(choiceContext, Owner, Owner.CurrentHp, ValueProp.Unpowered, null, cardPlay: null);
        }
        // 效果不自动移除，每回合重置计数
        _damageDealtThisTurn = 0m;
        _attackedThisTurn = false;
        await Task.CompletedTask;
    }
}
