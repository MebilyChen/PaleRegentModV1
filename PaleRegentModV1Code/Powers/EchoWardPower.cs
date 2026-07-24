using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【回响守护】buff（由卡牌【完美格挡】施加）。
/// 层数 = 剩余生效回合数。你的回合开始时：获得 3 点格挡，层数 -1；层数归零后消失。
///
/// 为什么不用原版 BlockNextTurnPower？
/// 原版那个只在"下一回合"触发一次就消失，无法表达"下两回合各 +3 格挡"，
/// 所以自制一个可以按层数持续多回合的版本。
///
/// 修改指南：
/// - 每回合格挡量：BlockPerTurn 静态字段，由完美格挡施加时写入（基础 3，升级 5）。
/// - 生效回合数由施加时的层数决定（完美格挡里 Apply 的 amount 参数）。
/// </summary>
public class EchoWardPower : PaleRegentModV1Power
{
    /// <summary>每回合开始获得的格挡量（由完美格挡施加时写入；基础 3，升级 5）。</summary>
    public static int BlockPerTurn = 3;

    public override PowerType Type => PowerType.Buff;
    // Counter：层数可叠加（重复打完美格挡会延长持续回合）
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 挂点：一方回合开始之后（AfterSideTurnStart）。
    /// participants.Contains(Owner) 表示"这次开始回合的一方包含本 Power 持有者"，
    /// 即只在玩家自己的回合开始时触发（原版 Blur/Coolant 等 Power 的标准写法）。
    /// </summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        Flash(); // 图标闪烁提示玩家 Power 触发了

        // 1. 获得格挡（ValueProp.Unpowered：不吃敏捷等加成的固定格挡，
        //    想让它吃加成可改成 ValueProp.Default）
        await CreatureCmd.GainBlock(Owner, BlockPerTurn, ValueProp.Unpowered, null);

        // 2. 层数 -1，归零自动移除
        await PowerCmd.Decrement(this);
    }
}
