using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Relics;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【有翼佣卫】生成牌。
/// 0 灵魂、1 虚空。
/// 获得 3 点格挡，升级后 6 点。
/// 造物牌：额外获得等同于【驾驭】层数的格挡。
/// 模具遗物自动打出时不获得驾驭加成。
/// </summary>
public class WingedRetainerCard : PaleRegentModV1Card
{
    private const int VoidCost = 1;

    private const decimal BaseBlock = 3m;
    private const decimal UpgradeBlockBonus = 3m;

    private const string CalculatedBlockKey = "CalculatedBlock";
    private const string CalculationBaseKey = "CalculationBase";

    public WingedRetainerCard()
        : base(
            0,
            CardType.Skill,
            CardRarity.Token,
            TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Mould];

    public override bool IsCreationCard => true;

    public override bool IsMould => true;

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    /// <summary>
    /// 计算公式：
    ///
    /// CalculationBase + CalculationExtra × 驾驭层数
    ///
    /// 即：
    ///
    /// 基础格挡 + 1 × 驾驭层数
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(BaseBlock),

        // 注意：正确名称是 CalculationExtraVar。
        new CalculationExtraVar(1m),

        new CalculatedVar(CalculatedBlockKey)
            .WithMultiplier(GetHarnessAmount)
    ];

    /// <summary>
    /// 返回当前用于动态变量计算的驾驭层数。
    /// </summary>
    private static decimal GetHarnessAmount(
        CardModel card,
        Creature? target)
    {
        // 模具遗物自动打出的牌不吃驾驭。
        if (MouldRelic.MouldAutoPlayFlag)
        {
            return 0m;
        }

        HarnessPower? harness =
            card.Owner?.Creature.GetPower<HarnessPower>();

        return harness?.Amount ?? 0m;
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        CalculatedVar calculatedBlock =
            (CalculatedVar)DynamicVars[CalculatedBlockKey];

        decimal block = calculatedBlock.Calculate(cardPlay.Target);

        // 这里传入 ValueProp.Move，
        // 让最终格挡继续经过正常的格挡修正流程。
        await CreatureCmd.GainBlock(
            Owner.Creature,
            block,
            ValueProp.Move,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
        // 3 → 6。
        DynamicVars[CalculationBaseKey]
            .UpgradeValueBy(UpgradeBlockBonus);
    }
}
