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
/// 【纯粹封印】debuff（机制文档：新增负面效果）。
/// 效果：[层数] 回合内，持有者每回合的第一次攻击不造成伤害。
/// 每经过持有者一方的回合结束，层数 -1；层数归零后消失。
///
/// 实现说明：
/// - ModifyDamageMultiplicative 返回 0 把伤害清零（参考原版 IntangiblePower 思路）；
///   只有"本回合还没触发过"时才清零，触发后本回合的后续攻击不受影响。
/// - 一次攻击牌的多段伤害（如多次打击）视为一次攻击（用 CardPlay 判定），
///   monster 无 cardPlay 时按单次 AttackCommand 处理。
/// </summary>
public class PureSealPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>本回合是否已封印过一次攻击。</summary>
    private bool _sealedThisTurn;

    /// <summary>正在封印的那次卡牌打出（多段攻击视为同一次）。</summary>
    private CardPlay? _sealingCardPlay;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner)
        {
            return 1m;
        }
        if (!props.IsPoweredAttack())
        {
            return 1m;
        }
        // 同一次卡牌打出的多段伤害都封印
        if (_sealedThisTurn)
        {
            return (cardPlay != null && cardPlay == _sealingCardPlay) ? 0m : 1m;
        }
        _sealedThisTurn = true;
        _sealingCardPlay = cardPlay;
        Flash();
        return 0m;
    }

    /// <summary>持有者一方回合结束：层数 -1，复位"本回合已封印"标记。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }
        _sealedThisTurn = false;
        _sealingCardPlay = null;
        await PowerCmd.Decrement(this);
    }
}
