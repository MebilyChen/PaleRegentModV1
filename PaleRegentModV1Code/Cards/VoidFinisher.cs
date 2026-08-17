using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空必杀】稀有攻击牌（虚空流的爆发终端）。
/// 0 灵魂 + X 虚空：消耗你全部虚空，造成 X 段、每段 4 点伤害；
/// 若 X 不小于 4，则段数翻倍（X×2 段）。为弃牌堆至多{IfUpgraded:show:X+1|X}张牌添加[gold]失心[/gold]。
/// 升级后：每段 5 点伤害，段数 X+1（翻倍时 (X+1)×2）。
///
/// 机制要点：
/// - 虚空 X 费在构造器用 CardTraits.SetVoidCostX 声明，
///   打出时 RitsuLib 自动把玩家当前全部虚空作为 X 支付；
///   实际支付了多少从 cardPlay.TryGetSecondaryResources 的账本（ledger）读取。
/// - 支付完虚空后调用 SyncPower 让 VoidPower 图标同步清零。
///
/// 修改指南：
/// - 每段伤害：DamagePerHit 常量。
/// - 翻倍阈值：DoubleThreshold 常量（X >= 该值时段数翻倍）。
/// </summary>
public class VoidFinisher : PaleRegentModV1Card
{
    /// <summary>每一段的基础伤害（用户可调）。</summary>
    private const int DamagePerHit = 4;

    /// <summary>段数翻倍阈值：支付的虚空 X 不小于此值时，攻击段数翻倍。</summary>
    private const int DoubleThreshold = 4;

    /// <summary>升级后的额外段数与额外失心选择数（X+1）。</summary>
    private int _bonusHits;

    public VoidFinisher() : base(0,
        CardType.Attack, CardRarity.Rare,
        TargetType.AnyEnemy)
    {
        // 声明虚空 X 费：打出时消耗全部虚空作为 X（乘数 1）。
        CardTraits.SetVoidCostX(this, 1);
    }

    // 虚空费 > 0（X 费）自动带消耗。
    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost, ModHoverTips.VoidCounter];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    // 卡面 !D! 显示每段伤害数值（含力量修正）。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(DamagePerHit, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 1. 从副资源支付账本读取本次实际支付的虚空数 = X。
        int x = 0;
        if (cardPlay.TryGetSecondaryResources(out SecondaryResourcePlayLedger ledger))
        {
            x = ledger.Spent(VoidResource.Id);
        }

        // 2. 虚空已被支付系统扣除，同步 VoidPower 图标（通常会清零移除）。
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
        if (x <= 0)
        {
            return; // 0 虚空打出（理论上不会发生），不造成伤害或附加失心。
        }

        // 3. 基础段数 = X + 升级加成；X >= 阈值时段数翻倍。
        int baseHits = x + _bonusHits;
        int hitCount = x >= DoubleThreshold ? baseHits * 2 : baseHits;

        // 4. 多段伤害。
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 5. 从弃牌堆至多选择 X 张（升级后至多 X+1 张）可附加【失心】的牌。
        int maxLostCount = x + _bonusHits;
        List<CardModel> eligibleCards = PileType.Discard.GetPile(cardPlay.Player).Cards
            .Where(c => CardTraits.CanApplyLost(c))
            .ToList();

        // 避免弃牌堆没有合格牌时打开空的选择界面。
        if (eligibleCards.Count == 0)
        {
            return;
        }

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            eligibleCards,
            cardPlay.Player,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, maxLostCount));

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyLost(card);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：每段伤害 4→5，段数与失心选择上限均为 X+1。
        DynamicVars.Damage.UpgradeValueBy(1);
        _bonusHits = 1;
    }
}
