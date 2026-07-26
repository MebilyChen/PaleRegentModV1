using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【容器】生成牌（表格设计：造物流，"容器计划"每回合生成 / 不惜代价 / 容器药水）。
/// 0 灵魂 技能：对一个敌人施加 1 层【纯粹封印】；
/// 消耗手牌中所有状态牌：少于 3 张 → 孕育出【失败容器】，
/// 3 张及以上 → 孕育出【纯粹容器】。消耗。
/// 升级后：施加 2 层纯粹封印。
///
/// 实现说明：孕育结果以新卡形式加入弃牌堆，本卡照常消耗。
/// </summary>
public class Vessel() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Token,
    TargetType.AnyEnemy)
{
    private const int BaseSeal = 1;
    private const int PureThreshold = 3;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<PureVessel>(IsUpgraded),
         HoverTipFactory.FromCard<FailedVessel>(IsUpgraded)];

    public override bool IsCreationCard => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PureSealPower>(BaseSeal)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 对一个敌人施加纯粹封印（1 层，升级 2 层）
        if (cardPlay.Target != null)
        {
            await PowerCmd.Apply<PureSealPower>(choiceContext, cardPlay.Target,
                DynamicVars["PureSealPower"].BaseValue, Owner.Creature, this);
        }

        // 2. 吞噬手牌中所有状态牌（表格："状态牌"，不限于感染）
        List<CardModel> statuses = CardPile.GetCards(Owner, PileType.Hand)
            .Where((CardModel c) => c.Type == CardType.Status && c != this)
            .ToList();
        foreach (CardModel status in statuses)
        {
            await CardCmd.Exhaust(choiceContext, status);
        }

        // 3. 按吞噬数量孕育结果，加入弃牌堆
        if (statuses.Count >= PureThreshold)
        {
            await CardPileCmd.AddToCombatAndPreview<PureVessel>(Owner.Creature, PileType.Discard, 1, Owner);
        }
        else
        {
            await CardPileCmd.AddToCombatAndPreview<FailedVessel>(Owner.Creature, PileType.Discard, 1, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：2 层纯粹封印
        DynamicVars["PureSealPower"].UpgradeValueBy(1m);
    }
}
