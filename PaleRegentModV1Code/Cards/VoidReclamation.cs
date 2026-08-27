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
/// 【虚空回收】技能牌。
/// 0 灵魂：消耗手牌中所有【虚空】状态牌；每消耗 2 张，获得 1 点虚空并抽 1 张牌。
/// 升级后：每消耗 2 张，获得 2 点虚空并抽 2 张牌。
/// </summary>
public class VoidReclamation() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    /// <summary>每次结算需要消耗的虚空牌数量。基础为 2。</summary>
    private int _voidsPerTrigger = 2;

    /// <summary>每次结算获得的虚空数量。基础为 1，升级后为 2。</summary>
    private int _voidGainPerTrigger = 1;

    /// <summary>每次结算抽取的卡牌数量。基础为 1，升级后为 2。</summary>
    private int _drawPerTrigger = 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<TheVoidStatus>(false), ModHoverTips.VoidCounter];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardPile hand = PileTypeExtensions.GetPile(PileType.Hand, Owner);
        List<CardModel> voids = hand.Cards
            .Where(c => c is TheVoidStatus || c is MegaCrit.Sts2.Core.Models.Cards.Void)
            .ToList();

        if (voids.Count == 0)
        {
            return;
        }

        foreach (CardModel v in voids)
        {
            await CardCmd.Exhaust(choiceContext, v);
        }

        // 整数除法：落单的 1 张虚空牌不会额外提供资源或抽牌。
        int triggerCount = voids.Count / _voidsPerTrigger;
        if (triggerCount == 0)
        {
            return;
        }

        await VoidResource.Gain(Owner, triggerCount * _voidGainPerTrigger);
        await CardPileCmd.Draw(choiceContext, triggerCount * _drawPerTrigger, Owner);
    }

    protected override void OnUpgrade()
    {
        _voidGainPerTrigger = 2;
        _drawPerTrigger = 2;
    }
}
