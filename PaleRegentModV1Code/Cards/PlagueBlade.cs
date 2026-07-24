using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【疫刃】攻击牌（机制文档：瘟疫流）。
/// 1 灵魂 攻击：造成 5 点伤害；消耗手牌中所有【感染】，每消耗 1 张伤害 +5。
/// 升级后：8 点伤害，每张 +6。
/// </summary>
public class PlagueBlade() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;
    private const int BaseDamagePerInfection = 5;

    /// <summary>升级后每张感染额外伤害（+1 → 6）。</summary>
    private int _perInfectionBonus;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 消耗手牌中所有【感染】
        List<CardModel> infections = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c is Infection)
            .ToList();
        foreach (CardModel infection in infections)
        {
            await CardCmd.Exhaust(choiceContext, infection);
        }

        decimal damage = DynamicVars.Damage.BaseValue +
            infections.Count * (BaseDamagePerInfection + _perInfectionBonus);
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        _perInfectionBonus = 1;
    }
}
