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
/// 【誓卫】buff（占位卡"誓卫"施加，机制文档：瘟疫流附属防御向）。
/// 效果：每回合你第一次失去生命时，获得 [层数] 点格挡。
///
/// 占位说明：数值取层数（卡牌施加 10 层 = +10 格挡），
/// 想改成固定数值可在这里写死。
/// </summary>
public class BulwarkOathPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _triggeredThisTurn;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || _triggeredThisTurn || result.UnblockedDamage <= 0)
        {
            return;
        }
        _triggeredThisTurn = true;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, cardPlay: null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }
        _triggeredThisTurn = false;
        await Task.CompletedTask;
    }
}
