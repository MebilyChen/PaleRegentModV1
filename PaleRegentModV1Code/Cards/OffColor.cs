using MegaCrit.Sts2.Core.HoverTips;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Patches;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【异色】：造成 5（升级后 7）点伤害 N 次；N 为本回合获得的虚空数。
/// </summary>
public class OffColor() : PaleRegentModV1Card(
    0,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost, ModHoverTips.VoidCounter];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new CurrentVoidCountVar()
    ];

    /// <summary>
    /// 牌面和结算共用这个方法，保证“次数”与实际攻击段数一致。
    /// </summary>
    private static int GetHitCount()
    {
        return Math.Max(0, VoidPowerListener.VoidGainedThisTurn);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        int hits = GetHitCount();
        for (int i = 0; i < hits; i++)
        {
            if (cardPlay.Target is not { IsAlive: true })
            {
                break;
            }

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1),
            (CardModel c) => c != this && CardTraits.CanApplyLost(c),
            this);

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyLost(card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }

    /// <summary>
    /// 将本回合已获得虚空数作为描述中的 {Amount} 动态变量。
    /// </summary>
    private sealed class CurrentVoidCountVar : DynamicVar
    {
        public CurrentVoidCountVar() : base("Amount", 0m)
        {
        }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = GetHitCount();
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return GetHitCount();
        }

        public override string ToString()
        {
            return GetHitCount().ToString();
        }
    }
}