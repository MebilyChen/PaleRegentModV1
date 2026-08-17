using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【深渊诞生】能力。
/// 每消耗 1 点虚空，对随机敌人造成层数点伤害。
/// 每获得 1 点虚空，获得层数点防御。
/// </summary>
public class BorninAbyssPower : PaleRegentModV1Power, ISecondaryResourceHookListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 在虚空支付完成后触发。一次支付 N 点虚空时，进行 N 段伤害；
    /// 每段都会重新随机选择一个存活敌人，允许重复命中同一敌人。
    /// </summary>
    public async Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        if (context.Player != Owner.Player ||
            context.Definition.Id != VoidResource.Id ||
            context.Amount <= 0 ||
            context.Card == null)
        {
            return;
        }

        Flash();

        await DamageCmd.Attack(Amount)
            .FromCard(context.Card, null)
            .TargetingRandomOpponents(Owner.CombatState)
            .WithHitCount(context.Amount)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(new ThrowingPlayerChoiceContext());
    }

    /// <summary>
    /// 在虚空实际增加后触发。一次获得 N 点虚空时，总共获得 N × 层数点防御。
    /// 仅响应 Gain，避免直接 Set、回合开始重置等数值变化误触发。
    /// </summary>
    public async Task AfterSecondaryResourceChanged(SecondaryResourceChangeContext context)
    {
        if (context.Player != Owner.Player ||
            context.Definition.Id != VoidResource.Id ||
            context.Reason != SecondaryResourceChangeReason.Gain ||
            context.Delta <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(
            Owner,
            Amount * context.Delta,
            ValueProp.Unpowered,
            cardPlay: null);
    }
}
