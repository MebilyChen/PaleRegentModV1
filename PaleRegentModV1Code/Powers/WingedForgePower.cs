using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Commands;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【有翼佣卫锻造】buff（能力牌"有翼佣卫"施加，机制文档：造物流）。
/// 效果：每回合开始时，将 [层数] 张【有翼佣卫】加入手牌。
/// </summary>
public class WingedForgePower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Owner.Player == null)
        {
            return;
        }
        Flash();
        await CardPileCmd.AddToCombatAndPreview<WingedRetainerCard>(Owner, PileType.Hand, (int)Amount, Owner.Player);
    }
}
