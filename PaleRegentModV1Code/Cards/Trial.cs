using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【回避】罕见技能牌（有代价的高性价比格挡）。
/// 1 灵魂：获得 10 点格挡，将一张状态牌【虚空】置入你的弃牌堆。
/// 升级后：获得 17 点格挡。2张虚空
///
/// 定位：低费高格挡，代价是牌库被塞垃圾；
/// 配合【再利用】（把"虚空"状态牌变成【集中】）可以化解负面。
/// 注：类名仍为 Trial（避免本地化 key 变动），卡牌标题改为"回避"。
///
/// 修改指南：
/// - 格挡：BaseBlock / UpgradeBlockBonus 常量。
/// - 塞的状态牌数量：StatusCount 常量。
/// </summary>
public class Trial() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    /// <summary>基础格挡。</summary>

    private const int BaseBlock = 10;

    /// <summary>升级后格挡增加量。</summary>

    private const int UpgradeBlockBonus = 7;
    /// <summary>未升级时置入的【虚空】数量。</summary>
    private const int BaseStatusCount = 1;

    /// <summary>升级后置入的【虚空】数量。</summary>
    private const int UpgradeStatusCount = 2;

    private int VoidStatusCount =>
        IsUpgraded ? UpgradeStatusCount : BaseStatusCount;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<TheVoidStatus>(false)];

    // 声明"这张牌提供格挡"，游戏会据此显示格挡预览等 UI
    public override bool GainsBlock => true;

    // 带 Defend 标签：与"对防御牌生效"的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得格挡：未升级 10，升级后 17。
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 未升级置入 1 张，升级后置入 2 张【虚空】到弃牌堆。
        await CardPileCmd.AddToCombatAndPreview<TheVoidStatus>(
            Owner.Creature,
            PileType.Discard,
            VoidStatusCount,
            Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
