using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【弃壳】技能牌（蓄灵载体）。
/// X 灵魂：结束回合；获得 X 层【蓄灵】；在本回合结束时，保留手牌前 X 张牌。
/// 升级后：蓄灵和保留数量均为 X + 1。
/// </summary>
public class CastOffShell() : PaleRegentModV1Card(
    0,
    CardType.Skill,
    CardRarity.Token,
    TargetType.Self)
{
    /// <summary>手牌聚焦悬停词条。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SoulNextTurnPower>((int?)null)];

    protected override bool HasEnergyCostX => true;

    /// <summary>升级后，蓄灵及保留数量各增加 1。</summary>
    private int _soulBonus;

    /// <summary>本次打出后、在本回合结束时需要保留的手牌数量。</summary>
    private int _retainCountThisTurn;

    /// <summary>
    /// 仅记录本效果亲自添加了 Retain 关键词的牌。
    /// 下回合开始时只移除这些牌的关键词，避免误移除牌本身自带的保留。
    /// </summary>
    private readonly HashSet<CardModel> _temporarilyRetainedCards = new();

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // 读取本次支付的 X（打出时已经扣除）。升级后两个效果均为 X + 1。
        int x = ResolveEnergyXValue();
        int amount = x + _soulBonus;

        if (amount > 0)
        {
            await PowerCmd.Apply<SoulNextTurnPower>(
                choiceContext,
                Owner.Creature,
                amount,
                Owner.Creature,
                this);
        }

        // 记录本次 X；真正添加保留关键词必须等到弃牌前的 BeforeFlush。
        _retainCountThisTurn = amount;

        // 结束你的回合（EndTurn 返回 void，不能 await）。
        PlayerCmd.EndTurn(Owner, false, (Func<Task>)null);
    }

    /// <summary>
    /// 手动结束回合后的弃牌结算前触发。Cards 的顺序即手牌当前顺序，
    /// Take(N) 会处理其中前 N 张。
    /// </summary>
    public override Task BeforeFlush(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner || _retainCountThisTurn <= 0)
        {
            return Task.CompletedTask;
        }

        foreach (CardModel card in PileType.Hand
                     .GetPile(player)
                     .Cards
                     .Take(_retainCountThisTurn))
        {
            // 原本自带 Retain 的牌无需标记，也绝不能在下回合移除它的 Retain。
            if (card.Keywords.Contains(CardKeyword.Retain))
            {
                continue;
            }

            card.AddKeyword(CardKeyword.Retain);
            _temporarilyRetainedCards.Add(card);
        }

        // 防止同一张弃壳在后续 Flush 中重复处理。
        _retainCountThisTurn = 0;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 已借由 Retain 跨过一次弃牌结算后，于下回合开始移除本效果临时加上的关键词。
    /// </summary>
    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner || _temporarilyRetainedCards.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (CardModel card in _temporarilyRetainedCards)
        {
            card.RemoveKeyword(CardKeyword.Retain);
        }

        _temporarilyRetainedCards.Clear();
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        _soulBonus = 1;
    }
    //不进入奖励池
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<TokenCardPool>();
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;
}
