using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【容器计划】能力牌（机制文档：造物流）。
/// 2 灵魂 能力：每回合开始时，将 1 张【容器】加入手牌，
/// 并将 1 张【羞愧】（原版诅咒）加入抽牌堆（伟大计划的代价）。
/// </summary>
public class VesselPlan() : PaleRegentModV1Card(2,
    CardType.Power, CardRarity.Rare,
    TargetType.Self)
{
    private const int VesselPerTurn = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VesselPlanPower>(VesselPerTurn)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VesselPlanPower>(choiceContext, Owner.Creature,
            DynamicVars["VesselPlanPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["VesselPlanPower"].UpgradeValueBy(1m);
    }
}
