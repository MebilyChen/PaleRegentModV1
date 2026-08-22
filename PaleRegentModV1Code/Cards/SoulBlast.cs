using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂爆破】攻击牌。
/// 造成伤害。本场战斗中，每次打出本牌后，所有【灵魂爆破】的攻击次数增加。
/// </summary>
public class SoulBlast : PaleRegentModV1Card
{
    private const string HitCountKey = "Amount";
    private const int BaseDamage = 10;
    private const int UpgradeDamageBonus = 2;
    private const int BaseHitCount = 1;
    private const int BaseHitCountIncrease = 1;
    private const int UpgradeHitCountIncrease = 2;

    // 每张【灵魂爆破】在本场战斗中累计获得的额外攻击次数。
    private int _bonusHitCount;

    private int HitCount => BaseHitCount + _bonusHitCount;

    public SoulBlast() : base(2,
        CardType.Attack, CardRarity.Rare,
        TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new CurrentHitCountVar(this)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 按当前卡面显示的攻击次数进行攻击。
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(HitCount)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 攻击结算后，提高本场战斗内所有【灵魂爆破】后续的攻击次数。
        int hitCountIncrease = IsUpgraded
            ? UpgradeHitCountIncrease
            : BaseHitCountIncrease;

        foreach (SoulBlast blast in Owner.PlayerCombatState!.AllCards.OfType<SoulBlast>())
        {
            blast._bonusHitCount += hitCountIncrease;
        }
    }

    protected override void OnUpgrade()
    {
        // 伤害：5 → 7。攻击次数增加量由文案的 IfUpgraded 和 OnPlay 中的 IsUpgraded 控制。
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }

    /// <summary>
    /// 向本地化的 {Amount:diff()} 提供本卡本场战斗中的实时攻击次数。
    /// 当前值为 1 + 本卡累计获得的额外攻击次数。
    /// </summary>
    private sealed class CurrentHitCountVar : DynamicVar
    {
        private readonly SoulBlast _card;

        public CurrentHitCountVar(SoulBlast card) : base(HitCountKey, BaseHitCount)
        {
            _card = card;
        }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = card is SoulBlast blast
                ? blast.HitCount
                : BaseHitCount;
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return _card.HitCount;
        }

        public override string ToString()
        {
            return _card.HitCount.ToString();
        }
    }
}
