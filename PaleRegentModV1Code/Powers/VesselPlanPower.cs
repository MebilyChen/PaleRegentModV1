using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【容器计划】buff（能力牌"容器计划"施加，机制文档：造物流）。
/// 效果：每回合开始时，将 [层数] 张【容器】加入手牌，
/// 并召集【羞愧】（君王之剑式：已有则全部移回手牌，一张都没有才生成一张）。
/// </summary>
public class VesselPlanPower : PaleRegentModV1Power
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
        await CardPileCmd.AddToCombatAndPreview<Vessel>(Owner, PileType.Hand, (int)Amount, Owner.Player);
        // Shame 特质（君王之剑式）：已有则全部移回手牌，一张都没有才生成一张，避免满手诅咒
        await CurseTraitHelper.Summon<Shame>(Owner.Player);
    }
}
