using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Patches;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂双刃】攻击牌。
/// 造成等同于本场战斗中，本牌所属玩家获得的灵魂点数与虚空点数总和的伤害。 //应该计入所有的点数变化，而不只是玩家出牌主动获得的点数
/// 升级后费用由 2 灵魂 + 2 虚空变为 1 灵魂 + 1 虚空。
/// </summary>
public class SoulBlades : PaleRegentModV1Card
{
    private const int BaseVoidCost = 2;

    public SoulBlades() : base(
        2,
        CardType.Attack,
        CardRarity.Rare,
        TargetType.AnyEnemy)
    {
        CardTraits.SetVoidCost(this, BaseVoidCost);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CurrentSoulBladesDamageVar()
    ];

    /// <summary>
    /// 牌面和实际结算的唯一伤害来源。
    /// card 为 null 时返回 0，避免图鉴/无归属预览访问玩家计数器。
    /// </summary>
    private static int GetCurrentDamage(CardModel? card)
    {
        // 图鉴、奖励预览等场景：使用固定基础伤害。
        if (!card.IsMutable)
        {
            return 0;
        }
        
        if (card?.Owner == null)
        {
            return 0;
        }

        return Math.Max(0, SoulBladesEnergyTracker.GetTotal(card.Owner));
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 与牌面 {Amount} 使用完全相同的取值方法。
        int damage = GetCurrentDamage(this);
        if (damage <= 0)
        {
            return;
        }

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        CardTraits.SetVoidCost(this, BaseVoidCost - 1);
    }

    /// <summary>
    /// 为本地化中的 {Amount} 提供基于本牌 Owner 的实时伤害值。
    /// </summary>
    private sealed class CurrentSoulBladesDamageVar : DynamicVar
    {
        public CurrentSoulBladesDamageVar() : base("Amount", 0m)
        {
        }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = GetCurrentDamage(card);
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return GetCurrentDamage(_owner as CardModel);
        }

        public override string ToString()
        {
            return GetCurrentDamage(_owner as CardModel).ToString();
        }
    }
}