using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【不灭锋刃】攻击牌（表 C#81，0727 新增）。
/// 0 灵魂：造成 6 点伤害。本回合中你每消耗 3 张牌，
/// 将 1 张【不灭锋刃】从抽牌/弃牌堆/消耗堆返回手牌。
/// 升级后：10 点伤害。
/// 备注：计数为回合级（回合结束清零），"每消耗 3 张"每凑满一组触发一次；
/// 返回优先从弃牌堆找，其次消耗堆。
/// </summary>
public class UndyingEdge() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 6;
    private const int UpgradeDamageBonus = 4;
    private const int ExhaustGroupSize = 3;

    /// <summary>本回合消耗牌计数（挂在卡实例上，回合开始清零）。</summary>
    private int _exhaustedThisTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>
    /// 全局消耗钩子：任何一张牌被消耗时触发（含本卡自身被消耗的场景）。
    /// 每凑满 3 张，把 全部的不灭锋刃（无论弃牌堆、抽牌堆、消耗堆）返回手牌。
    /// </summary>
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner != Owner)
        {
            return;
        }

        _exhaustedThisTurn++;
        if (_exhaustedThisTurn % ExhaustGroupSize != 0)
        {
            return;
        }

        // 先建立快照，避免移动卡牌时修改正在遍历的牌堆集合。
        List<CardModel> cardsToReturn = FindAllReturnable();
        foreach (CardModel cardToReturn in cardsToReturn)
        {
            await CardPileCmd.Add(
                cardToReturn,
                PileType.Hand,
                CardPilePosition.Top,
                null,
                false);
        }
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        // 回合开始清零回合级计数（等价于“本回合内”口径）
        if (player == Owner)
        {
            _exhaustedThisTurn = 0;
        }
        return Task.CompletedTask;
    }

    private List<CardModel> FindAllReturnable()
    {
        List<CardModel> result = [];

        foreach (PileType pile in new[]
                 {
                     PileType.Discard,
                     PileType.Draw,
                     PileType.Exhaust
                 })
        {
            foreach (CardModel card in CardPile.GetCards(Owner, pile))
            {
                if (card is UndyingEdge)
                {
                    result.Add(card);
                }
            }
        }

        return result;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
