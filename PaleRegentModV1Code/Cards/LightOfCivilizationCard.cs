using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【文明之光】能力牌（表 C#87，0727 新增）。
/// 4 灵魂：获得能力【文明之光】——灵魂能量大于 0 且手牌为空时，抽 3 张牌。
/// 升级后：抽 5 张牌。
/// </summary>
public class LightOfCivilizationCard() : PaleRegentModV1Card(4,
    CardType.Power, CardRarity.Rare,
    TargetType.Self)
{
    private const int BaseAmount = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<LightOfCivilizationPower>(BaseAmount)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<LightOfCivilizationPower>(choiceContext, Owner.Creature,
            DynamicVars["LightOfCivilizationPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["LightOfCivilizationPower"].UpgradeValueBy(2);
    }
}
