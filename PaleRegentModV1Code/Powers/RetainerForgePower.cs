using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【佣卫工厂】buff（能力牌"佣卫工厂"施加，机制文档：造物流）。
/// 效果：每回合开始时，将 [层数] 张【国王佣卫】加入手牌。
/// MakeUpgraded：由升级版【佣卫工厂+】置 true，改为生成【国王佣卫+】。
/// </summary>
public class RetainerForgePower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>是否生成升级版【国王佣卫+】（由升级版卡牌置 true）。</summary>
    public bool MakeUpgraded;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Owner.Player == null)
        {
            return;
        }
        Flash();
        for (int i = 0; i < (int)Amount; i++)
        {
            KingsRetainer card = Owner.CombatState.CreateCard<KingsRetainer>(Owner.Player);
            if (MakeUpgraded)
            {
                CardCmd.Upgrade(card, (CardPreviewStyle)1);
            }
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner.Player, (CardPilePosition)1),
                2.2f, (CardPreviewStyle)1);
        }
    }
}
