using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 弃壳·甲（Cast-Off Shell: Carapace）—— 罕见遗物（机制文档：弃壳，占位设计）。
/// 效果：每回合开始时获得 3 点格挡。
/// 使用 1 场战斗后碎裂（战斗胜利后自动移除）。
/// </summary>
public class CastOffShellCarapace : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool IsUsedUp => true;

    private const int Block = 3;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, Block, ValueProp.Unpowered, cardPlay: null);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await RelicCmd.Remove(this);
    }
}
