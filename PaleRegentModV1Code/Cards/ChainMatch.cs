using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【连连看】技能牌。
/// 本回合内，你每连续打出 1 张同名牌，对所有敌人造成
/// {ChainResonancePower:diff()} 点伤害，并获得
/// {ChainResonancePower:diff()} 点格挡。
/// 本场战斗内，该效果每触发一次，伤害与格挡永久提高。
/// </summary>
public class ChainMatch() : PaleRegentModV1Card(
    1,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseDamage = 3;
    private const int DamagePerTrigger = 1;
    private const int UpgradeDamageBonus = 2;

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ChainResonancePower>(BaseDamage),
        new DynamicVar("ChainResonancePowerAdd", DamagePerTrigger)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        decimal initialDamage =
            DynamicVars["ChainResonancePower"].BaseValue;

        decimal damageAdd =
            DynamicVars["ChainResonancePowerAdd"].BaseValue;

        ChainResonancePower? power =
            Owner.Creature.GetPower<ChainResonancePower>();

        // Power 不存在时创建。
        if (power is null)
        {
            await PowerCmd.Apply<ChainResonancePower>(
                choiceContext,
                Owner.Creature,
                initialDamage,
                Owner.Creature,
                this);

            power = Owner.Creature.GetPower<ChainResonancePower>();
        }
        else
        {
            // Power 已存在时，在现有 Amount 上增加本回合临时伤害与格挡数值。
            // 这部分会在回合结束时自动扣除。
            await power.AddTemporaryDamageForTurn(
                choiceContext,
                initialDamage,
                this);
        }

        // 启用本回合监听；不会重置 Amount，也不会清除临时数值。
        power?.ActivateForTurn(initialDamage, damageAdd);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ChainResonancePower"]
            .UpgradeValueBy(UpgradeDamageBonus);
    }
}
