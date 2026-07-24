using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【疫刃】攻击牌（机制文档：瘟疫流，占位命名）。
/// 1 灵魂 攻击：造成 5 点伤害；消耗手牌中的【感染】。本场战斗中每有一张已消耗的【感染】，
/// 伤害 +3（吞噬病灶淬炼刀锋）。
/// 升级后：基础伤害 +3。
/// </summary>
public class PlagueBlade() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;
    private const int DamagePerInfection = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int exhaustedInfections = CardPile.GetCards(Owner, PileType.Exhaust)
            .Count(c => c is Infection);
        decimal damage = DynamicVars.Damage.BaseValue + exhaustedInfections * DamagePerInfection;
        await DamageCmd.Attack(damage)
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
