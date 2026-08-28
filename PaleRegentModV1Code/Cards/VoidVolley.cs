using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空连射】攻击牌。
/// 0 灵魂 + X 虚空：造成 X 次 7 点伤害，并在手牌中生成 X+1 张【虚空】状态牌。
/// 从抽牌堆选择 X 张牌添加[gold]失心[/gold]。
/// 升级后：每段 10 点伤害，X+1 次；选择 X+1 张牌添加[gold]失心[/gold]，
/// </summary>
public class VoidVolley : PaleRegentModV1Card
{
    private const int DamagePerHit = 7;

    /// <summary>升级后的额外段数与额外失心选择数（X+1）。</summary>
    private int _bonusHits;

    public VoidVolley() : base(
        0,
        CardType.Attack,
        CardRarity.Uncommon,
        TargetType.AnyEnemy)
    {
        CardTraits.SetVoidCostX(this);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(DamagePerHit, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost, HoverTipFactory.FromCard<TheVoidStatus>(false)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 从本次二级资源支付账本读取实际消耗的【虚空】数量作为 X。
        int x = 0;
        if (cardPlay.TryGetSecondaryResources(
                out SecondaryResourcePlayLedger ledger))
        {
            x = ledger.Spent(VoidResource.Id);
        }

        // 支付虚空后同步其对应能力的数值与显示。
        await VoidResource.SyncPower(
            choiceContext,
            cardPlay.Player,
            this);

        if (x <= 0)
        {
            return;
        }

        // 1. X（升级后 X+1）段伤害。
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(x + _bonusHits)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 手牌生成 X 张【虚空】状态牌。
        await CardPileCmd.AddToCombatAndPreview<TheVoidStatus>(
            Owner.Creature,
            PileType.Hand,
            x + _bonusHits,
            Owner);

        // 3. 从抽牌堆选择 X 张（升级后 X+1 张）可附加【失心】的牌。
        // 若合格牌数量不足，FromSimpleGrid 会直接返回全部合格牌。
        int lostCount = x + _bonusHits;
        List<CardModel> eligibleCards = PileType.Draw.GetPile(cardPlay.Player).Cards
            .Where(c => CardTraits.CanApplyLost(c))
            .ToList();

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            eligibleCards,
            cardPlay.Player,
            new CardSelectorPrefs(SelectionScreenPrompt, lostCount));

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyLost(card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        _bonusHits = 1;
    }
}
