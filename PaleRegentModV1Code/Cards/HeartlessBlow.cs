using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【失心重击】攻击牌。
/// 0 灵魂 + 2 虚空：造成 7 点伤害。自带【失心】并获得相应的重放效果。
/// 升级后：12 点伤害。
/// </summary>
public class HeartlessBlow : PaleRegentModV1Card
{
    private const int VoidCost = 2;
    private const int BaseDamage = 7;
    private const int UpgradeDamageBonus = 5;

    public HeartlessBlow() : base(0,
        CardType.Attack, CardRarity.Common,
        TargetType.AnyEnemy)
    {
        // 固定登记 2 点虚空费用。
        CardTraits.SetVoidCost(this, VoidCost);
    }

    /// <summary>
    /// 保留自带失心：此处灵魂费本来就是 0，因此失心不会额外增加虚空费；
    /// 仍会保留失心提供的重放、消耗及对应词条表现。
    /// </summary>
    public override bool HasInnateLost => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [ModHoverTips.Lost];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}