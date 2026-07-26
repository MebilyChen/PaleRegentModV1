using MegaCrit.Sts2.Core.HoverTips;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【精益求精】技能牌（机制文档：造物流）。
/// 1 灵魂 技能：抽 1 张牌。获得 3 层【驾驭】（造物牌数值 +层数）。
/// 升级后：驾驭 5 层。
/// </summary>
public class Refinement() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    private const int BaseHarness = 3;
    private const int DrawCount = 1;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Harness];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<HarnessPower>(BaseHarness)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DrawCount, cardPlay.Player);
        await PowerCmd.Apply<HarnessPower>(choiceContext, Owner.Creature,
            DynamicVars["HarnessPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HarnessPower"].UpgradeValueBy(2m);
    }
}
