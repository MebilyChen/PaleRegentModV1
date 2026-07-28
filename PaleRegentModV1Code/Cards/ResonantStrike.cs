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
/// 【共鸣一击】攻击牌（表 C#63，0727 新增）。
/// 1 灵魂：造成 5 点伤害，本回合每生成 1 点虚空或灵魂，额外 +3 点伤害。
/// 升级后：8 点伤害，每点 +4。
/// 备注：本回合"生成的灵魂"统计的是回合内额外获得的灵魂（蓄灵发放、卡牌获能等，
/// 通过 CombatCounters 埋点），回合开始的常规灵魂恢复不计入。
/// </summary>
public class ResonantStrike() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;

    /// <summary>本回合每点虚空/灵魂的额外伤害（升级后 4）。</summary>
    private int _bonusPerResource = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int resourceGained = VoidPowerListener.VoidGainedThisTurn + CombatCounters.SoulGainedThisTurn;
        decimal totalDamage = DynamicVars.Damage.BaseValue + resourceGained * _bonusPerResource;

        await DamageCmd.Attack(totalDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        _bonusPerResource = 4;
    }
}
