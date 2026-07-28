using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【证明价值】技能牌（表 C#100，0727 新增，多人协作牌）。
/// 1 灵魂：选择一名其他玩家的 1 张手牌，使其获得 1 次【重放】。消耗。
/// 升级后：可选择 2 张。
/// 备注：单人游戏时退化为选择自己的手牌（表格未说明单人行为，已按兜底处理，见条目备注）。
/// </summary>
public class ShowYourWorth : PaleRegentModV1Card
{
    private int _selectCount = 1;

    public ShowYourWorth() : base(1,
        CardType.Skill, CardRarity.Uncommon,
        TargetType.Self)
    {
        AddKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 找到另一名玩家（多人协作）；单人时兜底为自己
        List<Player> others = CombatState!.PlayerCreatures
            .Select(c => c.Player)
            .OfType<Player>()
            .Where(p => p != Owner)
            .ToList();
        Player targetPlayer = others.Count > 0
            ? Owner.RunState.Rng.CombatTargets.NextItem(others)
            : Owner;

        CardPile hand = PileTypeExtensions.GetPile(PileType.Hand, targetPlayer);
        List<CardModel> candidates = hand.Cards.Where(c => c != this).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        int count = Math.Min(_selectCount, candidates.Count);
        List<CardModel> selected = (await CardSelectCmd.FromCombatPile(
            choiceContext, hand, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, count),
            (Func<CardModel, bool>)((CardModel c) => c != this))).ToList();

        foreach (CardModel card in selected)
        {
            // 获得 1 次重放（本场有效）
            card.BaseReplayCount += 1;
        }
    }

    protected override void OnUpgrade()
    {
        _selectCount = 2;
    }
}
