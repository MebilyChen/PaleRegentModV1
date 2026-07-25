using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【旧日仇敌】负面（机制文档：效果表 P#5，由【集火敕令】施加）。
/// 效果：【瘟疫】效果的随机攻击对象集中在持有此 Power 的生物上。
///       持续 [层数] 回合（持有者一方的回合结束时减少 1 层）。
///
/// 实现说明：
/// - 本 Power 自身不做伤害逻辑，只作为"集火标记"存在；
///   实际的目标选择在 PlaguePower.AfterDamageGiven 里：
///   优先寻找场上存活且带有 AncientEnemyPower 的生物作为瘟疫随机攻击的目标。
/// - 旧实现使用 PlaguePower.FocusTarget 静态字段（已删除），
///   改为独立 Power 后支持持续回合数、可被驱散、存档一致性更好。
///
/// 修改指南：
/// - 想改持续时间递减时机：调整 AfterSideTurnEnd 里的 participants 判断。
/// - 想让多个敌人同时被标记时有优先级：改 PlaguePower 里的 FirstOrDefault 逻辑。
/// </summary>
public class AncientEnemyPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 持有者一方的回合结束时层数 -1，减到 0 自动移除（Decrement 内部处理）。
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        await PowerCmd.Decrement(choiceContext, this, 1);
    }
}
