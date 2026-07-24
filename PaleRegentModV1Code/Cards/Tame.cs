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
/// 【驯化】技能牌（机制文档：造物流终端）。
/// 2 灵魂 技能：消耗你手牌中所有的【虚空】状态牌：
/// ≥5 张 → 将 1 张【虚空化神】加入手牌；
/// ≥2 张 → 将 1 张【虚空化形】加入手牌。消耗。
/// 升级后：生成升级版（虚空化神+/虚空化形+）。
/// </summary>
public class Tame() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Rare,
    TargetType.Self)
{
    private const int GodThreshold = 5;
    private const int FormThreshold = 2;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 消耗手牌中所有的【虚空】状态牌
        List<CardModel> voids = CardPile
            .GetCards(Owner, PileType.Hand)
            .Where(c => c is TheVoidStatus)
            .ToList();

        foreach (CardModel v in voids)
        {
            await CardCmd.Exhaust(choiceContext, v);
        }

        CardModel made = null;
        if (voids.Count >= GodThreshold)
        {
            made = Owner.Creature.CombatState.CreateCard<VoidGivenFocus>(Owner);
        }
        else if (voids.Count >= FormThreshold)
        {
            made = Owner.Creature.CombatState.CreateCard<VoidGivenForm>(Owner);
        }

        if (made != null)
        {
            // 升级后：生成升级版（虚空化神+/虚空化形+）
            if (IsUpgraded)
            {
                CardCmd.Upgrade(made, (CardPreviewStyle)1);
            }
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(made, PileType.Hand, Owner, (CardPilePosition)1),
                2.2f, (CardPreviewStyle)1);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：生成升级版牌（见 OnPlay 的 IsUpgraded 分支）
    }
}
