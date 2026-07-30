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
/// 【苦痛烙印】能力牌（表 C#66，0727 新增）。
/// 2 灵魂 + 1 虚空：每当你对敌人造成伤害，对其施加 1 层【苦痛之路】。
/// 升级后：2 层。
/// </summary>
public class BrandOfPainCard : PaleRegentModV1Card
{
    private const int VoidCost = 1;
    private const int BaseStacks = 1;
    private const int UpgradeStacksBonus = 1;

    public BrandOfPainCard() : base(2,
        CardType.Power, CardRarity.Uncommon,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BrandOfPainPower>(BaseStacks)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BrandOfPainPower>(choiceContext, Owner.Creature,
            DynamicVars["BrandOfPainPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BrandOfPainPower"].UpgradeValueBy(UpgradeStacksBonus);
    }
}
