using MegaCrit.Sts2.Core.HoverTips;
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
/// 【集火敕令】攻击牌（机制文档：卡牌表 C#34）。
/// 1 灵魂 攻击：造成 1 点伤害，对自身施加 1 层【瘟疫】，对目标施加 1 层【旧日仇敌】。
/// 升级后：造成 1 点伤害2次，施加 2 层瘟疫（旧日仇敌仍为 1 层）。
/// 20260725 批次：集火改用独立的 AncientEnemyPower，不再用 PlaguePower.FocusTarget 静态字段。
/// 备注：表格 G38 牌面文案未写"造成 1 点伤害"，但 P38 效果说明有 1 点伤害，代码保留 1 伤。
/// </summary>
public class FocusFireEdict() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 1;
    private const int BasePlague = 1;
    private const int BaseAncientEnemy = 1;
    private const int BaseHitCount = 1;
    private const int UpgradeHitCountBonus = 1;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PlaguePower>((int?)null),
         HoverTipFactory.FromPower<AncientEnemyPower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move), new PowerVar<PlaguePower>(BasePlague),
         new PowerVar<AncientEnemyPower>(BaseAncientEnemy)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        // 不升级此变量，因此始终施加 1 层旧日仇敌。
        await PowerCmd.Apply<AncientEnemyPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["AncientEnemyPower"].BaseValue,
            Owner.Creature,
            this);
        
        // 未升级：1 层；升级后：2 层。
        await PowerCmd.Apply<PlaguePower>(
            choiceContext,
            cardPlay.Player.Creature,
            DynamicVars["PlaguePower"].BaseValue,
            Owner.Creature,
            this);

        
        // 未升级：1 次；升级后：2 次。
        int hitCount = BaseHitCount
                       + (IsUpgraded ? UpgradeHitCountBonus : 0);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
       
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PlaguePower"].UpgradeValueBy(1m);
    }
}
