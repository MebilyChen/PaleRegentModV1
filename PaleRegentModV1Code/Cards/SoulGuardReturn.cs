using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【防御】技能牌。
/// 1 灵魂：获得 7 点格挡。升级后格挡 +3（10 点），返回手牌。
///
/// 修改指南：
/// - 改基础格挡：改 BaseBlock 常量。
/// - 改升级增幅：改 UpgradeBlockBonus 常量。
/// </summary>
public class SoulGuardReturn() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    /// <summary>基础格挡。</summary>
    private const int BaseBlock = 5;
    /// <summary>升级后格挡增加量。</summary>
    private const int UpgradeBlockBonus = 3;

    // 声明"这张牌提供格挡"，游戏会据此显示格挡预览等 UI
    public override bool GainsBlock => true;

    // 带 Defend 标签：与"对防御牌生效"的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    // BlockVar 声明格挡动态变量：卡面描述里的 !B! 会显示此数值（含敏捷等修正）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await CardPileCmd.Add(cardPlay.Card, PileType.Hand); //返回手牌
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
