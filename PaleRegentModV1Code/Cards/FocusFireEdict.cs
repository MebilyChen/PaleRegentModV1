using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【集火号令】攻击牌（机制文档：瘟疫流"嘲讽/集中目标"）。
/// 1 灵魂 攻击：造成 1 点伤害，对目标施加 1 层【瘟疫】，
/// 本回合【瘟疫】的随机攻击全部集中到该敌人（旧日仇敌）。
/// 升级后：施加 2 层瘟疫。
/// </summary>
public class FocusFireEdict() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 1;
    private const int BasePlague = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move), new PowerVar<PlaguePower>(BasePlague)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 本回合瘟疫集中目标（回合结束由 PlaguePower 复位）
        PlaguePower.FocusTarget = cardPlay.Target;
        await PowerCmd.Apply<PlaguePower>(choiceContext, cardPlay.Target,
            DynamicVars["PlaguePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PlaguePower"].UpgradeValueBy(1m);
    }
}
