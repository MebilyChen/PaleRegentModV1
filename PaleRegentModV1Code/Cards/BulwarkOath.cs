using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【誓卫】能力牌（机制文档：瘟疫流附属防御向）。
/// 3 灵魂 能力：每回合你第一次失去生命时，获得 10 点格挡
/// （Power 按层数给格挡，卡牌施加 10 层）。
/// 升级后：13 点格挡（施加 13 层）。
/// </summary>
public class BulwarkOath() : PaleRegentModV1Card(3,
    CardType.Power, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseStacks = 10;
    private const int UpgradedStacks = 13;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BulwarkOathPower>(BaseStacks)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BulwarkOathPower>(choiceContext, Owner.Creature,
            DynamicVars["BulwarkOathPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BulwarkOathPower"].UpgradeValueBy(UpgradedStacks - BaseStacks);
    }
}
