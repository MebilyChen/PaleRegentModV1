using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【卫群壁垒】能力牌（表 C#88，0727 新增）。
/// 1 灵魂 + 1 虚空：获得能力【卫群壁垒】——每生成 1 张俑卫牌，获得 3 点格挡。
/// 升级后：5 点格挡。
/// </summary>
public class RetainerBulwarkCard : PaleRegentModV1Card
{
    private const int BaseBlock = 3;
    private const int VoidCost = 1;

    public RetainerBulwarkCard() : base(1,
        CardType.Power, CardRarity.Uncommon,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<RetainerBulwarkPower>(BaseBlock)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RetainerBulwarkPower>(choiceContext, Owner.Creature,
            DynamicVars["RetainerBulwarkPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RetainerBulwarkPower"].UpgradeValueBy(2);
    }
}
