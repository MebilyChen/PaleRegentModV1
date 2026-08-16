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
/// 0 灵魂 + 1 虚空：打出后消耗（清除病灶）。保留。
/// 若回合结束时仍留在手牌：随机将一张其他手牌变为【感染】，
/// 并将你所有的【疑虑】加入手牌（若没有则生成一张）——君王之剑式，不会满手诅咒。
/// 感染状态牌打出后给自己 + 1层瘟疫
/// 
/// 联动：
/// - 疫刃按消耗牌堆中的感染数量加伤；
/// - 疫收把手牌感染转化为虚空；
/// - 疫蔓（PlagueSpreadPower）在感染生成时触发——统一入口
///   NotifyGenerated(creature) 由生成方调用。
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
        [ModHoverTips.InfectionRule,HoverTipFactory.FromPower<PlaguePower>((int?)null),
         HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Doubt>(false)];

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PlaguePower>(BasePlague)];

    /// <summary>
    /// 回合结束仍在手牌：病情恶化——
    /// 1) 随机将一张其他手牌（非感染）变为【感染】；
    /// 2) Doubt 特质（君王之剑式）：将你所有的【疑虑】加入手牌；
    ///    若一张都没有才生成一张，避免满手诅咒。
    /// </summary>
    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // 1) 随机一张其他手牌（非感染、非【纯粹】）变为感染（用官方战斗随机数流，保证联机/回放一致）
        // 20260725 批次：带【纯粹】的牌不会被感染（机制文档：名词表·纯粹）
        List<CardModel> candidates = CardPile.GetCards(Owner, PileType.Hand)
            .Where((CardModel c) => c != this && c is not Infection && !CardTraits.IsPure(c))
            .ToList();
        if (candidates.Count > 0)
        {
            CardModel target = Owner.RunState.Rng.CombatTargets.NextItem(candidates);
            await CardCmd.TransformTo<Infection>(target);
            await NotifyGenerated(Owner.Creature, 1);
        }

        // 2) 召回疑虑
        await CurseTraitHelper.Summon<MegaCrit.Sts2.Core.Models.Cards.Doubt>(Owner);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出即消耗（虚空费>0 自动带消耗）
        // 给自己加1层瘟疫
        await PowerCmd.Apply<PlaguePower>(
            choiceContext,
            cardPlay.Player.Creature,
            DynamicVars["PlaguePower"].BaseValue,
            cardPlay.Player.Creature,
            this
        );
        //改回无效果的牌，删除async，注释上面，取消下行注释
        //return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }

    /// <summary>
    /// 感染生成统一通知入口：所有"生成感染"的代码生成后调用一次，
    /// 触发持有者身上的【疫蔓】。
    /// </summary>
    public static async Task NotifyGenerated(MegaCrit.Sts2.Core.Entities.Creatures.Creature owner, int count)
    {
        // 20260727：全局感染生成计数（病态辐射 C#70 等卡牌的统计口径）
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
