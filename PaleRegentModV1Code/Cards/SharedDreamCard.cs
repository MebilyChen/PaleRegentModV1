using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【同享一梦】能力牌（表 C#98，0727 新增）。
/// 4 灵魂：获得能力【同享一梦】——每回合开始，你和所有盟友获得 1 层【入梦】。
/// 升级后：2 层。
/// </summary>
public class SharedDreamCard() : PaleRegentModV1Card(4,
    CardType.Power, CardRarity.Rare,
    TargetType.Self)
{
    private const int BaseAmount = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<SharedDreamPower>(BaseAmount)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SharedDreamPower>(choiceContext, Owner.Creature,
            DynamicVars["SharedDreamPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["SharedDreamPower"].UpgradeValueBy(1);
    }
}
