using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【钢魂模式】能力牌。
/// 固有。虚无。
/// 从玩家永久牌组中选择一张可移除的牌，使其对应的战斗副本离开本场战斗。
/// 战斗结束后，将原牌和一张复制品加入牌组。
/// 升级后：4 点能量消耗降为 3 点。
/// </summary>
public sealed class SteelSoul : PaleRegentModV1Card
{
    private const int EnergyCostValue = 4;

    public SteelSoul() : base(
        EnergyCostValue,
        CardType.Power,
        CardRarity.Rare,
        TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<SteelSoulPower>(1)];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Innate, CardKeyword.Ethereal];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 选择源明确使用永久牌组；本场战斗临时生成的牌不会出现在此列表中。
        var deckCards = Owner.Deck.Cards
            .Where(card => card.IsRemovable)
            .ToList();
        var combatState = Owner.Creature.CombatState;
        if (deckCards.Count == 0 || combatState == null)
        {
            return;
        }

        // 永久牌组中的卡牌没有 CombatState。直接将其交给战斗内网格预览时，
        // 含 X 值等动态变量的牌会在预览阶段访问空 CombatState，导致选择界面无法完成。
        // 因此先为每张候选原牌建立只用于界面展示的战斗副本，并以 DeckVersion 回指原牌。
        var previewCards = deckCards
            .Select(deckCard =>
            {
                var previewCard = combatState.CloneCard(deckCard);
                previewCard.DeckVersion = deckCard;
                previewCard.UpgradePreviewType = CardUpgradePreviewType.Combat;
                return previewCard;
            })
            .ToList();

        CardModel? selectedCard;
        try
        {
            var selectedPreview = (
                await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    previewCards,
                    Owner,
                    new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)
                )
            ).FirstOrDefault();
            selectedCard = selectedPreview?.DeckVersion;
        }
        catch (TaskCanceledException)
        {
            return;
        }
        finally
        {
            // 预览副本从未加入任一战斗卡堆；选择结束后必须从 CombatState 清理。
            foreach (var previewCard in previewCards)
            {
                if (combatState.ContainsCard(previewCard))
                {
                    combatState.RemoveCard(previewCard);
                }
            }
        }

        if (selectedCard == null)
        {
            return;
        }

        // 仅从永久牌组的卡堆中取出原牌，不调用 RemoveFromDeck，避免将其从 RunState 永久删除。
        selectedCard.RemoveFromCurrentPile();

        // 移除与原牌关联的战斗副本；不会影响战斗中额外生成的牌，也不会进入消耗牌堆。
        var combatVersions = Owner.PlayerCombatState.AllCards
            .Where(card => card.DeckVersion == selectedCard)
            .ToList();
        await CardPileCmd.RemoveFromCombat(combatVersions);

        var power = await PowerCmd.Apply<SteelSoulPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["SteelSoulPower"].BaseValue,
            Owner.Creature,
            this);
        power?.SetSelectedCard(selectedCard);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
