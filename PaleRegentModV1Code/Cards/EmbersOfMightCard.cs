using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【力量余烬】能力牌（表 C#102，0727 新增）。
/// 2 灵魂：获得能力【力量余烬】——每当有 1 张牌被消耗时，随机一名玩家获得 1 点力量。
/// 升级后：2 点力量。
/// </summary>
public class EmbersOfMightCard() : PaleRegentModV1Card(2,
    CardType.Power, CardRarity.Rare,
    TargetType.Self)
{
    private const int BaseAmount = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<EmbersOfMightPower>(BaseAmount)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EmbersOfMightPower>(choiceContext, Owner.Creature,
            DynamicVars["EmbersOfMightPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EmbersOfMightPower"].UpgradeValueBy(1);
    }
}
