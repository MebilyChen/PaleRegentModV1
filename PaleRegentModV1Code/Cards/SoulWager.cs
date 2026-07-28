using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂典当】技能牌（表 C#85，0727 新增）。
/// X 灵魂：从弃牌堆选择 1 张牌，使其获得重放 X 并获得消耗；
/// 战斗结束时若该牌在消耗牌堆中，则将其从卡组移除（本场生成的牌不移除，
/// 因为生成牌本来就不在卡组里）。消耗。
/// 升级后：重放 X+1。
/// 备注：重放使用原版 BaseReplayCount 机制（打出时额外结算 N 次）；
/// "战斗结束移除"通过 DeckCmd/RunState 在战斗胜利结算时执行。
/// </summary>
public class SoulWager() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Rare,
    TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    /// <summary>升级后的额外重放次数（X+1）。</summary>
    private int _upgradeBonus;

    /// <summary>本场被典当的牌（战斗结束时检查是否在消耗堆）。</summary>
    private CardModel? _wageredCard;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.ReplayStatic)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue() + _upgradeBonus;
        if (x <= 0)
        {
            return;
        }

        CardPile discard = PileTypeExtensions.GetPile(PileType.Discard, Owner);
        if (!discard.Cards.Any())
        {
            return;
        }

        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
            choiceContext, discard, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            (System.Func<CardModel, bool>)((CardModel _) => true));

        foreach (CardModel card in selected)
        {
            card.BaseReplayCount += x;
            card.AddKeyword(CardKeyword.Exhaust);
            _wageredCard = card;
        }
    }

    /// <summary>
    /// 战斗结束钩子：若被典当的牌在消耗堆里且存在卡组对应牌
    /// （DeckVersion，本场生成牌无 DeckVersion），则从卡组移除。
    /// </summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);

        if (_wageredCard == null)
        {
            return;
        }

        CardModel card = _wageredCard;
        _wageredCard = null;

        // 注意：此钩子触发时战斗牌堆可能已清理，用打出时记录的引用判断
        bool inExhaust = card.Pile != null && card.Pile.Type == PileType.Exhaust;
        CardModel? deckCard = card.DeckVersion;
        if (inExhaust && deckCard != null && deckCard.Pile != null
            && deckCard.Pile.Type == PileType.Deck)
        {
            // 本场生成的牌没有 DeckVersion，天然不会被移除（符合表格备注）
            await CardPileCmd.RemoveFromDeck(deckCard);
        }
    }

    protected override void OnUpgrade()
    {
        _upgradeBonus = 1;
    }
}
