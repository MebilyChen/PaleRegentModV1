using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【咒痛宣告】技能牌。
/// X 灵魂：对所有敌人施加 X 层【苦痛之路】；每张诅咒额外增加 1 层。
/// 升级后额外增加 1 层。
/// </summary>
public class CurseboundAgony() : PaleRegentModV1Card(
    0,
    CardType.Skill,
    CardRarity.Rare,
    TargetType.AllEnemies)
{
    protected override bool HasEnergyCostX => true;

    /// <summary>升级后的额外层数（X+1）。</summary>
    private int _upgradeBonus;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PathOfPainPower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CurrentAmountVar()
    ];

    /// <summary>统计手牌、抽牌、弃牌和消耗堆中的所有诅咒。</summary>
    private int GetCurseCount()
    {
        return CardPile.GetCards(
                Owner,
                PileType.Hand,
                PileType.Draw,
                PileType.Discard,
                PileType.Exhaust)
            .Count(c => c.Type == CardType.Curse);
    }

    /// <summary>
    /// 牌面与实际 PowerCmd.Apply 共用的最终层数。
    /// ResolveEnergyXValue 会取当前 X 灵魂支付值；在手牌预览时则按当前可用 X 值显示。
    /// </summary>
    private int GetAmount()
    {
        return ResolveEnergyXValue() + GetCurseCount() + _upgradeBonus;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = GetAmount();
        if (amount <= 0)
        {
            return;
        }

        foreach (Creature enemy in CombatState!.GetOpponentsOf(Owner.Creature)
                     .Where(c => c.IsAlive)
                     .ToList())
        {
            await PowerCmd.Apply<PathOfPainPower>(
                choiceContext,
                enemy,
                amount,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        _upgradeBonus = 1;
    }

    /// <summary>为本地化中的 {Amount} 提供当前最终施加层数。</summary>
    private sealed class CurrentAmountVar : DynamicVar
    {
        public CurrentAmountVar() : base("Amount", 0m)
        {
        }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = (card as CurseboundAgony)?.GetAmount() ?? 0;
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return (_owner as CurseboundAgony)?.GetAmount() ?? 0;
        }

        public override string ToString()
        {
            return ((_owner as CurseboundAgony)?.GetAmount() ?? 0).ToString();
        }
    }
}