using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【疫触】攻击牌（机制文档：瘟疫流基础，占位命名）。
/// 1 灵魂 攻击：造成 6 点伤害，对目标施加 2 层【瘟疫】。
/// 升级后：伤害 +3，瘟疫 +1。
/// </summary>
public class PlagueTouch() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Common,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 6;
    private const int UpgradeDamageBonus = 3;
    private const int BasePlague = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move), new PowerVar<PlaguePower>(BasePlague)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await PowerCmd.Apply<PlaguePower>(choiceContext, cardPlay.Target,
            DynamicVars["PlaguePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        DynamicVars["PlaguePower"].UpgradeValueBy(1m);
    }
}
