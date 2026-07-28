using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂支柱】攻击牌（表 C#80，0727 新增）。
/// 0 灵魂：造成 5 点伤害，你的消耗牌堆里每有 1 张牌，额外 +5 点伤害。
/// 升级后：每张 +7 点伤害。
/// </summary>
public class SoulPillars() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Rare,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;

    /// <summary>消耗堆每张牌的额外伤害（升级后 7）。</summary>
    private int _damagePerExhausted = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int exhaustedCount = CardPile.GetCards(Owner, PileType.Exhaust).Count();
        decimal totalDamage = DynamicVars.Damage.BaseValue + exhaustedCount * _damagePerExhausted;

        await DamageCmd.Attack(totalDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        _damagePerExhausted = 7;
    }
}
