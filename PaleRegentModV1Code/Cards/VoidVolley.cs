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

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空连射】攻击牌（表 C#89，0727 新增）。
/// X 灵魂：造成 X 次 7 点伤害，并在手牌中生成 X 张【虚空】状态牌。选择抽牌堆{IfUpgraded:show:X+1|X}张牌添加[gold]失心[/gold]。
/// 升级后：每段 10 点伤害，X+1 次（生成张数仍为 X）。
/// 备注：表格升级栏"10x(X+1)"按"段数 X+1、生成 X 张"实现；
/// 若生成张数也要 X+1 请告知。
/// </summary>
public class VoidVolley() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int DamagePerHit = 7;

    protected override bool HasEnergyCostX => true;

    /// <summary>升级后的额外段数与额外失心选择数（X+1）。</summary>
    private int _bonusHits;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(DamagePerHit, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost, HoverTipFactory.FromCard<TheVoidStatus>(false)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int x = ResolveEnergyXValue();
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

        // 2. 手牌生成 X 张虚空状态牌。
        await CardPileCmd.AddToCombatAndPreview<TheVoidStatus>(
            Owner.Creature,
            PileType.Hand,
            x,
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
