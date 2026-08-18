using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Patches;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【病态辐射】攻击牌。
/// 生成 1 张【感染】，并随机攻击；攻击次数为 1 + 本场战斗此前生成的感染数。
/// </summary>
public class PestilentRadiation() : PaleRegentModV1Card(
    0,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.RandomEnemy)
{
    private const int BaseDamage = 3;
    private const int UpgradeDamageBonus = 2;
    private const int BaseInfections = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new CurrentHitCountVar()
    ];

    /// <summary>
    /// 牌面和实际结算共用的攻击次数。
    /// 保持原逻辑：在本牌生成感染之前读取统计值。
    /// </summary>
    private static int GetHitCount()
    {
        return 1 + Math.Max(0, CombatCounters.InfectionGeneratedThisCombat);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先取次数，保持原实现的结算语义。
        int hits = GetHitCount();

        // 生成 1 张【感染】，并将本次生成计入战斗统计。
        await CardPileCmd.AddToCombatAndPreview<Infection>(
            Owner.Creature,
            PileType.Hand,
            BaseInfections,
            Owner);
        await Infection.NotifyGenerated(Owner.Creature, BaseInfections);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(CombatState!, true)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }

    /// <summary>
    /// 为本地化文本中的 {Amount} 提供实时攻击次数。
    /// </summary>
    private sealed class CurrentHitCountVar : DynamicVar
    {
        public CurrentHitCountVar() : base("Amount", 1m)
        {
        }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = GetHitCount();
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return GetHitCount();
        }

        public override string ToString()
        {
            return GetHitCount().ToString();
        }
    }
}