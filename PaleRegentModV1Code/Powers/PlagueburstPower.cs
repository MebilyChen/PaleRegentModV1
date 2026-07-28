using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Cards;
using System.Linq;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【疫爆】buff（效果表 P#22，卡牌 C#60 疫爆 施加）。
/// 效果：每生成 1 张【感染】，对所有敌人造成 [层数] 点伤害。
///
/// 实现说明：
/// - 挂 AfterCardGeneratedForCombat 全局生成钩子，判定 card is Infection；
/// - 伤害为 Power 触发伤害（非攻击牌），带 Unpowered 不吃力量加成，
///   与原版荆棘类效果口径一致；如需吃力量可去掉 ValueProp.Unpowered；
/// - 逐张结算：一次生成 2 张感染就触发 2 次全体伤害。
/// </summary>
public class PlagueburstPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card is not Infection)
        {
            return;
        }

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }

        Flash();
        ThrowingPlayerChoiceContext choiceContext = new ThrowingPlayerChoiceContext();
        foreach (Creature enemy in combatState.GetOpponentsOf(Owner).Where(c => c.IsAlive).ToList())
        {
            await CreatureCmd.Damage(choiceContext, enemy, Amount,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner);
        }
    }
}
