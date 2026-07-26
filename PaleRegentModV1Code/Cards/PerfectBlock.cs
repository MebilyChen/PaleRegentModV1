using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【完美格挡】技能牌（机制文档：卡牌表 C#11，20260725 批次改版）。
/// 4 灵魂：获得 10 点格挡；获得 3 点【覆甲】。
/// 升级后：获得 15 点格挡；获得 5 点覆甲。
///
/// 20260725 批次修改（表格 S15 备注"复用游戏里【覆甲】能力"）：
/// - 删除自定义的 EchoWardPower（回响守护），改用原版覆甲 PlatingPower：
///   每回合结束获得等同层数的格挡，受到未被格挡的攻击伤害时层数 -1。
/// - 备注：若编译报错找不到 PlatingPower，说明原版类名/命名空间不同，
///   请在 Rider 里用 "Plating" 全局搜索原版程序集确认后回传我修正。
///
/// 修改指南：
/// - 即时格挡：BaseBlock / UpgradeBlockBonus 常量。
/// - 覆甲层数：BasePlating 常量与 OnUpgrade 里的升级值。
/// </summary>
public class PerfectBlock() : PaleRegentModV1Card(4,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    /// <summary>打出时立即获得的格挡。</summary>
    private const int BaseBlock = 10;
    /// <summary>升级后即时格挡增加量（10→15）。</summary>
    private const int UpgradeBlockBonus = 5;
    /// <summary>覆甲层数（基础 3，升级 5）。</summary>
    private const int BasePlating = 3;
    /// <summary>升级后覆甲增加量（3→5）。</summary>
    private const int UpgradePlatingBonus = 2;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PlatingPower>((int?)null)];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move), new PowerVar<PlatingPower>(BasePlating)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 立即获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 获得原版【覆甲】（每回合结束按层数获得格挡）
        await PowerCmd.Apply<PlatingPower>(choiceContext, cardPlay.Player.Creature,
            DynamicVars["PlatingPower"].BaseValue, cardPlay.Player.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
        DynamicVars["PlatingPower"].UpgradeValueBy(UpgradePlatingBonus);
    }
}
