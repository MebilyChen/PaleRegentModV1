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
/// 【病态辐射】攻击牌（表 C#70，0727 新增）。
/// 0 灵魂：对随机敌人造成 3 点伤害，本场战斗每生成过 1 张【感染】，
/// 额外攻击 1 次（即攻击次数 = 1 + 感染生成数）。
/// 升级后：5 点伤害。
/// 备注：感染生成数由 CombatCounters.InfectionGeneratedThisCombat 统计，
/// 所有走 Infection.NotifyGenerated 入口的生成都会计数。
/// </summary>
public class PestilentRadiation() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.RandomEnemy)
{
    private const int BaseDamage = 3;
    private const int UpgradeDamageBonus = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hits = 1 + CombatCounters.InfectionGeneratedThisCombat;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(CombatState!, true)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
