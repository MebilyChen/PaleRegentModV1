using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【有翼佣卫】能力牌（机制文档：造物流）。
/// 1 灵魂 能力：每回合开始时，将 1 张【有翼佣卫】（格挡造物牌）加入手牌。
/// 升级后：每回合 2 张。
/// </summary>
public class WingedRetainer() : PaleRegentModV1Card(1,
    CardType.Power, CardRarity.Common,
    TargetType.Self)
{
    private const int ForgePerTurn = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WingedForgePower>(ForgePerTurn)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WingedForgePower>(choiceContext, Owner.Creature,
            DynamicVars["WingedForgePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WingedForgePower"].UpgradeValueBy(1m);
    }
}
