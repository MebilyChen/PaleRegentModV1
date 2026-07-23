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
/// 【失败实验】生成牌（机制文档：造物流，"虚空实验"X&lt;3 时生成）。
/// 0 灵魂 攻击（全体）：对所有敌人造成 9 点伤害并施加 1 层【虚空之触】，
/// 同时对自己施加 1 层【虚空之触】（失控的实验殃及自身）。消耗。
/// </summary>
public class FailedExperiment() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Special,
    TargetType.AllEnemies)
{
    private const int BaseDamage = 9;
    private const int UpgradeDamageBonus = 3;
    private const int TouchAmount = 1;

    public override bool IsCreationCard => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        // 对所有存活敌人施加虚空之触
        await PowerCmd.Apply<VoidTouchPower>(choiceContext, CombatState!.HittableEnemies, TouchAmount, Owner.Creature, this);
        // 失控代价：自己也吃 1 层
        await PowerCmd.Apply<VoidTouchPower>(choiceContext, Owner.Creature, TouchAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
