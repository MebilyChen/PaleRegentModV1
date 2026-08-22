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
/// 【召回精锐】技能牌。
/// 1 灵魂 + 1 虚空：从消耗牌堆中选择至多 2（升级后 3）张造物牌，添加苍白后放回手牌。
/// 造物判定：KingsRetainer / WingedRetainerCard / PureVessel / Vessel /
/// VoidGivenFocus / VoidGivenForm / FailedExperiment。
/// </summary>
public class EliteRecall : PaleRegentModV1Card
{
    private const int VoidCost = 1;

    /// <summary>放回张数；升级后为 3。</summary>
    private int _recallCount = 2;

    public EliteRecall() : base(
        1,
        CardType.Skill,
        CardRarity.Common,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        ModHoverTips.CreationRule
    ];
   
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // 先筛出符合“造物”定义的消耗牌；选择界面只会显示这些牌。
        List<CardModel> eligibleCards = CardPile
            .GetCards(Owner, PileType.Exhaust)
            .Where(IsCreationCard)
            .ToList();

        if (eligibleCards.Count == 0)
        {
            return;
        }

        // 牌数不足时，改为选择全部可选牌，避免要求玩家选择不存在的第 2/3 张。
        int selectionCount = System.Math.Min(_recallCount, eligibleCards.Count);
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt,0, selectionCount);

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            eligibleCards,
            Owner,
            prefs);

        foreach (CardModel card in selectedCards)
        {
            //添加苍白
            CardTraits.ApplyPale(card);
            // Add 会将该 CardModel 从其当前牌堆转入手牌，无须另行手动移出消耗牌堆。
            await CardPileCmd.Add(
                card,
                PileType.Hand,
                CardPilePosition.Top,
                null,
                false);
        }
    }

    /// <summary>判断卡牌是否属于本效果可选择的“造物”牌。</summary>
    private static bool IsCreationCard(CardModel card) => card is
        KingsRetainer or
        WingedRetainerCard or
        PureVessel or
        Vessel or
        VoidGivenFocus or
        VoidGivenForm or
        FailedExperiment;

    protected override void OnUpgrade()
    {
        _recallCount = 3;
    }
}
