using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂格挡】普通技能牌（带过牌的防御）。
/// 1 灵魂：获得 7 点格挡，抽 1 张牌。
///
/// 修改指南：
/// - 格挡：BaseBlock / UpgradeBlockBonus 常量。
/// - 抽牌数：DrawCount 常量。
/// </summary>
public class SoulBlock() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    /// <summary>基础格挡。</summary>
    private const int BaseBlock = 7;
    /// <summary>升级后格挡增加量。</summary>
    private const int UpgradeBlockBonus = 3;
    /// <summary>抽牌数。</summary>
    private const int DrawCount = 1;

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        // 2. 抽 1 张牌
        await CardPileCmd.Draw(choiceContext, DrawCount, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
