using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【制造佣卫】能力牌（机制文档：造物流）。
/// 1 灵魂 能力：每回合开始时，将 1 张【国王佣卫】加入手牌。
/// 升级后：改为每回合 2 张（层数+1）。
/// </summary>
public class MakeRetainer() : PaleRegentModV1Card(1,
    CardType.Power, CardRarity.Common,
    TargetType.Self)
{
    private const int ForgePerTurn = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<RetainerForgePower>(ForgePerTurn)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RetainerForgePower>(choiceContext, Owner.Creature,
            DynamicVars["RetainerForgePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RetainerForgePower"].UpgradeValueBy(1m);
    }
}
