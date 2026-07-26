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
/// 升级后：获得 13 点格挡。
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
    private const int UpgradeBlockBonus = 3;
    /// <summary>置入弃牌堆的【虚空】状态牌数量。</summary>
    private const int StatusCount = 1;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<TheVoidStatus>(false)];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 生成状态牌【虚空】进弃牌堆（AddToCombatAndPreview 会播放"卡牌加入"预览动画）
        await CardPileCmd.AddToCombatAndPreview<TheVoidStatus>(Owner.Creature, PileType.Discard, StatusCount, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
