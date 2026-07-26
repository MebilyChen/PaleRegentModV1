using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【回退】罕见技能牌（虚空的"退出机制"）。
/// 0 灵魂 + X 虚空：将 X 点虚空转化为 X 点灵魂。【消耗】。
/// 升级后：转化为 X+1 点灵魂。
///
/// 定位：攒了一堆虚空但不想继续欠债时的止损/爆发牌——
/// 把虚空债一次性变现为当回合灵魂。与【染色】互为反向操作。
///
/// 机制要点：
/// - 虚空 X 费用 CardTraits.SetVoidCostX 声明（与虚空必杀同款），
///   打出时自动消耗全部虚空作为 X，实际支付量从 ledger 读取。
///
/// 修改指南：
/// - 升级加成：_energyBonus 字段（X+1）。
/// </summary>
public class Rollback : PaleRegentModV1Card
{
    /// <summary>额外灵魂获得量（升级后 +1）。</summary>
    private int _energyBonus;

    public Rollback() : base(0,
        CardType.Skill, CardRarity.Uncommon,
        TargetType.Self)
    {
        // 声明虚空 X 费：打出时消耗全部虚空作为 X（乘数 1）
        CardTraits.SetVoidCostX(this, 1);
    }

    // 打出后消耗（防止一场战斗里反复无限转换）
    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.VoidCounter];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 从副资源支付账本读取本次实际支付的虚空数 = X
        int x = 0;
        if (cardPlay.TryGetSecondaryResources(out SecondaryResourcePlayLedger ledger))
        {
            x = ledger.Spent(VoidResource.Id);
        }

        // 2. 虚空已被支付系统扣除，同步 VoidPower 图标（通常会清零移除）
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);

        int gain = x + _energyBonus;
        if (gain <= 0)
        {
            return; // 没有虚空则什么都不发生
        }

        // 3. 1:1 转换为灵魂（升级后 X+1）
        await PlayerCmd.GainEnergy(gain, cardPlay.Player);
    }

    protected override void OnUpgrade()
    {
        // 升级：转化为 X+1 点灵魂
        _energyBonus = 1;
    }
}
