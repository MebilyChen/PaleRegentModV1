using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【誓卫】能力牌（机制文档：占位命名）。
/// 1 灵魂 能力：每回合你第一次失去生命时，获得 10 点格挡（层数×10）。
/// 升级后：层数 +1（即 20 点格挡）。
/// </summary>
public class BulwarkOath() : PaleRegentModV1Card(1,
    CardType.Power, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseStacks = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BulwarkOathPower>(BaseStacks)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BulwarkOathPower>(choiceContext, Owner.Creature,
            DynamicVars["BulwarkOathPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BulwarkOathPower"].UpgradeValueBy(1m);
    }
}
