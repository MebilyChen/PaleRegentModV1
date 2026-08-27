using PaleRegentModV1.PaleRegentModV1Code.Traits;
using MegaCrit.Sts2.Core.HoverTips;
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
/// 【疫收】技能牌（机制文档：瘟疫流）。
/// [gold]消耗[/gold]手牌中所有的[gold]感染[/gold]，每张获得{IfUpgraded:show:3|2}点格挡并抽{IfUpgraded:show:2|1}张牌。
/// </summary>
public class PlagueHarvest() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    private const int BaseBlockPerInfection = 2;
    private const int BaseDrawPerInfection = 1;

    /// <summary>升级后每张感染额外提供的格挡与抽牌数量。</summary>
    private int _blockBonus;
    private int _drawBonus;

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    public override bool GainsBlock => true;

    /// <summary>手牌聚焦悬停词条。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Infection>(false),
         HoverTipFactory.Static(StaticHoverTip.Block)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> infections = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c is Infection ||
                        c is MegaCrit.Sts2.Core.Models.Cards.Infection)
            .ToList();

        if (infections.Count == 0)
        {
            return;
        }

        foreach (CardModel infection in infections)
        {
            await CardCmd.Exhaust(choiceContext, infection);
        }

        await CreatureCmd.GainBlock(
            Owner.Creature,
            infections.Count * (BaseBlockPerInfection + _blockBonus),
            ValueProp.Move,
            cardPlay);

        await CardPileCmd.Draw(
            choiceContext,
            infections.Count * (BaseDrawPerInfection + _drawBonus),
            cardPlay.Player);
    }

    protected override void OnUpgrade()
    {
        _blockBonus = 1;
        _drawBonus = 1;
    }
}

/* 原逻辑：
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【疫收】技能牌（机制文档：瘟疫流）。
/// 1 灵魂 技能：消耗手牌中所有【感染】，~~每张获得 1 点虚空~~ 每张获得格挡并抽 1 张牌。
/// 升级后：每张获得 2 点虚空并抽 2 张牌。
/// </summary>
public class PlagueHarvest() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    private const int BaseVoidPerInfection = 1;
    private const int BaseDrawPerInfection = 1;

    /// <summary>升级后每张感染额外虚空/抽牌。</summary>
    private int _voidBonus;
    private int _drawBonus;
    
    // 带 Defend 标签：与“对防御牌生效”的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    public override bool GainsBlock => true;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Infection>(false), //, HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Infection>(false)
         ModHoverTips.VoidCounter];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> infections = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c is Infection||
                        c is MegaCrit.Sts2.Core.Models.Cards.Infection)
            .ToList();
        if (infections.Count == 0)
        {
            return;
        }

        foreach (CardModel infection in infections)
        {
            await CardCmd.Exhaust(choiceContext, infection);
        }

        await VoidResource.Gain(Owner, infections.Count * (BaseVoidPerInfection + _voidBonus));
        await VoidResource.SyncPower(choiceContext, Owner, this);

        await CardPileCmd.Draw(choiceContext,
            infections.Count * (BaseDrawPerInfection + _drawBonus), cardPlay.Player);
    }

    protected override void OnUpgrade()
    {
        _voidBonus = 1;
        _drawBonus = 1;
    }
}
*/