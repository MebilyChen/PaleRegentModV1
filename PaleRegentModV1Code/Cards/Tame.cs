using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【驯化】技能牌。
/// 2 灵魂：消耗你手牌、抽牌堆和弃牌堆中的全部【虚空】状态牌。
/// 未升级：虚空不少于 5 张时获得【虚空化神】；不少于 2 张时获得【虚空化形】；否则获得【失败实验】。
/// 升级后：虚空不少于 5 张时获得【虚空化神+】；否则获得【虚空化形+】；并具有保留。
/// </summary>
public class Tame() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Rare,
    TargetType.Self)
{
    private const int GodThreshold = 5;
    private const int FormThreshold = 2;

    /// <summary>
    /// 升级版只有【虚空化神+】和【虚空化形+】两种结果；
    /// 未升级版才可能得到【失败实验】。
    /// </summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => //,HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Void>(false)
        IsUpgraded
            ?
            [
                HoverTipFactory.FromCard<VoidGivenFocus>(true),
                HoverTipFactory.FromCard<VoidGivenForm>(true)
            ]
            :
            [
                HoverTipFactory.FromCard<VoidGivenFocus>(false),
                HoverTipFactory.FromCard<VoidGivenForm>(false),
                HoverTipFactory.FromCard<FailedExperiment>(false)
            ];

    /// <summary>
    /// 升级后获得 Retain。此属性负责 Canonical 卡牌、图鉴和预览中的关键词状态。
    /// </summary>
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [CardKeyword.Retain] : [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 统计并消耗手牌、抽牌堆、弃牌堆内全部【虚空】状态牌。
        List<CardModel> voids = CardPile.GetCards(Owner, PileType.Hand)
            .Concat(CardPile.GetCards(Owner, PileType.Draw))
            .Concat(CardPile.GetCards(Owner, PileType.Discard))
            .Where(card => card is TheVoidStatus||
                           card is MegaCrit.Sts2.Core.Models.Cards.Void)
            .ToList();

        foreach (CardModel voidCard in voids)
        {
            await CardCmd.Exhaust(choiceContext, voidCard);
        }

        CardModel made;

        if (voids.Count >= GodThreshold)
        {
            // 未升级：虚空化神；升级后：虚空化神+。
            made = Owner.Creature.CombatState.CreateCard<VoidGivenFocus>(Owner);
        }
        else if (IsUpgraded || voids.Count >= FormThreshold)
        {
            // 未升级时仅在虚空为 2–4 张时得到虚空化形；
            // 升级后只要虚空少于 5 张就得到虚空化形+。
            made = Owner.Creature.CombatState.CreateCard<VoidGivenForm>(Owner);
        }
        else
        {
            // 仅未升级且虚空不足 2 张时得到失败实验。
            made = Owner.Creature.CombatState.CreateCard<FailedExperiment>(Owner);
        }

        // 升级后只升级化神/化形；失败实验不会在升级版分支中生成。
        if (IsUpgraded &&
            (made is VoidGivenFocus || made is VoidGivenForm))
        {
            CardCmd.Upgrade(made, (CardPreviewStyle)1);
        }

        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(
                made,
                PileType.Hand,
                Owner,
                (CardPilePosition)1),
            0f,
            (CardPreviewStyle)1);
    }

    protected override void OnUpgrade()
    {
        // 对当前可变卡牌实例实际应用 Retain，确保升级后战斗内能够保留。
        CardCmd.ApplyKeyword(this, [CardKeyword.Retain]);
    }
}
