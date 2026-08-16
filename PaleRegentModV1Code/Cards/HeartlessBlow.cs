using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【失心重击】攻击牌（表 C#68，0727 新增）。
/// 1 灵魂：造成 12 点伤害。自带【失心】（灵魂费转虚空费 1、重放 2）。
/// 升级后：18 点伤害。
/// </summary>
public class HeartlessBlow : PaleRegentModV1Card
{
    private const int BaseDamage = 12;
    private const int UpgradeDamageBonus = 6;

    public HeartlessBlow() : base(1,
        CardType.Attack, CardRarity.Common,
        TargetType.AnyEnemy)
    {
    }

    /// <summary>
    /// 自带失心：灵魂费并入虚空费、获得重放 1（见 CardTraits.ApplyLost）。
    /// 20260728 修复：不能在构造器里 ApplyLost（canonical 实例会抛
    /// CanonicalModelException 导致启动崩溃），改由基类在进入战斗时施加。
    /// </summary>
    public override bool HasInnateLost => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [ModHoverTips.Lost];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

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
