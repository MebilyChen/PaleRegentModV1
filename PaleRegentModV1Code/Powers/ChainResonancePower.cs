using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【连锁共鸣】buff（效果表 P#23，卡牌 C#62 连连看 / C#71 连锁反应 共用）。
/// 效果：本回合内连续打出同名牌时，从第 2 张起每张对所有敌人造成 [层数] 点伤害。
///
/// 两张卡的差异（备注：表格要求两卡共用一个 Power）：
/// - 【连连看 C#62】：效果仅"本回合"有效 → 施加的层数计入 _tempAmount，
///   回合结束时扣除（若扣完则整个 Power 移除）；
/// - 【连锁反应 C#71】：能力牌，效果持续整场战斗 → 正常叠 Amount。
///
/// "连续打出同名牌"的判定口径（已在条目后备注）：
/// 紧接着上一张打出的牌与其同名才算"连续"，中间插入其他牌则中断重新计数；
/// 每连续 1 次（即链条中第 2、3、4... 张）各触发一次全体伤害。
/// </summary>
public class ChainResonancePower : PaleRegentModV1Power
{
    /// <summary>本回合限定的层数部分（连连看 C#62 施加，回合结束扣除）。</summary>
    private decimal _tempAmount;

    /// <summary>上一张打出的牌的 ModelId（用于判断"连续同名"，同 ModelId 即同名），回合结束清空。</summary>
    private MegaCrit.Sts2.Core.Models.ModelId? _lastCardId;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>把本次施加的层数标记为"本回合限定"（连连看打出后调用）。</summary>
    public void MarkTemporary(decimal amount)
    {
        _tempAmount += amount;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 只统计持有者自己打出的牌
        if (cardPlay.Player != Owner.Player)
        {
            return;
        }

        MegaCrit.Sts2.Core.Models.ModelId cardId = cardPlay.Card.Id;
        bool isChain = _lastCardId != null && _lastCardId.Equals(cardId);
        _lastCardId = cardId;

        if (!isChain)
        {
            return;
        }

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null || Amount <= 0)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in combatState.GetOpponentsOf(Owner).Where(c => c.IsAlive).ToList())
        {
            await CreatureCmd.Damage(choiceContext, enemy, Amount,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner);
        }
    }

    /// <summary>回合结束：扣除本回合限定部分，并重置连击记录。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, System.Collections.Generic.IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        _lastCardId = null;

        if (_tempAmount <= 0)
        {
            return;
        }

        decimal expiring = _tempAmount;
        _tempAmount = 0;
        if (Amount - expiring <= 0)
        {
            await PowerCmd.Remove(this);
        }
        else
        {
            // 没有 PowerCmd.Reduce，用 ModifyAmount 负偏移量扣除回合限定层数
            await PowerCmd.ModifyAmount(choiceContext, this, -expiring, Owner, null);
        }
    }
}
