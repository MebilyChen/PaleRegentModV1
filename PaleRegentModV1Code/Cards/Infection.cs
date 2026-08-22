using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【感染】状态牌（机制文档：瘟疫流核心资源牌）。
/// 0 灵魂 + 1 虚空：手动打出后消耗（清除病灶）。保留。
/// 若回合结束时仍留在手牌：
/// 1. 将所有【疑虑】加入手牌；若没有则生成一张（君王之剑式，不会满手诅咒）。
/// 2. 随机将一张手牌变为【感染】；纯粹牌和感染牌都不能被选中。
/// 3. 统计抽牌堆和弃牌堆中的【疑虑】数量，并施加等量虚弱；没有疑虑时按生成的 1 张结算。
/// 手动打出后，获得 1 层【瘟疫】。
/// </summary>
public class Infection : PaleRegentModV1Card
{
    private const int VoidCost = 1;
    private const int BasePlague = 1;

    public Infection() : base(0,
        CardType.Status, CardRarity.Status,
        TargetType.None)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.InfectionRule, HoverTipFactory.FromPower<PlaguePower>((int?)null),
         HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Doubt>(false)];

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PlaguePower>(BasePlague)];

    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // 先记录抽牌堆与弃牌堆中的疑虑数量。后续 Summon 会移动这些卡，
        // 因此必须在召回前计数；若为 0，则按“生成 1 张疑虑”结算 1 层虚弱。
        int doubtCount = CardPile.GetCards(Owner, PileType.Draw, PileType.Discard)
            .Count(card => card is MegaCrit.Sts2.Core.Models.Cards.Doubt);
        int weakAmount = Math.Max(1, doubtCount);

        // 只从“其他”手牌中选目标；任何感染牌和纯粹牌都不能被转化。
        List<CardModel> candidates = CardPile.GetCards(Owner, PileType.Hand)
            .Where(card => card != this && card is not Infection && !CardTraits.IsPure(card))
            .ToList();

        // 如果手牌里没有可感染目标，先在手牌中召回/生成疑虑，再从更新后的手牌中选取。
        // CurseTraitHelper.Summon 和 CardCmd.TransformTo 都不是出牌流程，不会触发本类的 OnPlay。
        bool summonedFallbackDoubt = candidates.Count == 0;
        if (summonedFallbackDoubt)
        {
            await CurseTraitHelper.Summon<MegaCrit.Sts2.Core.Models.Cards.Doubt>(Owner);
            candidates = CardPile.GetCards(Owner, PileType.Hand)
                .Where(card => card != this && card is not Infection && !CardTraits.IsPure(card))
                .ToList();
        }

        // 只转化目标卡；this 本身永远不是候选目标。
        if (candidates.Count > 0)
        {
            CardModel target = Owner.RunState.Rng.CombatTargets.NextItem(candidates);
            CardPileAddResult? transformResult = await CardCmd.TransformTo<Infection>(target);
            if (transformResult.HasValue)
            {
                // 明确给予新感染本回合保留，确保它能穿过本次回合结束的手牌清理。
                transformResult.Value.cardAdded.GiveSingleTurnRetain();
                await NotifyGenerated(Owner.Creature, 1);
            }
        }

        // 原本存在其他可感染手牌时，按既有规则在转化后召回全部疑虑。
        if (!summonedFallbackDoubt)
        {
            await CurseTraitHelper.Summon<MegaCrit.Sts2.Core.Models.Cards.Doubt>(Owner);
        }

        // 按召回前“抽牌堆 + 弃牌堆”的疑虑总数直接施加等量虚弱。
        // 逻辑与疑虑本身的回合结束效果一致：首次获得虚弱时跳过本回合的衰减。
        bool alreadyHasWeak = Owner.Creature.HasPower<WeakPower>();
        PowerModel? weakPower = await PowerCmd.Apply<WeakPower>(
            choiceContext,
            Owner.Creature,
            weakAmount,
            Owner.Creature,
            this);
        if (weakPower != null && !alreadyHasWeak)
        {
            weakPower.SkipNextDurationTick = true;
        }

        // HasTurnEndInHandEffect 的标准结算会在本方法返回后将当前 this 放入弃牌堆。
        // 在此之前生成一个独立的感染副本并给予“仅本回合保留”：
        // 它不会进入本轮已建立的回合末结算队列，因此会留在手牌；
        // 下一回合单回合保留标记清除后，它会作为普通感染再次参与增生。
        CardModel retainedReplacement = Owner.Creature.CombatState.CreateCard<Infection>(Owner);
        retainedReplacement.GiveSingleTurnRetain();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(retainedReplacement, PileType.Hand, Owner),
            1.2f,
            CardPreviewStyle.HorizontalLayout);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 这里仅在玩家手动打出感染时触发；回合末逻辑不会调用此方法。
        // 打出即消耗（虚空费 > 0 自动带消耗），并给自己加 1 层瘟疫。
        await PowerCmd.Apply<PlaguePower>(
            choiceContext,
            cardPlay.Player.Creature,
            DynamicVars["PlaguePower"].BaseValue,
            cardPlay.Player.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
    }

    /// <summary>
    /// 感染生成统一通知入口：所有“生成感染”的代码生成后调用一次，
    /// 触发持有者身上的【疫蔓】。
    /// </summary>
    public static async Task NotifyGenerated(MegaCrit.Sts2.Core.Entities.Creatures.Creature owner, int count)
    {
        Patches.CombatCounters.NotifyInfectionGenerated(count);

        PlagueSpreadPower? spread = owner.GetPower<PlagueSpreadPower>();
        if (spread == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            await spread.OnInfectionGenerated();
        }
    }

    // 不进入奖励池。
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<TokenCardPool>();
    public override bool CanBeGeneratedInCombat => false;
}
