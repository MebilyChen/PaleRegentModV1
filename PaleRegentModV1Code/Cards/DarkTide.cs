using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【黑暗潮汐】（表格 C#8，20260725 新增）—— 虚空打击的先古升级。
/// 0 灵魂 0 虚空 攻击：面前每有一个敌人，对全体敌人造成 1 次 10(15) 点伤害，
/// 且每次虚空 +1。然后为手牌任意张牌添加【失心】。
///
/// 实现说明：
/// - 稀有度：表格 L8=Ancient（先古）。已在原版源码 CardRarity.cs 确认枚举含 Ancient。
/// - "先古升级"（初始牌升级为此卡）的官方关联 API 未知，
///   目前作为独立卡牌实现，进入卡池方式待游戏内验证后调整。
/// - "任意张"选牌：以当前手牌数为选择上限（可少选）。
/// </summary>
public class DarkTide() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Ancient, // 表格 L8：先古（原版枚举已确认）
    TargetType.AllEnemies)
{
    /// <summary>基础伤害（每次）。</summary>
    private const int BaseDamage = 10;

    /// <summary>升级后伤害增加量（10 → 15）。</summary>
    private const int UpgradeDamageBonus = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState;
        if (combatState == null)
        {
            return;
        }

        // 1. 面前每有一个敌人 → 重复 N 次：对全体敌人造成伤害 + 虚空 +1
        int repeats = combatState.HittableEnemies.Count;
        for (int i = 0; i < repeats; i++)
        {
            if (combatState.HittableEnemies.Count == 0)
            {
                break;
            }
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(combatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            await VoidResource.Gain(cardPlay.Player, 1);
        }
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);

        // 2. 为手牌任意张牌添加【失心】（上限 = 手牌数，可少选）
        List<CardModel> hand = CardPile.GetCards(Owner, PileType.Hand).ToList();
        int max = hand.Count(c => c != this && CardTraits.CanApplyLost(c));
        if (max <= 0)
        {
            return;
        }
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, max), // 0~max张可少选（原版 Purity 同款重载）
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
