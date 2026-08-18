using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Patches;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【共鸣一击】攻击牌。
/// 1 灵魂：造成伤害；本回合每生成 1 点虚空或额外获得 1 点灵魂，伤害增加。
/// </summary>
public class ResonantStrike() : PaleRegentModV1Card(
    1,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;
    private const int BonusPerResource = 3;
    private const int UpgradeBonusPerResourceBonus = 1;

    /// <summary>
    /// CalculatedDamage = CalculationBase + ExtraDamage × multiplier。
    /// multiplier 每次显示牌面或结算时读取当前回合资源计数。
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(BaseDamage),
        new ExtraDamageVar(BonusPerResource),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (_, __) => GetResourceGainedThisTurn())
    ];

    private static int GetResourceGainedThisTurn()
    {
        return VoidPowerListener.VoidGainedThisTurn
            + CombatCounters.SoulGainedThisTurn;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 与牌面展示共用同一个动态变量，避免显示值和结算值不一致。
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(UpgradeDamageBonus);            // 5 -> 8
        DynamicVars.ExtraDamage.UpgradeValueBy(UpgradeBonusPerResourceBonus);     // 3 -> 4
    }
}