using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【精益求精】技能牌（机制文档：造物流，占位设计）。
/// 1 灵魂 技能：获得 2 层【驾驭】（本场战斗中造物牌数值 +层数）。
/// 升级后：+3 层。
/// </summary>
public class Refinement() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseHarness = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<HarnessPower>(BaseHarness)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HarnessPower>(choiceContext, Owner.Creature,
            DynamicVars["HarnessPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HarnessPower"].UpgradeValueBy(1m);
    }
}
