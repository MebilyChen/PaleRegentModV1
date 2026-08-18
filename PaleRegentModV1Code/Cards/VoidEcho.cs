using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Patches;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空回声】技能牌。
/// 获得等同于本场战斗此前已获得虚空总量的虚空；消耗。
/// </summary>
public class VoidEcho() : PaleRegentModV1Card(
    3,
    CardType.Skill,
    CardRarity.Rare,
    TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost, HoverTipFactory.FromPower<VoidPower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CurrentVoidAmountVar()
    ];

    /// <summary>
    /// 牌面和结算共用的虚空获得量。
    /// 保持原逻辑：此值只代表本次打出前，本场已获得的虚空总量。
    /// </summary>
    private static int GetVoidAmount()
    {
        return Math.Max(0, CombatCounters.VoidGainedThisCombat);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 必须先读值再 Gain，避免本次 Gain 被提前计入而产生自我放大。
        int amount = GetVoidAmount();
        if (amount > 0)
        {
            await VoidResource.Gain(cardPlay.Player, amount);
            await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
        }

        List<CardModel> eligibleCards = PileType.Draw.GetPile(cardPlay.Player).Cards
            .Where(c => CardTraits.CanApplyLost(c))
            .ToList();

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            eligibleCards,
            cardPlay.Player,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, eligibleCards.Count));

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyLost(card);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    /// <summary>
    /// 为本地化中的 {Amount} 提供当前本场虚空累计值。
    /// </summary>
    private sealed class CurrentVoidAmountVar : DynamicVar
    {
        public CurrentVoidAmountVar() : base("Amount", 0m)
        {
        }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = GetVoidAmount();
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return GetVoidAmount();
        }

        public override string ToString()
        {
            return GetVoidAmount().ToString();
        }
    }
}