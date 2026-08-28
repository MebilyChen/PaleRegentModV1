using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using PaleRegentModV1.PaleRegentModV1Code.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【容器计划】buff（能力牌"容器计划"施加，机制文档：造物流）。
/// 效果：你的每回合开始时，将 [层数] 张【容器】加入手牌。
/// MakeUpgraded 为 true 时生成升级版【容器+】（由容器计划+设置）。
/// </summary>
public class VesselPlanPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>是否生成升级版【容器+】（由容器计划+设置）。</summary>
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
            CardModel vessel = combatState.CreateCard<Vessel>(Owner.Player);
            if (MakeUpgraded)
            {
                CardCmd.Upgrade(vessel, (CardPreviewStyle)1);
            }
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(vessel, PileType.Hand, Owner.Player, (CardPilePosition)1),
                0f, (CardPreviewStyle)1);
        }
    }
}
