using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空实验】技能牌（机制文档：造物流）。
/// 0 灵魂 + X 虚空：X ≥ 2 → 变为【虚空化形】；
/// 否则 → 变为【失败实验】。
/// 变形后的牌回到手牌中。
/// 升级后：变形后的牌为升级版（虚空化形+/失败实验+）。
/// </summary>
public class VoidExperiment : PaleRegentModV1Card
{
    private const int SuccessThreshold = 2;

    // 出牌过程中不能直接变形当前牌；先记录目标牌，待出牌流程将本牌直接放回手牌后再替换。
    private CardModel? _pendingTransformation;

    public VoidExperiment() : base(0,
        CardType.Skill, CardRarity.Common,
        TargetType.Self)
    {
        CardTraits.SetVoidCostX(this);
    }

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<VoidGivenForm>(IsUpgraded),
         HoverTipFactory.FromCard<FailedExperiment>(IsUpgraded),
         ModHoverTips.VoidCounter];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = 0;
        if (cardPlay.TryGetSecondaryResources(out SecondaryResourcePlayLedger ledger))
        {
            x = ledger.Spent(VoidResource.Id);
        }

        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);

        // 记录目标牌。本牌会在出牌流程结束时直接回到手牌，再安全替换。
        CardModel transformed = x >= SuccessThreshold
            ? Owner.Creature.CombatState.CreateCard<VoidGivenForm>(Owner)
            : Owner.Creature.CombatState.CreateCard<FailedExperiment>(Owner);

        // 升级后：变化得到升级版（虚空化形+/失败实验+）。
        if (IsUpgraded)
        {
            CardCmd.Upgrade(transformed, (CardPreviewStyle)1);
        }

        _pendingTransformation = transformed;
    }

    // 与【粒子墙】相同：若本牌原本会进入弃牌堆，则直接将其结算位置改为手牌。
    protected override CardLocation GetResultLocationForCardPlay()
    {
        CardLocation resultLocation = base.GetResultLocationForCardPlay();
        if (resultLocation.pileType == PileType.Discard)
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
        // 升级：变化得到升级版牌（见 OnPlay 的 IsUpgraded 分支）。
    }
}
