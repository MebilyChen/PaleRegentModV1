using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【锐意进击】攻击牌（表 C#65，0727 新增）。
/// 1 灵魂：造成 7 点伤害，抽 1 张牌。
/// 升级后：10 点伤害，抽 2 张牌。
/// </summary>
public class PressTheAttack() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Common,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 7;
    private const int UpgradeDamageBonus = 3;
    private const int BaseDraw = 1;
    private const int UpgradeDrawBonus = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move),
         new CardsVar(BaseDraw)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, cardPlay.Player);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        DynamicVars.Cards.UpgradeValueBy(UpgradeDrawBonus);
    }
}
