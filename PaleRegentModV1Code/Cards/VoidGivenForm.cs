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
/// 【虚空化形】生成牌（机制文档：造物流，"虚空实验"X≥3 / "驯化"5+ 生成）。
/// 0 灵魂 攻击（全体）：对所有敌人造成 10 点伤害并施加 3 层【虚空之触】。
/// 纯粹。消耗。
/// </summary>
public class VoidGivenForm() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Token,
    TargetType.AllEnemies)
{
    private const int BaseDamage = 10;
    private const int UpgradeDamageBonus = 4;
    private const int TouchAmount = 3;

    public override bool IsCreationCard => true;
    public override bool IsPure => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_giant_horizontal_slash")
            .Execute(choiceContext);

        await PowerCmd.Apply<VoidTouchPower>(choiceContext, CombatState!.HittableEnemies, TouchAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
