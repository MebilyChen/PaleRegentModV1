using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂法术】普通攻击牌（苍白特质的载体）。
/// 1 灵魂：造成 7 点伤害，选择一张手牌附加【苍白】。
///
/// 机制要点：
/// - 【苍白】：取消失心（恢复灵魂费）、清空虚空费，获得【虚无】
///   （见 Traits/CardTraits.ApplyPale）。
/// - 用途：给失心过的牌"解债"，或主动给关键牌上虚无换取清掉虚空费。
///
/// 修改指南：
/// - 伤害：BaseDamage / UpgradeDamageBonus 常量。
/// - 选牌提示文案：cards.json 的 PALEREGENTMODV1-SOUL_SPELL.selectionScreenPrompt。
/// </summary>
public class SoulSpell() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Common,
    TargetType.AnyEnemy)
{
    /// <summary>基础伤害。</summary>
    private const int BaseDamage = 7;
    /// <summary>升级后伤害增加量。</summary>
    private const int UpgradeDamageBonus = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 1. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 从手牌选择 1 张牌附加【苍白】（排除自己；苍白对任何牌都可附加）
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            (CardModel c) => c != this,
            this);

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyPale(card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
