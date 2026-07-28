using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空反噬】攻击牌（表 C#77，0727 新增）。
/// 0 灵魂：造成 7 点伤害，在手牌中生成 1 张【虚空】状态牌。
/// 升级后：10 点伤害。
/// </summary>
public class VoidBacklash() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Common,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 7;
    private const int UpgradeDamageBonus = 3;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<TheVoidStatus>(false)];

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

        // 在手牌中生成 1 张虚空状态牌
        await CardPileCmd.AddToCombatAndPreview<TheVoidStatus>(Owner.Creature, PileType.Hand, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
