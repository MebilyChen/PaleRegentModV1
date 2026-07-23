using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【终结技】罕见攻击牌（高费大招）。
/// 6 灵魂：对所有敌人造成 40 点伤害。
///
/// 定位：巨额灵魂投入的清场大招；
/// 与【失心】联动（失心后 0 灵魂 6 虚空 + 重放1，打两次 80 伤）是设计文档里的核心 combo。
///
/// 修改指南：
/// - 伤害：BaseDamage / UpgradeDamageBonus 常量。
/// </summary>
public class FinishingMove() : PaleRegentModV1Card(6,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AllEnemies)
{
    /// <summary>基础伤害。</summary>
    private const int BaseDamage = 40;
    /// <summary>升级后伤害增加量。</summary>
    private const int UpgradeDamageBonus = 10;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对所有敌人造成伤害（TargetingAllOpponents = AoE，参考原版 Whirlwind）
        // CombatState 在打出卡牌时必非空，用 ! 消除 CS8604
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_giant_horizontal_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
