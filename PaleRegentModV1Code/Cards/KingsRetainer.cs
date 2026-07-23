using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【国王佣卫】生成牌（机制文档：造物流，"制造佣卫"每回合生成）。
/// 0 灵魂 攻击：造成 10 点伤害。消耗。
/// 造物牌：受【驾驭 Harness】加成（HarnessPower.ModifyDamageAdditive 自动生效）。
/// （文档提到与弃壳遗物的联动后续再接。）
/// </summary>
public class KingsRetainer() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Special,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 10;
    private const int UpgradeDamageBonus = 3;

    public override bool IsCreationCard => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

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
