using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【返祖】稀有能力牌（后期引擎）。
/// 4 灵魂：获得【返祖】buff——你每打出一张牌，回复 1 点灵魂。每次获得虚空时，额外选择至多1张手牌添加失心。
///
/// 定位：一次性投资 4 灵魂，之后每张牌都便宜 1 费，
/// 与失心牌（0 灵魂费）联动时每张牌净赚 1 灵魂。
///
/// 修改指南：
/// - 每张牌回复量 = 施加的层数（PowerStacks 常量），改成 2 就是每张牌回 2。
/// - 触发逻辑在 Powers/AtavismPower.cs 里改。
/// </summary>
public class Atavism() : PaleRegentModV1Card(4,
    CardType.Power, CardRarity.Rare,
    TargetType.Self)
{
    /// <summary>施加的返祖层数（= 每打出一张牌回复的灵魂数）。</summary>
    private const int PowerStacks = 1;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost, HoverTipFactory.FromPower<AtavismPower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AtavismPower>(choiceContext, cardPlay.Player.Creature, PowerStacks, cardPlay.Player.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：费用 4→3（UpgradeBy 是升级语义的标准降费 API，卡面会显示绿色费用）
        EnergyCost.UpgradeBy(-1);
    }
}
