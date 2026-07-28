using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 【缚丝封印 Seal Of Binding】罕见遗物（机制表：遗物 R#11，0727 新增）。
/// 效果：战斗开始时，对所有敌人施加 1 层【纯粹封印】。
///
/// 实现说明：
/// - 参考原版 BagOfMarbles（战斗开始全体易伤）：挂 BeforeSideTurnStart，
///   在第 1 回合、且本遗物持有者参战的一侧回合开始前触发，
///   用 PowerCmd.Apply 的集合重载一次性对 combatState.HittableEnemies 上 debuff。
/// - 不用 BeforeCombatStart 是因为该钩子没有 choiceContext，
///   而 PowerCmd.Apply 需要一个选择上下文参与联机同步。
/// - HoverTips 展示【纯粹封印】词条（HoverTipFactory.FromPower）。
/// </summary>
public class SealOfBinding : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    private const int SealStacks = 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PureSealPower>()];

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState.TurnNumber > 1)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<PureSealPower>(choiceContext, combatState.HittableEnemies, SealStacks, Owner.Creature, null);
    }
}
