using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【瘟疫】debuff（机制文档：新增负面效果，表格设计版）。
/// 效果：本回合内，增加 [层数] 的力量；本回合的每次攻击将额外对随机生物
/// 造成 [层数] 次 3 点基础攻击（随机对象包括自己、队友、敌人和召唤物；
/// 基础攻击计入力量增长，即每段 3 点吃持有者的伤害加成）。
/// 持有者一方回合结束时效果消失（"本回合内"）。
///
/// 实现说明：
/// - 力量增益：通过 ModifyDamageAdditive 提供 +Amount 攻击伤害加成（等效力量，
///   避免额外挂 StrengthPower 带来的回合结束同步移除问题）；
/// - 每次攻击后：AfterDamageGiven 触发 [层数] 段独立随机 3 点攻击伤害
///   （不带 Unpowered，吃力量/瘟疫加成），_resolving 防止段伤递归触发自身；
/// - 嘲讽卡【集火号令】通过 FocusTarget 静态字段占位实现：本回合内瘟疫的
///   随机攻击全部集中在该目标上（回合结束清空）；
/// - 【疫佑】：玩家侧任意生物持有 PlagueWardPower 时，随机目标排除玩家侧。
/// </summary>
public class PlaguePower : PaleRegentModV1Power
{
    /// <summary>每段随机攻击的基础伤害。</summary>
    private const decimal DamagePerHit = 3m;

    /// <summary>嘲讽占位：本回合瘟疫随机攻击集中的目标（null = 正常随机）。</summary>
    public static Creature? FocusTarget;

    /// <summary>防止随机段伤本身再次触发瘟疫（递归保护）。</summary>
    private bool _resolving;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>力量增益部分：持有者造成的攻击伤害 +Amount（加法修正，等效力量）。</summary>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner || target == Owner)
        {
            return 0m;
        }
        return Amount;
    }

    /// <summary>持有者每次造成攻击伤害后：额外进行 [层数] 次独立随机 3 点基础攻击。</summary>
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || _resolving)
        {
            return;
        }

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }

        _resolving = true;
        try
        {
            Flash();
            int hits = (int)Amount;
            for (int i = 0; i < hits; i++)
            {
                // 每一段攻击都重新随机一个存活生物；嘲讽期间集中在 FocusTarget 上
                Creature? extraTarget = FocusTarget is { IsAlive: true }
                    ? FocusTarget
                    : PickRandomAliveCreature(combatState);
                if (extraTarget == null)
                {
                    break;
                }
                // 基础攻击：不带 Unpowered，计入力量/瘟疫加成
                await CreatureCmd.Damage(choiceContext, extraTarget, DamagePerHit, ValueProp.SkipHurtAnim, Owner);
            }
        }
        finally
        {
            _resolving = false;
        }
    }

    /// <summary>持有者一方回合结束：瘟疫消失（"本回合内"）。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
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
