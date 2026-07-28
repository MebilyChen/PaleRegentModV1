using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【蚀心一击】攻击牌（表 C#92，0727 新增）。
/// 1 灵魂 + 1 虚空：造成 18 点伤害，施加 1 层虚弱、1 层易伤。失心。
/// 升级后：25 点伤害，各 2 层。
/// </summary>
public class HollowingStrike : PaleRegentModV1Card
{
    private const int BaseDamage = 18;
    private const int UpgradeDamageBonus = 7;
    private const int VoidCost = 1;

    /// <summary>虚弱/易伤层数（升级后 2）。</summary>
    private int _debuffAmount = 1;

    public HollowingStrike() : base(1,
        CardType.Attack, CardRarity.Uncommon,
        TargetType.AnyEnemy)
    {
        CardTraits.SetVoidCost(this, VoidCost);
        CardTraits.ApplyLost(this);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost,
         HoverTipFactory.FromPower<WeakPower>(_debuffAmount),
         HoverTipFactory.FromPower<VulnerablePower>(_debuffAmount)];

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
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target,
                _debuffAmount, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target,
                _debuffAmount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        _debuffAmount = 2;
    }
}
