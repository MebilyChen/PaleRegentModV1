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
/// 0 灵魂 + X 虚空：X ≥ 2 → 将 1 张【虚空化形】加入手牌；
/// 否则 → 将 1 张【失败实验】加入手牌。消耗。
/// 升级后：生成的牌为升级版（虚空化形+/失败实验+）。
/// </summary>
public class VoidExperiment : PaleRegentModV1Card
{
    private const int SuccessThreshold = 2;

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

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = 0;
        if (cardPlay.TryGetSecondaryResources(out SecondaryResourcePlayLedger ledger))
        {
            x = ledger.Spent(VoidResource.Id);
        }
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);

        CardModel made = x >= SuccessThreshold
            ? Owner.Creature.CombatState.CreateCard<VoidGivenForm>(Owner)
            : Owner.Creature.CombatState.CreateCard<FailedExperiment>(Owner);

        // 升级后：生成升级版（虚空化形+/失败实验+）
        if (IsUpgraded)
        {
            CardCmd.Upgrade(made, (CardPreviewStyle)1);
        }
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(made, PileType.Hand, Owner, (CardPilePosition)1),
            2.2f, (CardPreviewStyle)1);
    }

    protected override void OnUpgrade()
    {
        // 升级：生成升级版牌（见 OnPlay 的 IsUpgraded 分支）
    }
}
