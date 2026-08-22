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
        // 图鉴、奖励预览等会使用 Canonical 卡牌；此时不能读取 Owner。
        if (!IsMutable)
        {
            return 0;
        }

        // 运行时卡牌才允许访问 Owner。
        if (Owner?.Deck == null)
        {
            return 0;
        }

        return CardPile.GetCards(
                Owner,
                PileType.Hand,
                PileType.Draw,
                PileType.Discard,
                PileType.Exhaust)
            .Count(c => c.Type == CardType.Curse);
    }

    /// <summary>
    /// 牌面 {Amount} 对应的“额外添加”层数：诅咒数量加上升级奖励。
    /// 不包含本牌支付的 X 灵魂，避免将最终总层数误显示为额外层数。
    /// </summary>
    private int GetExtraAmount()
    {
        return GetCurseCount() + _upgradeBonus;
    }

    /// <summary>
    /// 实际施加的总层数：支付的 X 灵魂加上额外层数。
    /// 在牌组、图鉴和奖励等非战斗预览中，没有可支付的 X 值，因此 X 按 0 处理。
    /// </summary>
    private int GetTotalAmount()
    {
        int xValue = CombatState == null ? 0 : ResolveEnergyXValue();
        return xValue + GetExtraAmount();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int totalAmount = GetTotalAmount();
        if (totalAmount <= 0)
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
                totalAmount,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        _upgradeBonus = 1;
    }

    /// <summary>为本地化中的 {Amount} 提供“额外添加”的层数，而非实际总层数。</summary>
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
            PreviewValue = (card as CurseboundAgony)?.GetExtraAmount() ?? 0;
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return (_owner as CurseboundAgony)?.GetExtraAmount() ?? 0;
        }

        public override string ToString()
        {
            return ((_owner as CurseboundAgony)?.GetExtraAmount() ?? 0).ToString();
        }
    }
}
