using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【失败容器】状态牌（表格设计：造物流，容器吸收状态牌不足时孕育）。
/// 1 灵魂 状态牌：无效果，打出仅消耗。Shame（生成时召回 Shame）。无升级。
/// </summary>
public class FailedVessel() : PaleRegentModV1Card(1,
    CardType.Status, CardRarity.Status,
    TargetType.Self)
{
    public override bool IsCreationCard => true;

    /// <summary>
    /// Shame 特质（君王之剑式）：此牌生成时，将你所有的 Shame 加入手牌（若没有则生成一张）。
    /// </summary>
    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.ShameRule,
            HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Shame>(false)];

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        await base.AfterCardGeneratedForCombat(card, creator); // 基类统一处理失心诅咒（LostDestiny）
        if (card == this)
        {
            await CurseTraitHelper.Summon<Shame>(Owner);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 表格设计：无效果，打出仅消耗（消耗由 CanonicalKeywords.Exhaust 处理）
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }
    
    //不进入奖励池
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<TokenCardPool>();
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;
}
