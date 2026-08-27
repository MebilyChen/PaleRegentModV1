using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚蚀重击】攻击牌（表 C#91，0727 新增）。
/// 2 灵魂 + 3 虚空：造成 35 点伤害，施加 5 层【虚空之触】。失心。
/// 升级后：40 点伤害，7 层。
/// </summary>
public class VoidscarStrike : PaleRegentModV1Card
{
    private const int BaseDamage = 35;
    private const int UpgradeDamageBonus = 5;
    private const int VoidCost = 3;
    private const string TouchKey = "VoidTouchPower";

    public VoidscarStrike() : base(2,
        CardType.Attack, CardRarity.Uncommon,
        TargetType.AnyEnemy)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    /// <summary>自带失心（20260728 修复：禁止构造器 ApplyLost，改由基类在入战时施加）。</summary>
    public override bool HasInnateLost => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move),
         new PowerVar<VoidTouchPower>(5)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost,HoverTipFactory.FromPower<VoidTouchPower>((int?)null) ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<VoidTouchPower>(choiceContext, cardPlay.Target,
                DynamicVars[TouchKey].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        DynamicVars[TouchKey].UpgradeValueBy(2);
    }
}
