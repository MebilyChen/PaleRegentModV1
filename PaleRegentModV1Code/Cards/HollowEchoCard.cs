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
/// 【空洞回响】能力牌（表 C#75，0727 新增）。
/// 0 灵魂 + 7 虚空：每回合开始时，若你的灵魂为 0，
/// 本回合你打出的第 1 张牌会额外打出 1 张失心复制品。
/// 升级后：5 虚空。
/// </summary>
public class HollowEchoCard : PaleRegentModV1Card
{
    private const int BaseVoidCost = 7;
    private const int UpgradedVoidCost = 5;

    public HollowEchoCard() : base(0,
        CardType.Power, CardRarity.Uncommon,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, BaseVoidCost);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<HollowEchoPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HollowEchoPower>(choiceContext, Owner.Creature,
            DynamicVars["HollowEchoPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        CardTraits.SetVoidCost(this, UpgradedVoidCost);
    }
}
