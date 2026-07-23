using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【驯化】技能牌（机制文档：造物流终端，占位设计）。
/// 2 灵魂 技能：消耗你手牌、抽牌堆、弃牌堆中所有的【虚空】状态牌：
/// ≥9 张 → 将 1 张【虚空化神】加入手牌；
/// ≥5 张 → 将 1 张【虚空化形】加入手牌；
/// 否则 → 将 1 张【失败实验】加入手牌。消耗。
/// </summary>
public class Tame() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Rare,
    TargetType.Self)
{
    private const int GodThreshold = 9;
    private const int FormThreshold = 5;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 收集三个牌堆里的所有【虚空】状态牌
        List<CardModel> voids = CardPile
            .GetCards(Owner, PileType.Hand, PileType.Draw, PileType.Discard)
            .Where(c => c is TheVoidStatus)
            .ToList();

        foreach (CardModel v in voids)
        {
            await CardCmd.Exhaust(choiceContext, v);
        }

        if (voids.Count >= GodThreshold)
        {
            await CardPileCmd.AddToCombatAndPreview<VoidGivenFocus>(Owner.Creature, PileType.Hand, 1, Owner);
        }
        else if (voids.Count >= FormThreshold)
        {
            await CardPileCmd.AddToCombatAndPreview<VoidGivenForm>(Owner.Creature, PileType.Hand, 1, Owner);
        }
        else
        {
            await CardPileCmd.AddToCombatAndPreview<FailedExperiment>(Owner.Creature, PileType.Hand, 1, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
