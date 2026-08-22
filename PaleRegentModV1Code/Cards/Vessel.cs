using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【容器】生成牌（表格设计：造物流，"容器计划"每回合生成 / 不惜代价 / 容器药水）。
/// 1 灵魂 技能：对一个敌人施加 1 层【纯粹封印】；
/// 消耗手牌中所有状态牌：少于 2 张 → 变为【失败容器】，
/// 2 张及以上 → 变为【纯粹容器】。
/// 变形后的牌回到手牌中。
/// 升级后：施加 2 层纯粹封印，且变化得到升级版容器牌。保留。
/// </summary>
public class Vessel() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Token,
    TargetType.AnyEnemy)
{
    private const int BaseSeal = 1;
    private const int PureThreshold = 2;

    // 出牌过程中不能直接变形当前牌；先记录目标牌，待出牌流程将本牌直接放回手牌后再替换。
    private CardModel? _pendingTransformation;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<PureVessel>(IsUpgraded),
         HoverTipFactory.FromCard<FailedVessel>(IsUpgraded)];

    public override bool IsCreationCard => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PureSealPower>(BaseSeal)];
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [CardKeyword.Retain] : [];

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

        // 【纯粹】的卡牌不能被变化。此时不保留延迟变形目标，
        // 并由 GetResultLocationForCardPlay 保持原版的弃牌堆结算。
        if (CardTraits.IsPure(this))
        {
            _pendingTransformation = null;
            return;
        }

        // 3. 记录变形目标。本牌会在出牌流程结束时直接回到手牌，再安全替换。
        CardModel transformed = statuses.Count >= PureThreshold
            ? Owner.Creature.CombatState.CreateCard<PureVessel>(Owner)
            : Owner.Creature.CombatState.CreateCard<FailedVessel>(Owner);

        // 升级后的【容器】变化为对应的升级版容器牌。
        if (IsUpgraded)
        {
            CardCmd.Upgrade(transformed);
        }

        _pendingTransformation = transformed;
    }

    // 非纯粹状态与【粒子墙】相同：若本牌原本会进入弃牌堆，则改为手牌；
    // 已附加【纯粹】时保留原版结算位置，因此会正常进入弃牌堆。
    protected override CardLocation GetResultLocationForCardPlay()
    {
        CardLocation resultLocation = base.GetResultLocationForCardPlay();
        if (!CardTraits.IsPure(this) && resultLocation.pileType == PileType.Discard)
        {
            resultLocation.pileType = PileType.Hand;
        }
        return resultLocation;
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        await base.AfterCardChangedPiles(card, oldPileType, clonedBy);

        // 仅在本牌完成出牌并已直接回到手牌后执行一次，避免干扰打出中的卡牌视图。
        if (card != this || Pile?.Type != PileType.Hand || _pendingTransformation == null)
        {
            return;
        }

        CardModel transformed = _pendingTransformation;
        _pendingTransformation = null;

        // 此时本牌已在手牌这一常规卡牌堆中，变形结果将直接保留在手牌。
        await CardCmd.Transform(this, transformed);
    }

    protected override void OnUpgrade()
    {
        // 升级：2 层纯粹封印；变化结果的升级处理见 OnPlay。
        DynamicVars["PureSealPower"].UpgradeValueBy(1m);
        // 升级后实际获得保留。
        CardCmd.ApplyKeyword(this, [CardKeyword.Retain]);
    }
    
    //不进入奖励池
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<TokenCardPool>();
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;
}
