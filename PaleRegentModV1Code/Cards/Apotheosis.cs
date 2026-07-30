using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【化神】uncommon能力牌（虚空流的核心终端）。
/// 0 灵魂 + 4 虚空：获得【化神】buff——
/// 每回合开始获得 1 点虚空（升级后 2 点），并选择一张手牌附加【失心】。
///
/// 机制要点：
/// - 虚空费在构造器里用 CardTraits.SetVoidCost 声明，
///   打出时由 RitsuLib 的副资源支付系统自动扣除 4点虚空（不够打不出）。
/// - 规则"虚空费>0 的卡自动带【消耗】"——能力牌打出后本身就移出战斗，
///   但仍在 CanonicalKeywords 声明 Exhaust 以保持卡面提示一致。
///
/// 修改指南：
/// - 改虚空费：VoidCost 常量。
/// - buff 的每回合效果在 Powers/ApotheosisPower.cs 里改。
/// </summary>
public class Apotheosis : PaleRegentModV1Card
{
    /// <summary>打出所需虚空费。</summary>
    private const int VoidCost = 4;

    public Apotheosis() : base(0,
        CardType.Power, CardRarity.Uncommon,
        TargetType.Self)
    {
        // 声明虚空费：打出时额外消耗 7 点虚空
        // （VoidResource.Register 在 MainFile.Initialize 里先于卡牌构造执行，安全）
        CardTraits.SetVoidCost(this, VoidCost);
    }

    // 虚空费>0 自动带消耗（能力牌本身不进弃牌堆，此处主要用于卡面关键词展示）
    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ApotheosisPower>((int?)null),
         ModHoverTips.Lost,
         HoverTipFactory.FromPower<VoidPower>((int?)null) ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ApotheosisPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施加【化神】buff（层数 = 每回合获得的虚空数；基础 1，升级 2）
        await PowerCmd.Apply<ApotheosisPower>(choiceContext, cardPlay.Player.Creature,
            DynamicVars["ApotheosisPower"].BaseValue, cardPlay.Player.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：每回合开始获得的虚空 1 → 2
        DynamicVars["ApotheosisPower"].UpgradeValueBy(1m);
    }
}
