using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【钢魂模式】能力。
/// 记录被暂时移出永久牌组的卡牌，并在战斗结束后返还该原牌及其复制品。
/// </summary>
public sealed class SteelSoulPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData() => new Data();

    /// <summary>
    /// 展示被移除牌的提示信息。
    /// 此处展示的是战斗状态中的提示副本，而非永久牌组原牌，防止含战斗动态变量的牌在预览时访问空 CombatState。
    /// </summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var hoverTipCard = GetInternalData<Data>().HoverTipCard;
            return hoverTipCard == null ? [] : [HoverTipFactory.FromCard(hoverTipCard)];
        }
    }

    /// <summary>
    /// 战斗结束后将原牌放回永久牌组，再按正常获得卡牌流程加入一张复制品。
    /// 原牌通过底层牌堆操作返还，避免被误记为本次战斗中新获得的卡牌。
    /// </summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        var owner = Owner.Player;
        var data = GetInternalData<Data>();
        var selectedCard = data.SelectedCard;
        if (owner == null || selectedCard == null)
        {
            return;
        }

        Flash();

        owner.Deck.AddInternal(selectedCard);

        var copiedCard = owner.RunState.CloneCard(selectedCard);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(copiedCard, PileType.Deck)
        );

        var combatState = Owner.CombatState;
        if (data.HoverTipCard != null && combatState != null && combatState.ContainsCard(data.HoverTipCard))
        {
            combatState.RemoveCard(data.HoverTipCard);
        }
    }

    /// <summary>
    /// 保存被移除的永久牌组原牌，并创建仅供本能力悬浮信息使用的战斗提示副本。
    /// </summary>
    public void SetSelectedCard(CardModel card)
    {
        var data = GetInternalData<Data>();
        data.SelectedCard = card;

        var combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }

        data.HoverTipCard = combatState.CloneCard(card);
        data.HoverTipCard.DeckVersion = card;
        data.HoverTipCard.UpgradePreviewType = CardUpgradePreviewType.Combat;
    }

    private sealed class Data
    {
        public CardModel? SelectedCard;
        public CardModel? HoverTipCard;
    }
}
