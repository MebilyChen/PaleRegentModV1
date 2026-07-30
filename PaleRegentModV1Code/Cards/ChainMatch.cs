using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【连连看】
///
/// 本回合内，每连续打出 1 张同名牌，
/// 对所有敌人造成 {ChainResonancePower} 点伤害。
///
/// 本场战斗内，该效果每触发一次，
/// 伤害 +{ChainResonancePowerAdd}。
/// </summary>
public class ChainMatch() : PaleRegentModV1Card(
    1,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    private const string DamageKey = "ChainResonancePower";
    private const string DamageAddKey = "ChainResonancePowerAdd";

    private const int BaseDamage = 3;
    private const int BaseDamageAdd = 1;

    private const int UpgradeDamageBonus = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ChainResonancePower>(BaseDamage),
        new DynamicVar(DamageAddKey, BaseDamageAdd)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        decimal baseDamage = DynamicVars[DamageKey].BaseValue;
        decimal damageAdd = DynamicVars[DamageAddKey].BaseValue;

        ChainResonancePower? power =
            Owner.Creature.GetPower<ChainResonancePower>();

        if (power == null)
        {
            // 第一次使用：创建 Power，并把 Amount 初始化为基础伤害。
            await PowerCmd.Apply<ChainResonancePower>(
                choiceContext,
                Owner.Creature,
                baseDamage,
                Owner.Creature,
                this);

            power = Owner.Creature.GetPower<ChainResonancePower>();
        }
        else if (power.Amount < baseDamage)
        {
            // 已经积累的伤害不能被重置。
            // 但如果使用了初始伤害更高的版本，则提高到该最低值。
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                baseDamage - power.Amount,
                Owner.Creature,
                this);
        }

        // 仅启用本回合，不重置当前累计伤害。
        power?.ActivateForTurn(baseDamage, damageAdd);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DamageKey]
            .UpgradeValueBy(UpgradeDamageBonus);
    }
}
