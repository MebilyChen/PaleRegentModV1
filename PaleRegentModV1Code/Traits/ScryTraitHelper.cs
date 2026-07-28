using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 预知 / 查看抽牌堆顶牌的通用工具。
///
/// 提供两种效果：
/// 1. PreviewTopAndTakeOne：查看抽牌堆顶 X 张，选择 1 张加入手牌。
/// 2. Scry：查看抽牌堆顶 X 张，选择任意张弃置；未选择的牌留在抽牌堆中。
///
/// 注意：
/// - 抽牌堆为空时会先尝试将弃牌堆洗入抽牌堆。
/// - 抽牌堆非空但不足 X 张时，只查看现有的牌，不额外洗牌补足。
/// - 所有牌堆移动均通过游戏命令完成，避免绕过动画、事件及联机同步。
/// </summary>
public static class ScryTraitHelper
{
    /// <summary>
    /// 查看抽牌堆顶 previewAmount 张牌，
    /// 选择其中最多 takeAmount 张加入手牌。
    /// </summary>
    /// <returns>实际加入手牌的牌。</returns>
    public static async Task<IReadOnlyList<CardModel>> PreviewTopAndTake(
        PlayerChoiceContext choiceContext,
        Player player,
        int previewAmount,
        int takeAmount,
        CardSelectorPrefs prefs)
    {
        if (previewAmount <= 0 || takeAmount <= 0)
        {
            return [];
        }

        List<CardModel> topCards =
            await GetTopCards(
                choiceContext,
                player,
                previewAmount);

        if (topCards.Count == 0)
        {
            return [];
        }

        // 抽牌堆可能不足 takeAmount 张。
        int actualTakeAmount = System.Math.Min(
            takeAmount,
            topCards.Count);

        List<CardModel> selectedCards =
            (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                topCards,
                player,
                prefs))
            .Take(actualTakeAmount)
            .ToList();

        List<CardModel> movedCards = [];

        foreach (CardModel card in selectedCards)
        {
            // 避免选牌结束后，这张牌已经被其他效果移动。
            if (card.Pile?.Type != PileType.Draw)
            {
                continue;
            }

            await CardPileCmd.Add(card, PileType.Hand);
            movedCards.Add(card);
        }

        return movedCards;
    }

    /// <summary>
    /// STS1 风格的“预知”：
    /// 查看抽牌堆顶 amount 张牌，选择任意张弃置。
    /// 未选择的牌仍留在抽牌堆中，并保持它们彼此之间的原顺序。
    /// </summary>
    /// <param name="choiceContext">当前玩家选择上下文。</param>
    /// <param name="player">执行效果的玩家。</param>
    /// <param name="amount">查看抽牌堆顶的牌数。</param>
    /// <param name="prefs">
    /// 选择界面的设置。通常将最大选择数设为 amount：
    /// new CardSelectorPrefs(SelectionScreenPrompt, amount)
    /// </param>
    /// <returns>实际被弃置的牌。</returns>
    public static async Task<IReadOnlyList<CardModel>> Scry(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount,
        CardSelectorPrefs prefs)
    {
        List<CardModel> topCards =
            await GetTopCards(choiceContext, player, amount);

        if (topCards.Count == 0)
        {
            return [];
        }

        List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            topCards,
            player,
            prefs)).ToList();

        if (selected.Count == 0)
        {
            return [];
        }

        List<CardModel> discarded = [];

        foreach (CardModel card in selected)
        {
            // 防止选择界面结束后，其他效果已经移动了该牌。
            if (card.Pile?.Type != PileType.Draw)
            {
                continue;
            }

            await CardCmd.Discard(choiceContext, card);
            discarded.Add(card);
        }

        return discarded;
    }

    /// <summary>
    /// 取得抽牌堆顶 amount 张牌的快照。
    /// 抽牌堆为空时，先尝试执行标准洗牌逻辑。
    /// </summary>
    private static async Task<List<CardModel>> GetTopCards(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount)
    {
        if (amount <= 0 ||
            player.PlayerCombatState == null ||
            player.Creature.CombatState == null)
        {
            return [];
        }

        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

        return PileType.Draw
            .GetPile(player)
            .Cards
            .Take(amount)
            .ToList();
    }
}
