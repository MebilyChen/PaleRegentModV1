using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空打击】初始牌组特色攻击牌（体现"虚空"双资源机制的入门卡）。
/// 1 灵魂：造成 3 点伤害，获得 1 点虚空，然后选择一张手牌附加【失心】。
/// 升级后伤害 +2。
///
/// 机制要点：
/// - 虚空是"先攒后花"的资源，这张牌是主要的产出手段之一；
///   回合开始时 VoidPower 会按虚空层数扣除等量灵魂（欠债机制）。
/// - 【失心】：目标牌灵魂费清零、并入虚空费，获得重放1（见 CardTraits.ApplyLost）。
///   X 费牌无法被附加失心（选择界面会过滤掉）。
///
/// 修改指南：
/// - 改伤害：BaseDamage / UpgradeDamageBonus 常量。
/// - 改虚空获得量：VoidGain 常量。
/// - 选牌提示文案：cards.json 里 PALEREGENTMODV1-VOID_STRIKE.selectionScreenPrompt。
/// </summary>
public class VoidStrike() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Basic,
    TargetType.AnyEnemy)
{
    /// <summary>基础伤害。</summary>
    private const int BaseDamage = 3;
    /// <summary>升级后伤害增加量。</summary>
    private const int UpgradeDamageBonus = 2;
    /// <summary>打出后获得的虚空数量。</summary>
    private const int VoidGain = 1;

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

        // 2. 获得虚空并同步展示层（VoidPower 图标）
        await VoidResource.Gain(cardPlay.Player, VoidGain);
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);

        // 3. 从手牌选择 1 张牌附加【失心】
        //    filter：过滤掉不能失心的牌（X 费牌）和自己
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
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
}
