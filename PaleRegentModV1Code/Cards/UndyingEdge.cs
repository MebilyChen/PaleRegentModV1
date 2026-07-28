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
/// 将 1 张【不灭锋刃】从弃牌堆/消耗堆返回手牌。
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
    /// 每凑满 3 张，把 1 张不灭锋刃（优先弃牌堆、其次消耗堆）返回手牌。
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

        // 找一张在弃牌堆或消耗堆里的不灭锋刃（本卡优先）
        CardModel? toReturn = FindReturnable();
        if (toReturn == null)
        {
            return;
        }

        await CardPileCmd.Add(toReturn, PileType.Hand, CardPilePosition.Top, null, false);
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

    private CardModel? FindReturnable()
    {
        foreach (PileType pile in new[] { PileType.Discard, PileType.Exhaust })
        {
            foreach (CardModel c in CardPile.GetCards(Owner, pile))
            {
                if (c is UndyingEdge)
                {
                    return c;
                }
            }
        }
        return null;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
