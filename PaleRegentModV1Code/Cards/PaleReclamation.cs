using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【苍白赎回】技能牌（表 C#86，0727 新增）。
/// 2 灵魂：从消耗牌堆选择 3 张牌，为其施加【苍白】后放回抽牌堆（洗入随机位置）。
/// 升级后：5 张。
/// </summary>
public class PaleReclamation() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    private int _reclaimCount = 3;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Pale];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardPile exhaust = PileTypeExtensions.GetPile(PileType.Exhaust, Owner);
        if (!exhaust.Cards.Any())
        {
            return;
        }

        List<CardModel> selected = (await CardSelectCmd.FromCombatPile(
            choiceContext, exhaust, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, _reclaimCount),
            (Func<CardModel, bool>)((CardModel _) => true))).ToList();

        foreach (CardModel c in selected)
        {
            CardTraits.ApplyPale(c);
            await CardPileCmd.Add(c, PileType.Draw, CardPilePosition.Random, null, false);
        }
    }

    protected override void OnUpgrade()
    {
        _reclaimCount = 5;
    }
}
