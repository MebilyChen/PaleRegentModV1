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
/// 【打击】初始牌组基础攻击牌。
/// 1 灵魂：造成 5 点伤害。升级后伤害 +3（8 点）。
///
/// 修改指南：
/// - 改基础伤害：改 BaseDamage 常量。
/// - 改升级增幅：改 UpgradeDamageBonus 常量。
/// - 改攻击特效：改 OnPlay 里 WithHitFx 的路径。
/// </summary>
public class Strike() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Basic,
    TargetType.AnyEnemy)
{
    /// <summary>基础伤害。</summary>
    private const int BaseDamage = 5;
    /// <summary>升级后伤害增加量。</summary>
    private const int UpgradeDamageBonus = 3;

    // 带 Strike 标签：与"对打击牌生效"的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    // DamageVar 声明伤害动态变量：卡面描述里的 !D! 会显示此数值（含力量等修正）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        // 对目标造成一次伤害，带斩击特效
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
