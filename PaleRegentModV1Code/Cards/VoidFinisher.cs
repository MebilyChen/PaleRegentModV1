using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空必杀】稀有攻击牌（虚空流的爆发终端）。
/// 0 灵魂 + X 虚空：消耗你全部虚空，造成 X 段、每段 4 点伤害；
/// 若 X 大于 5，则段数翻倍（X×2 段）。
///
/// 机制要点：
/// - 虚空 X 费在构造器用 CardTraits.SetVoidCostX 声明，
///   打出时 RitsuLib 自动把玩家当前全部虚空作为 X 支付；
///   实际支付了多少从 cardPlay.TryGetSecondaryResources 的账本（ledger）读取。
/// - 支付完虚空后调用 SyncPower 让 VoidPower 图标同步清零。
///
/// 修改指南：
/// - 每段伤害：DamagePerHit 常量。
/// - 翻倍阈值：DoubleThreshold 常量（X > 该值时段数翻倍）。
/// </summary>
public class VoidFinisher : PaleRegentModV1Card
{
    /// <summary>每一段的基础伤害（用户可调）。</summary>
    private const int DamagePerHit = 4;
    /// <summary>段数翻倍阈值：支付的虚空 X 大于此值时，攻击段数变为 X×2。</summary>
    private const int DoubleThreshold = 5;

    public VoidFinisher() : base(0,
        CardType.Attack, CardRarity.Rare,
        TargetType.AnyEnemy)
    {
        // 声明虚空 X 费：打出时消耗全部虚空作为 X（乘数 1）
        CardTraits.SetVoidCostX(this, 1);
    }

    // 虚空费>0（X 费）自动带消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    // 卡面 !D! 显示每段伤害数值（含力量修正）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(DamagePerHit, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 1. 从副资源支付账本读取本次实际支付的虚空数 = X
        int x = 0;
        if (cardPlay.TryGetSecondaryResources(out SecondaryResourcePlayLedger ledger))
        {
            x = ledger.Spent(VoidResource.Id);
        }

        // 2. 虚空已被支付系统扣除，同步 VoidPower 图标（通常会清零移除）
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);

        if (x <= 0)
        {
            return; // 0 虚空打出（理论上不会发生），不造成伤害
        }

        // 3. X > 阈值时段数翻倍
        int hitCount = x > DoubleThreshold ? x * 2 : x;

        // 4. 多段伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级：每段伤害 +2（4→6），可按需调整
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}
