using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 弃壳·刃（Cast-Off Shell: Blade）—— 普通遗物（机制文档：弃壳，占位设计）。
/// 效果：每回合开始时，对一个随机敌人造成 5 点伤害。
/// 使用 1 场战斗后碎裂（战斗胜利后自动移除）。
///
/// 占位说明：文档中"弃壳"具体拆分方式未定，先按"刃（攻）/甲（防）"
/// 两件占位，数值 5 伤 / 3 格挡，一场战斗后失效。
/// </summary>
public class CastOffShellBlade : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    /// <summary>标记为一次性遗物（UI 显示为用完即碎）。</summary>
    public override bool IsUsedUp => true;

    private const int Damage = 5;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }
        if (combatState.HittableEnemies.Count == 0)
        {
            return;
        }
        Flash();
        Creature target = Owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target, Damage, ValueProp.Unpowered, Owner.Creature);
    }

    /// <summary>战斗结束后碎裂。</summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await RelicCmd.Remove(this);
    }
}
