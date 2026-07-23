using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【瘟疫】debuff（机制文档：新增负面效果）。
/// 效果：持有者的回合结束时，进行【层数】次独立的随机攻击——
/// 每一次都重新随机选取一个存活生物（包括自己、队友、敌人和召唤物）作为目标，
/// 造成固定伤害。结算完毕后瘟疫消失。
///
/// 例：叠加 2 层 = 本回合结束时随机打 2 次，每次都单独随机一个对象；
/// 多段伤害即增加攻击段数，而不是把伤害合并成一次。
///
/// 占位说明（后续可微调）：
/// - 每段伤害占位为 3 点（DamagePerHit 常量），改数值直接改常量即可。
/// - 嘲讽卡【集火号令】通过 FocusTarget 静态字段占位实现：本回合内瘟疫的
///   随机攻击全部集中在该目标上（回合结束清空）。
/// - 【疫佑】：玩家侧任意生物持有 PlagueWardPower 时，随机目标排除玩家侧。
/// </summary>
public class PlaguePower : PaleRegentModV1Power
{
    /// <summary>每段随机攻击的占位伤害。</summary>
    private const decimal DamagePerHit = 3m;

    /// <summary>嘲讽占位：本回合瘟疫随机攻击集中的目标（null = 正常随机）。</summary>
    public static Creature? FocusTarget;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 持有者回合结束时：进行层数次独立随机攻击（每次单独随机目标），随后瘟疫消失。
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        ICombatState? combatState = Owner.CombatState;
        if (combatState != null)
        {
            Flash();
            int hits = (int)Amount;
            for (int i = 0; i < hits; i++)
            {
                // 每一段攻击都重新随机一个存活生物；嘲讽期间集中在 FocusTarget 上
                Creature? target = FocusTarget is { IsAlive: true }
                    ? FocusTarget
                    : PickRandomAliveCreature(combatState);
                if (target == null)
                {
                    break;
                }
                await CreatureCmd.Damage(choiceContext, target, DamagePerHit, ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner);
            }
        }

        FocusTarget = null;
        await PowerCmd.Remove(this);
    }

    private Creature? PickRandomAliveCreature(ICombatState combatState)
    {
        // 疫佑占位：只要玩家侧有任意生物持有 PlagueWardPower，随机目标就排除玩家侧
        bool warded = combatState.PlayerCreatures.Any(c => c.HasPower<PlagueWardPower>());
        List<Creature> candidates = combatState.Creatures
            .Where(c => c.IsAlive && (!warded || !combatState.PlayerCreatures.Contains(c)))
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        // 用官方战斗随机数流，保证多人联机/回放一致性；
        // 持有者可能是敌人（Player 为 null），此时取任意一个玩家的 Rng。
        Player? player = Owner.Player ?? combatState.PlayerCreatures.FirstOrDefault()?.Player;
        if (player == null)
        {
            return candidates[0];
        }
        return player.RunState.Rng.CombatTargets.NextItem(candidates);
    }
}
