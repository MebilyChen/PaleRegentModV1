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
/// 效果：本回合内持有者累计造成 ≥ [层数] 点伤害则解除该效果；
/// 否则在持有者一方的回合结束时，受到等同于自己当前生命值的伤害（可被格挡）。
///
/// 实现说明：
/// - AfterDamageGiven 累计持有者本回合实际造成的 HP 伤害（result.HpDamage）；
/// - AfterSideTurnEnd 判定：达标 → Remove；未达标 → 按当前生命值打一刀
///   （不带 Unblockable，可被格挡），然后同样 Remove（一次性判定）。
///
/// 修改指南：想让它持续多回合判定，把未达标分支里的 Remove 改成
/// 重置 _damageDealtThisTurn 即可。
/// </summary>
public class PathOfPainPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>本回合持有者累计造成的伤害。</summary>
    private decimal _damageDealtThisTurn;

    /// <summary>累计持有者造成的 HP 伤害。</summary>
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || target == Owner)
        {
            return;
        }
        _damageDealtThisTurn += result.TotalDamage;
        await Task.CompletedTask;
    }

    /// <summary>持有者一方回合结束：判定是否走完苦痛之路。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }
        Flash();
        if (_damageDealtThisTurn < Amount && Owner.IsAlive)
        {
            // 未达标：受到等同当前生命值的伤害（可被格挡）
            await CreatureCmd.Damage(choiceContext, Owner, Owner.CurrentHp, ValueProp.Unpowered, null, cardPlay: null);
        }
        await PowerCmd.Remove(this);
    }
}
