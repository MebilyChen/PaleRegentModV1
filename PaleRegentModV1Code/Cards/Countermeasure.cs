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

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【应变一击】攻击牌（表 C#61，0727 新增）。
/// 1 灵魂：造成 7 点伤害；若目标意图为攻击，施加 1 层虚弱，否则施加 1 层易伤。
/// 升级后：10 点伤害，施加层数变为 2。
/// </summary>
public class Countermeasure() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Common,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 7;
    private const int UpgradeDamageBonus = 3;

    /// <summary>施加的虚弱/易伤层数（升级后 2）。</summary>
    private int _debuffStacks = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(_debuffStacks),
        HoverTipFactory.FromPower<VulnerablePower>(_debuffStacks)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 意图判定：意图为攻击 → 虚弱；否则 → 易伤
        if (cardPlay.Target.IsAlive)
        {
            bool intendsToAttack = cardPlay.Target.Monster?.IntendsToAttack ?? false;
            if (intendsToAttack)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, _debuffStacks, Owner.Creature, this);
            }
            else
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, _debuffStacks, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        _debuffStacks = 2;
    }
}
