using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【连锁反应】能力牌（表 C#71，0727 新增）。
/// 3 灵魂：获得 8 层【连锁共鸣】。
/// 升级后：10 层。
/// 备注：与 C#62【连锁引信】共用 ChainResonancePower。
/// </summary>
public class ChainReactionCard() : PaleRegentModV1Card(3,
    CardType.Power, CardRarity.Rare,
    TargetType.Self)
{
    private const int BaseStacks = 8;
    private const int UpgradeStacksBonus = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ChainResonancePower>(BaseStacks)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ChainResonancePower>(choiceContext, Owner.Creature,
            DynamicVars["ChainResonancePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ChainResonancePower"].UpgradeValueBy(UpgradeStacksBonus);
    }
}
