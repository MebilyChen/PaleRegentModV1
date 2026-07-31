using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Hooks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Patches;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【苦痛之路】debuff。
///
/// Amount 表示本回合还需要造成多少点伤害。
///
/// 例如初始为 5 层：
/// 造成 3 点伤害后显示为 2 层；
/// 再造成 2 点伤害后立即移除。
///
/// 如果本回合造成过伤害，但回合结束时仍未清空层数，
/// 持有者受到等同于当前生命值的伤害。
///
/// 如果效果仍存在，则在持有者下一回合开始时，
/// 恢复到本回合开始前的层数。
/// </summary>
public class PathOfPainPower :
    PaleRegentModV1Power,
    IHealthBarForecastSource
{
    /// <summary>
    /// 苦痛之路在血条上的颜色。
    ///
    /// 使用接近纯黑、略带一点冷色的颜色，
    /// 防止和完全空掉的血条区域融为一体。
    /// </summary>
    private static readonly Color HealthBarForecastColor =
        new("050508");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 本回合已经从 Amount 中扣除的层数。
    /// 下一回合开始时用于恢复。
    /// </summary>
    private int _amountReducedThisTurn;

    /// <summary>
    /// 持有者本回合是否造成过实际伤害。
    /// </summary>
    private bool _attackedThisTurn;

    /// <summary>
    /// 在持有者的血条上显示苦痛之路的黑色预览段。
    ///
    /// BaseLib 会自动读取实现了
    /// IHealthBarForecastSource 的 Power，
    /// 因此不需要手动注册，也不需要创建自定义 Node。
    /// </summary>
    public IEnumerable<HealthBarForecastSegment>
        GetHealthBarForecastSegments(
            HealthBarForecastContext context)
    {
        /*
         * 正常情况下，BaseLib 只会把这个 Power
         * 作为 Owner 身上的血条来源进行读取。
         *
         * 这里仍然检查一次，防止错误显示到其他生物血条上。
         */
        if (context.Creature != Owner || Amount <= 0)
        {
            yield break;
        }

        yield return new HealthBarForecastSegment(
            /*
             * 黑条代表当前剩余的苦痛层数。
             *
             * SetAmount() 改变 Amount 后，
             * BaseLib 下一次刷新血条时会自动更新长度。
             */
            Amount: Amount,

            /*
             * Color 还会作为默认的预览条染色。
             */
            Color: HealthBarForecastColor,

            /*
             * FromRight 表示从当前生命值的右侧向左延伸，
             * 使用类似中毒的显示方向。
             *
             * BaseLib 会先为原作中毒保留空间，
             * 再绘制这个自定义段。
             *
             * 因此结果是：
             *
             * 正常生命 | 苦痛 | 中毒
             * 0000       触触触   毒毒毒
             */
            Direction: HealthBarForecastDirection.FromRight,

            /*
             * Order 只影响多个自定义 FromRight 效果之间的顺序。
             *
             * 数值越低，越靠近当前生命值的右侧，
             * 也就是越靠近中毒区域。
             */
            Order: 0,

            /*
             * 不使用自定义 Shader，
             * 直接使用颜色染色原作风格的九宫格血条纹理。
             */
            OverlayMaterial: null,

            /*
             * 明确设置黑色视觉染色。
             */
            OverlaySelfModulate: HealthBarForecastColor,

            /*
             * 该参数主要作用于 FromLeft，
             * FromRight 下保持默认即可。
             */
            LeftOriginLayout:
                HealthBarForecastLeftOriginLayout.Chained,

            LeftExclusiveZGroup: 0,

            /*
             * 即使 Amount 大于当前生命值，
             * 也不把生命数字染成黑色。
             *
             * 苦痛之路并不是像毒一样无条件立即结算的伤害，
             * 它还取决于本回合是否攻击以及是否清空层数。
             */
            AffectsHpLabel: false
        );
    }

    /// <summary>
    /// 持有者造成伤害后，减少对应的显示层数。
    /// </summary>
    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        // 只处理 Power 持有者对其他目标造成的伤害事件。
        if (dealer != Owner || target == Owner)
        {
            return;
        }

        /*
         * 只要进入了伤害结算，就视为本回合攻击过。
         *
         * 即使最终伤害为 0，例如：
         * - 被格挡完全抵消；
         * - 被某些效果减伤到 0；
         *
         * 回合结束时仍然会触发苦痛之路的惩罚。
         */
        _attackedThisTurn = true;

        int damage = decimal.ToInt32(result.TotalDamage);

        /*
         * 0 点伤害算攻击过，但不减少层数。
         */
        if (damage <= 0)
        {
            return;
        }

        // 本次伤害足以清空剩余层数，立即移除。
        if (damage >= Amount)
        {
            Flash();
            await PowerCmd.Remove(this);
            return;
        }

        // 记录本回合减少的层数，下回合开始时恢复。
        _amountReducedThisTurn += damage;

        Flash();
        SetAmount(Amount - damage);
    }

    /// <summary>
    /// 持有者一方回合结束。
    ///
    /// 如果本回合造成过伤害，但 Power 仍未被清除，
    /// 则受到等同于当前生命值的伤害。
    /// </summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 只处理持有者所在阵营的回合结束。
        if (side != Owner.Side || !participants.Contains(Owner))
        {
            return;
        }

        if (_attackedThisTurn && Owner.IsAlive)
        {
            Flash();

            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                Owner.CurrentHp,
                ValueProp.Unpowered,
                null,
                cardPlay: null
            );
        }

        /*
         * 此处只重置攻击标记。
         *
         * 不要清空 _amountReducedThisTurn，
         * 因为下一回合开始时还需要用它恢复层数。
         */
        _attackedThisTurn = false;
    }

    /// <summary>
    /// 持有者下一回合开始时，恢复上一回合减少的层数。
    /// </summary>
    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        // 只处理持有者所在阵营的回合开始。
        if (side != Owner.Side || !participants.Contains(Owner))
        {
            return Task.CompletedTask;
        }

        if (_amountReducedThisTurn > 0)
        {
            /*
             * 例如：
             *
             * 初始为 5 层；
             * 上回合造成 3 点伤害；
             * 当前 Amount 为 2；
             * _amountReducedThisTurn 为 3；
             *
             * 恢复后：2 + 3 = 5。
             */
            SetAmount(Amount + _amountReducedThisTurn);
        }

        // 开始新一回合的统计。
        _amountReducedThisTurn = 0;
        _attackedThisTurn = false;

        return Task.CompletedTask;
    }
}
