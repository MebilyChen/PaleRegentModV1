using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【纯粹容器】生成牌（表格设计：造物流，容器吸收 3+ 张状态牌孕育的完全体）。
/// 0 灵魂 攻击（单体）：消耗你所有牌堆（手牌/抽牌堆/弃牌堆）中的状态牌，
/// 每消耗 1 张，此牌伤害 +5；造成 20 点基础伤害；
/// 对目标施加 2 层【纯粹封印】；你获得 1 层【虚空护卫】。
/// 纯粹。消耗。Regret（生成时召回 Regret）。
/// 升级后：每张状态牌 +7 伤害。
/// </summary>
public class PureVessel() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Token,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 20;
    private const int BonusPerStatus = 5;
    private const int UpgradedBonusPerStatus = 7;
    private const int SealAmount = 2;
    private const int GuardAmount = 1;

    /// <summary>每消耗一张状态牌的伤害加成（基础 5，升级 7）。</summary>
    private int _bonusPerStatus = BonusPerStatus;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Pure,
         HoverTipFactory.FromPower<PureSealPower>((int?)null),
         HoverTipFactory.FromPower<VoidGuardPower>((int?)null),
         HoverTipFactory.FromCard<Regret>(false)];

    public override bool IsCreationCard => true;
    public override bool IsPure => true;

    /// <summary>
    /// Regret 特质（君王之剑式）：此牌生成时，将你所有的 Regret 加入手牌（若没有则生成一张）。
    /// </summary>
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        await base.AfterCardGeneratedForCombat(card, creator); // 基类统一处理失心诅咒（LostDestiny）
        if (card == this)
        {
            await CurseTraitHelper.Summon<Regret>(Owner);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 消耗所有牌堆（手牌/抽牌堆/弃牌堆）中的状态牌，每张 +5 伤（升级 +7）
        List<CardModel> statuses = CardPile.GetCards(Owner, PileType.Hand)
            .Concat(CardPile.GetCards(Owner, PileType.Draw))
            .Concat(CardPile.GetCards(Owner, PileType.Discard))
            .Where((CardModel c) => c.Type == CardType.Status && c != this)
            .ToList();
        foreach (CardModel status in statuses)
        {
            await CardCmd.Exhaust(choiceContext, status);
        }

        decimal damage = DynamicVars.Damage.BaseValue + statuses.Count * _bonusPerStatus;

        // 2. 造成伤害
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_giant_horizontal_slash")
            .Execute(choiceContext);

        // 3. 对目标施加 2 层纯粹封印
        if (cardPlay.Target != null)
        {
            await PowerCmd.Apply<PureSealPower>(choiceContext, cardPlay.Target, SealAmount, Owner.Creature, this);
        }

        // 4. 自己获得 1 层虚空护卫
        await PowerCmd.Apply<VoidGuardPower>(choiceContext, Owner.Creature, GuardAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        _bonusPerStatus = UpgradedBonusPerStatus;
    }
}
