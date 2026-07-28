using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂光环】攻击牌（表 C#78，0727 新增）。
/// 2 灵魂：对随机敌人造成 5 点伤害 3 次，对所有敌人施加 1 层虚弱。
/// 升级后：5 点伤害 4 次，2 层虚弱。
/// </summary>
public class SoulHalos() : PaleRegentModV1Card(2,
    CardType.Attack, CardRarity.Common,
    TargetType.RandomEnemy)
{
    private const int BaseDamage = 5;
    private const int BaseHits = 3;
    private const int UpgradeHitsBonus = 1;

    /// <summary>对所有敌人施加的虚弱层数（升级后 2）。</summary>
    private int _weakStacks = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move),
         new RepeatVar(BaseHits)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对随机敌人造成 5 点伤害，共 Repeat 次
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(CombatState!, true)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 对所有敌人施加虚弱
        await PowerCmd.Apply<WeakPower>(choiceContext,
            CombatState!.GetOpponentsOf(Owner.Creature),
            _weakStacks, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(UpgradeHitsBonus);
        _weakStacks = 2;
    }
}
