using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【深渊诞生】能力牌。
/// 3 灵魂 + 1 虚空：获得 3 层【深渊诞生】。
/// 【深渊诞生】：每消耗 1 点虚空，对随机敌人造成层数点伤害；
/// 每获得 1 点虚空，获得层数点防御。
/// 升级后：2 灵魂 + 1 虚空。
/// </summary>
public class BorninAbyss : PaleRegentModV1Card
{
    private const int VoidCost = 1;
    private const int PowerAmount = 3;

    public BorninAbyss() : base(2,
        CardType.Power, CardRarity.Rare,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BorninAbyssPower>(PowerAmount)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BorninAbyssPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["BorninAbyssPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}