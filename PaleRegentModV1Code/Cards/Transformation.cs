using PaleRegentModV1.PaleRegentModV1Code.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【转生】（表格 C#11，20260725 新增）。
/// 4 灵魂 技能/Rare：恢复 3 点生命。在你的抽牌堆生成一张【弃壳】。虚无。
/// 升级：生成【弃壳+】（恢复量与费用不变）。
///
/// 实现说明：
/// - 恢复生命：CreatureCmd.Heal（与 WhiteRootPower 同款 API）。
/// - 弃壳生成到抽牌堆（PileType.Draw），升级后生成升级版。
/// - 关键词：虚无（Ethereal，回合结束未打出则消耗）。
/// </summary>
public class Transformation() : PaleRegentModV1Card(4,
    CardType.Skill, CardRarity.Rare,
    TargetType.None)
{
    /// <summary>恢复生命量。</summary>
    private const int HealAmount = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal];

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<CastOffShell>(IsUpgraded),
         HoverTipFactory.FromPower<SoulNextTurnPower>((int?)null)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 恢复 3 点生命
        await CreatureCmd.Heal(cardPlay.Player.Creature, (decimal)HealAmount, true);

        // 2. 在抽牌堆生成一张弃壳（升级后生成弃壳+）
        CardModel shell = Owner.Creature.CombatState.CreateCard<CastOffShell>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(shell, (CardPreviewStyle)1);
        }
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(shell, PileType.Draw, Owner, (CardPilePosition)1),
            2.2f, (CardPreviewStyle)1);
    }

    protected override void OnUpgrade()
    {
        // 升级仅改变生成的弃壳为升级版（牌面 {IfUpgraded:show:弃壳+|弃壳}）
    }
}
