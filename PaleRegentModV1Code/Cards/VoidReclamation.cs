using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空回收】技能牌（表 C#95，0727 新增）。
/// 0 灵魂：消耗手牌中所有【虚空】状态牌，每消耗 1 张：获得 1 点虚空并抽 1 张牌。
/// 升级后：每张获得 2 点虚空并抽 2 张牌。
/// </summary>
public class VoidReclamation() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    /// <summary>每消耗 1 张虚空状态牌获得的虚空/抽牌数（升级后 2）。</summary>
    private int _gainPerCard = 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<TheVoidStatus>(false), ModHoverTips.VoidCounter];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardPile hand = PileTypeExtensions.GetPile(PileType.Hand, Owner);
        List<CardModel> voids = hand.Cards.Where(c => c is TheVoidStatus).ToList();
        if (voids.Count == 0)
        {
            return;
        }

        foreach (CardModel v in voids)
        {
            await CardCmd.Exhaust(choiceContext, v);
        }

        int total = voids.Count * _gainPerCard;
        // 获得虚空
        await VoidResource.Gain(Owner, total);
        // 抽牌
        await CardPileCmd.Draw(choiceContext, total, Owner);
    }

    protected override void OnUpgrade()
    {
        _gainPerCard = 2;
    }
}
