using MegaCrit.Sts2.Core.HoverTips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【疫刃】攻击牌（机制文档：卡牌表 C#29，20260725 批次改版）。
/// 1 灵魂 攻击：消耗手牌中全部【感染】；造成 5 点伤害，
/// 消耗牌堆中每有 1 张【感染】，伤害 +3（含本次刚吞下的）。
/// 升级后：8 点伤害，每张 +5（表格 G33/H33/P33/Q33）。
/// </summary>
public class PlagueBlade() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;

    /// <summary>每张已消耗感染的额外伤害（基础 3，升级 5）。</summary>
    private int _damagePerInfection = 3;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Infection>(false)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 消耗手牌中所有【感染】
        List<CardModel> infections = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c is Infection)
            .ToList();
        foreach (CardModel infection in infections)
        {
            await CardCmd.Exhaust(choiceContext, infection);
        }

        // 20260725 批次：改为按"消耗牌堆中的感染数"计伤（含本次刚吞的）
        int exhaustedInfections = CardPile.GetCards(Owner, PileType.Exhaust)
            .Count(c => c is Infection);
        decimal damage = DynamicVars.Damage.BaseValue +
            exhaustedInfections * _damagePerInfection;
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        _damagePerInfection = 5;
    }
}
