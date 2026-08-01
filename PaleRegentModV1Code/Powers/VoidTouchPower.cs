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
/// 【虚空之触】debuff（机制文档：新增负面效果）。
/// 效果：减少等同层数的力量；在持有者的回合结束时，失去等同层数的生命
/// （可被格挡），随后该效果整个消失（连同力量减益一起解除）。
///
/// 实现说明：
/// - 减力量：不真的施加 StrengthPower，而是用 ModifyDamageAdditive 在
///   持有者作为攻击方时对伤害做 -Amount 修正（等效于负力量，且随本 Power
///   消失自动解除，省去"归还力量"的麻烦）。
/// - 回合结束伤害：AfterSideTurnEnd 里对 Owner 造成 Amount 点伤害，
///   ValueProp 不带 Unblockable，因此可以被格挡；每回合减少1层 （原效果：Remove 自身)。
/// - 血条展示：由本 Power 提供与当前 Amount 对应的深色预览段。
///
/// 修改指南：
/// - 想让它变成"不可被格挡"：把 ValueProp.Unpowered 改为
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
    /// 持有者一方的回合结束时：失去层数点生命（可被格挡），~~然后整个效果消失~~。
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }
        Flash();
        if (Owner.IsAlive && Amount > 0)
        {
            // 不带 Unblockable：可被格挡；Unpowered：不吃力量/易伤等加成
            await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, null, cardPlay: null);
        }
        //await PowerCmd.Remove(this);
        await PowerCmd.Decrement(this);
    }
}
