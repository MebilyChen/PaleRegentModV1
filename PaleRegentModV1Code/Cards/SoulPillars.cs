using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂支柱】
/// 造成5点伤害。
/// 消耗牌堆中每有1张牌，额外造成5点伤害。
/// 升级后每张额外造成7点伤害。
/// </summary>
public class SoulPillars() : PaleRegentModV1Card(
    0,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int DamagePerExhaustedCard = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // 计算公式中的固定基础伤害。
        new CalculationBaseVar(BaseDamage),

        // 每张消耗牌提供的额外伤害。
        new ExtraDamageVar(DamagePerExhaustedCard),

        // CalculationBase + ExtraDamage × 消耗堆牌数。
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier(
                (CardModel card, Creature? _) =>
                    PileType.Exhaust
                        .GetPile(card.Owner)
                        .Cards
                        .Count)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(
            cardPlay.Target,
            nameof(cardPlay.Target));

        decimal damage =
            ((CalculatedVar)DynamicVars["CalculatedDamage"])
            .Calculate(cardPlay.Target);

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 每张牌的额外伤害由5提升至7。
        DynamicVars["ExtraDamage"].UpgradeValueBy(2m);
    }
}