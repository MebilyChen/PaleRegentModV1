using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空化模】（表格 C#9，20260725 新增）。
/// 0 灵魂 1 虚空 技能/Uncommon：获得 5(+7) 点格挡；
/// 0 灵魂费卡牌伤害 +5(+7)。消耗。模具。
///
/// 实现说明：
/// - 加伤效果通过 VoidGivenMouldPower 实现（挂在玩家身上，整场有效）。
/// - 【模具】特质：本场战斗中每消耗 1 张同名牌，战斗结束时有 1% 概率
///   获得对应“模具·虚空化模”遗物（见 MouldHelper / 名词表 N#9）。
/// - 虚空费 > 0 的牌打出后自动消耗（CardTraits.SetVoidCost 已同步 Exhaust 关键词）。
/// </summary>
public class VoidGivenMould : PaleRegentModV1Card
{
    /// <summary>虚空费。</summary>
    private const int VoidCost = 1;

    /// <summary>0 灵魂费卡牌伤害加成（基础 5，升级 7）。</summary>
    private const int BaseBonus = 5;
    private const int UpgradedBonus = 7;

    /// <summary>获得格挡数值（基础 5，升级 7）。</summary>
    private const int BaseBlock = 5;
    private const int UpgradedBlock = 7;

    private int _bonus = BaseBonus;

    // 带 Defend 标签：与“对防御牌生效”的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    public override bool GainsBlock => true;

    public VoidGivenMould() : base(
        0,
        CardType.Skill,
        CardRarity.Uncommon,
        TargetType.None)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    /// <summary>模具牌标记（战斗结束按消耗数概率成为遗物，见名词表 N#9）。</summary>
    public override bool IsMould => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new BlockVar(BaseBlock, ValueProp.Move),
            new PowerVar<VoidGivenMouldPower>(BaseBonus)
        ];

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Mould];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(
            cardPlay.Player.Creature,
            DynamicVars.Block,
            cardPlay);

        await PowerCmd.Apply<VoidGivenMouldPower>(
            choiceContext,
            cardPlay.Player.Creature,
            _bonus,
            cardPlay.Player.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        _bonus = UpgradedBonus;
        DynamicVars.Block.UpgradeValueBy(UpgradedBlock - BaseBlock);
        DynamicVars["VoidGivenMouldPower"].UpgradeValueBy(UpgradedBonus - BaseBonus);
    }
}
