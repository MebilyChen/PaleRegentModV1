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

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【虚空之触】debuff。
/// 效果：减少等同层数的力量；在持有者的回合结束时，造成等同层数的伤害
/// （可被格挡）。若该伤害实际使生命值减少，则该效果消失；若伤害被完全格挡，
/// 则该效果保留并减少 1 层。
///
/// 实现说明：
/// - 减力量：不真的施加 StrengthPower，而是用 ModifyDamageAdditive 在
///   持有者作为攻击方时对伤害做 -Amount 修正（等效于负力量，且随本 Power
///   消失自动解除，省去“归还力量”的麻烦）。
/// - 回合结束伤害：在 AfterSideTurnEnd 中记录结算前生命值，结算后以
///   CurrentHp 是否降低判定是否实际造成伤害。ValueProp 不带 Unblockable，
///   因此伤害可以被格挡。
/// - 血条展示：由本 Power 提供与当前 Amount 对应的深色预览段。
///
/// 修改指南：
/// - 想让它变成“不可被格挡”：把 ValueProp.Unpowered 改为
///   ValueProp.Unblockable | ValueProp.Unpowered。
/// </summary>
public class VoidTouchPower :
    PaleRegentModV1Power,
    IHealthBarForecastSource
{
    /// <summary>
    /// 虚空之触在血条上的深色预览颜色。
    /// </summary>
    private static readonly Color HealthBarForecastColor =
        new("050508");

    /// <summary>
    /// 在持有者血条上显示与当前层数等长的虚空之触预览段。
    /// </summary>
    public IEnumerable<HealthBarForecastSegment>
        GetHealthBarForecastSegments(
            HealthBarForecastContext context)
    {
        if (context.Creature != Owner || Amount <= 0)
        {
            yield break;
        }

        yield return new HealthBarForecastSegment(
            Amount: Amount,
            Color: HealthBarForecastColor,
            Direction: HealthBarForecastDirection.FromRight,
            Order: 0,
            OverlayMaterial: null,
            OverlaySelfModulate: HealthBarForecastColor,
            LeftOriginLayout:
                HealthBarForecastLeftOriginLayout.Chained,
            LeftExclusiveZGroup: 0,
            AffectsHpLabel: false
        );
    }

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 等效负力量：持有者打出的受力量加成攻击，伤害 -Amount。
    /// </summary>
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
        return -Amount;
    }

    /// <summary>
    /// 持有者一方的回合结束时：造成等同层数的可格挡伤害。
    /// 实际扣血则移除本效果；未扣血（被完全格挡）则仅减少 1 层。
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || !Owner.IsAlive || Amount <= 0)
        {
            return;
        }

        Flash();

        // Damage 不带 Unblockable：可被格挡；Unpowered：不吃力量/易伤等加成。
        var hpBeforeDamage = Owner.CurrentHp;
        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            Amount,
            ValueProp.Unpowered,
            null,
            cardPlay: null);

        // 以实际生命值是否降低为准，而不是以伤害数值或是否有格挡为准。
        if (Owner.CurrentHp < hpBeforeDamage)
        {
            await PowerCmd.Remove(this);
            return;
        }

        await PowerCmd.Decrement(this);
    }
}
