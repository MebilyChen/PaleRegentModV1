using MegaCrit.Sts2.Core.HoverTips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 1 灵魂。消耗手牌中所有【感染】；造成伤害。
/// 最终伤害 = 基础伤害 + （消耗区【感染】数 + 手牌【感染】数）× 每张附加伤害。
/// 牌面计数把手牌中的感染也计入，等价于本牌打出、它们被消耗后的最终数量。
/// </summary>
public class PlagueBlade() : PaleRegentModV1Card(
    1,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;
    private const int DamagePerInfection = 3;
    private const int UpgradeDamagePerInfectionBonus = 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Infection>(false)];

    /// <summary>
    /// 将牌面数字和实际结算统一为同一个动态变量。
    /// CalculatedDamage = CalculationBase + ExtraDamage × multiplier。
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(BaseDamage),
        new ExtraDamageVar(DamagePerInfection),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (CardModel card, Creature _) => CountInfectionsForFinalDamage(card))
    ];

    /// <summary>
    /// 牌面需要预览「打出本牌并消耗手牌感染之后」的数值。
    /// 所以要把消耗区已有感染和手牌中将被消耗的感染相加。
    /// 本牌结算完消耗后，手牌感染会移到消耗区，合计不变。
    /// </summary>
    private static int CountInfectionsForFinalDamage(CardModel card)
    {
        return CardPile.GetCards(card.Owner, PileType.Exhaust).Count(c => c is Infection || c is MegaCrit.Sts2.Core.Models.Cards.Infection)
            + CardPile.GetCards(card.Owner, PileType.Hand).Count(c => c is Infection || c is MegaCrit.Sts2.Core.Models.Cards.Infection);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        List<CardModel> infections = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c is Infection || c is MegaCrit.Sts2.Core.Models.Cards.Infection)
            .ToList();

        foreach (CardModel infection in infections)
        {
            await CardCmd.Exhaust(choiceContext, infection);
        }

        // 不再手算伤害；与牌面展示使用完全相同的动态变量。
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(UpgradeDamageBonus);          // 5 -> 8
        DynamicVars.ExtraDamage.UpgradeValueBy(UpgradeDamagePerInfectionBonus); // 3 -> 5
    }
}