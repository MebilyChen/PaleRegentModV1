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
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【感染】状态牌（机制文档：瘟疫流核心资源牌）。
/// 0 灵魂 + 1 虚空：手动打出后消耗（清除病灶）。保留。
/// 若回合结束时仍留在手牌：随机将一张其他手牌变为【感染】，
/// 并将你所有的【疑虑】加入手牌（若没有则生成一张）——君王之剑式，不会满手诅咒。
/// 感染状态牌手动打出后给自己 + 1 层瘟疫。
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

    /// <summary>
    /// 回合结束仍在手牌时，仅在手牌内执行生成/转化，绝不调用 OnPlay，
    /// 不创建 CardPlay，也不移动 this。只有玩家手动打出感染时才会进入 OnPlay。
    /// </summary>
    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // 只从“其他”手牌中选目标；this 永远不会被转化或作为打出目标。
        List<CardModel> candidates = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c != this && c is not Infection && !CardTraits.IsPure(c))
            .ToList();

        // 如果手牌里只有感染，先在手牌中召回/生成疑虑，再把这张疑虑转化为感染。
        // CurseTraitHelper.Summon 和 CardCmd.TransformTo 都不是出牌流程，
        // 因而不会触发本类的 OnPlay。
        bool summonedFallbackDoubt = candidates.Count == 0;
        if (summonedFallbackDoubt)
        {
            await CurseTraitHelper.Summon<MegaCrit.Sts2.Core.Models.Cards.Doubt>(Owner);
            candidates = CardPile.GetCards(Owner, PileType.Hand)
                .Where(c => c != this && c is not Infection && !CardTraits.IsPure(c))
                .ToList();
        }

        // 只转化目标卡；this 保持在手牌，随后由 Retain 正常保留。
        if (candidates.Count > 0)
        {
            CardModel target = Owner.RunState.Rng.CombatTargets.NextItem(candidates);
            await CardCmd.TransformTo<Infection>(target);
            await NotifyGenerated(Owner.Creature, 1);
        }

        // 原本存在其他可感染手牌时，保留原有的“转化后再召回疑虑”规则。
        if (!summonedFallbackDoubt)
        {
            await CurseTraitHelper.Summon<MegaCrit.Sts2.Core.Models.Cards.Doubt>(Owner);
        }
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
}
