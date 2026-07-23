using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【瘟疫】debuff（机制文档：新增负面效果）。
/// 效果：本回合内 +层数力量；本回合持有者打出的下一张攻击牌结算后，
/// 额外对随机生物（含敌我双方）造成层数次、每次等于该攻击伤害的攻击——
/// 占位简化版：额外攻击伤害固定为瘟疫层数，对随机存活生物各打一次，共层数次。
/// 回合结束时瘟疫消失。
///
/// 占位说明（后续可微调）：
/// - 文档原义"对随机生物造成层数次攻击"的攻击数值定义不明确，这里先用
///   "每次造成 [层数] 点伤害" 占位；要改成复读原攻击伤害需要在 AfterAttack
///   里读 command.Results 的伤害值，接口都留好了。
/// - "包括自己、队友、敌人和召唤物"：从 CombatState 全部存活 Creature 里随机。
/// - 嘲讽卡的"集中目标"通过 FocusTarget 静态字段占位实现（回合结束清空）。
/// </summary>
public class PlaguePower : PaleRegentModV1Power
{
    /// <summary>嘲讽占位：本回合瘟疫随机攻击集中的目标（null = 正常随机）。</summary>
    public static Creature? FocusTarget;

    /// <summary>本回合是否已经触发过（只对"下一次攻击"生效）。</summary>
    private bool _triggeredThisTurn;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>+层数力量（本回合内，瘟疫本身回合结束就消失）。</summary>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (Owner != dealer)
        {
            return 0m;
        }
        if (!props.IsPoweredAttack())
        {
            return 0m;
        }
        return Amount;
    }

    /// <summary>
    /// 持有者的下一次攻击结算后：对随机生物追加层数次攻击。
    /// </summary>
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != Owner || _triggeredThisTurn || !command.DamageProps.IsPoweredAttack())
        {
            return;
        }
        _triggeredThisTurn = true;
        Flash();

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }
        for (int i = 0; i < Amount; i++)
        {
            // 嘲讽占位：有集中目标且存活则全部打它；否则随机
            Creature? target = FocusTarget is { IsAlive: true }
                ? FocusTarget
                : PickRandomAliveCreature(combatState);
            if (target == null)
            {
                break;
            }
            await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner);
        }
    }

    /// <summary>回合结束：瘟疫消失，触发标记与嘲讽目标复位。</summary>
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
        var player = Owner.Player ?? combatState.PlayerCreatures.FirstOrDefault()?.Player;
        if (player == null)
        {
            return candidates[0];
        }
        return player.RunState.Rng.CombatTargets.NextItem(candidates);
    }
}
