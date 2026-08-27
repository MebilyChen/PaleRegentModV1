using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【诱变增生】能力牌（表 C#56，0727 新增）。
/// 3 灵魂：每当你生成一张状态牌，获得 1 点力量。
/// 升级后：2 灵魂。
/// </summary>
public class MutagenicGrowthCard() : PaleRegentModV1Card(3,
    CardType.Power, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int StrengthPerStatus = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<MutagenicGrowthPower>(StrengthPerStatus)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>((int?)null)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MutagenicGrowthPower>(choiceContext, Owner.Creature,
            DynamicVars["MutagenicGrowthPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
