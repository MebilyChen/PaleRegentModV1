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
/// 生成 1(2) 张【感染】，并随机攻击；攻击次数为 1 + 本场战斗此前生成的感染数。
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
    private const int UpgradeInfectionsBonus = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new CurrentHitCountVar()
    ];

    /// <summary>
    /// 牌面和实际结算共用的攻击次数。
    /// 保持原逻辑：在本牌生成感染之前读取统计值。
    /// </summary>
    private int GetHitCount()
    {
        // 读取时再兜底确认一次战斗身份。
        CombatCounters.EnsureInfectionCombat(CombatState);
        return 1 + Math.Max(0, CombatCounters.InfectionGeneratedThisCombat);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先取次数，保持原实现的结算语义：本次生成感染前读取统计值。
        int hits = GetHitCount();

        // 未升级：1 张；升级后：2 张。
        int infectionsToGenerate = BaseInfections
                                   + (IsUpgraded ? UpgradeInfectionsBonus : 0);

        await CardPileCmd.AddToCombatAndPreview<Infection>(
            Owner.Creature,
            PileType.Hand,
            infectionsToGenerate,
            Owner);

        // 统计必须与实际生成数量一致，否则后续攻击次数会少算。
        await Infection.NotifyGenerated(Owner.Creature, infectionsToGenerate);

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
            // 非常重要：
            // 不要保存构造 CurrentHitCountVar 时的卡牌引用，
            // 而是使用当前正在刷新牌面的实际 CardModel。
            if (card is PestilentRadiation radiation)
            {
                PreviewValue = radiation.GetHitCount();
            }
            else
            {
                PreviewValue = 1m;
            }
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return PreviewValue;
        }

        public override string ToString()
        {
            return PreviewValue.ToString();
        }
    }
}