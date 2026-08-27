using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 本回合临时提高失心牌重放次数的增益。
/// 回合结束时按自身层数回退加成，并移除自身。
/// </summary>
public class LostReplayThisTurnPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;

    // 同一回合重复获得时合并层数；回合结束时一次性回退全部层数。
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 持有者一方的回合结束时，撤销本 Power 提供的临时失心重放层数。
    /// </summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner))
        {
            return;
        }

        CardTraits.RemoveLostReplayCount(Amount);
        await PowerCmd.Remove(this);
    }
}