using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【痛苦汇流】攻击牌。
/// 造成伤害，并施加等同于本场所有玩家已打出攻击牌数的【苦痛之路】；本张计入其中。
/// </summary>
public class ConfluenceOfPain() : PaleRegentModV1Card(
    1,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AnyEnemy)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    private const int BaseDamage = 12;
    private const int UpgradeDamageBonus = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new CurrentAmountVar()
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PathOfPainPower>(1)];

    /// <summary>历史中已经登记的所有玩家攻击牌数。</summary>
    private static int GetRecordedAttackCount()
    {
        return CombatManager.Instance?.History.CardPlaysStarted
            .Count(e => e.CardPlay.Card.Type == CardType.Attack) ?? 0;
    }

    /// <summary>
    /// 打出前的牌面预览需要把即将打出的本张攻击计入。
    /// </summary>
    private static int GetPreviewAmount()
    {
        return GetRecordedAttackCount() + 1;
    }

    /// <summary>
    /// OnPlay 时本张已经登记进 CardPlaysStarted，因此不能再 +1。
    /// </summary>
    private static int GetPlayAmount()
    {
        return GetRecordedAttackCount();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        int amount = GetPlayAmount();
        if (amount > 0 && cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<PathOfPainPower>(
                choiceContext,
                cardPlay.Target,
                amount,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }

    /// <summary>
    /// 为本地化中的 {Amount} 提供“含本张”的牌面预览层数。
    /// </summary>
    private sealed class CurrentAmountVar : DynamicVar
    {
        public CurrentAmountVar() : base("Amount", 1m)
        {
        }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = GetPreviewAmount();
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return GetPreviewAmount();
        }

        public override string ToString()
        {
            return GetPreviewAmount().ToString();
        }
    }
}