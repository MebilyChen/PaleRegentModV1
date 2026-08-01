using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【失心诅咒】（表格 C#12，20260725 新增）。
/// 0 灵魂 3 虚空 能力/Rare：你生成的牌获得【失心】。
/// 升级：虚空费 4 → 2。
///
/// 实现说明：
/// - 打出后施加 LostDestinyPower（标记型 Buff）。
/// - 生效点：PaleRegentModV1Card.AfterCardGeneratedForCombat 基类钩子，
///   任何生成入战斗的本 Mod 卡牌在生成时校验持有者是否有该 Power，
///   若可失心（CardTraits.CanApplyLost）则自动附加【失心】。
/// - 能力牌打出后移出战斗（Power 卡默认行为，由 CardType.Power 处理）。
/// </summary>
public class LostDestiny : PaleRegentModV1Card
{
    /// <summary>基础虚空费（升级后 2）。</summary>
    private const int BaseVoidCost = 3;
    private const int UpgradedVoidCost = 2;

    public LostDestiny() : base(0,
        CardType.Power, CardRarity.Rare,
        TargetType.None)
    {
        CardTraits.SetVoidCost(this, BaseVoidCost);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<LostDestinyPower>(1)];

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<LostDestinyPower>((int?)null),
         ModHoverTips.Lost];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<LostDestinyPower>(
            choiceContext, cardPlay.Player.Creature, 1,
            cardPlay.Player.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：虚空费 4 → 2
        CardTraits.SetVoidCost(this, UpgradedVoidCost);
    }
}
