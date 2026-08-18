using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【驯化】技能牌（机制文档：卡牌表 C#28，20260725 批次改版）。
/// 2 灵魂 技能：消耗你所有牌堆（手牌/抽牌堆/弃牌堆）中全部的【虚空】状态牌：
/// ≥5 张 → 获得【虚空化神】；
/// ≥2 张 → 获得【虚空化形】；
/// 否则 → 获得【失败实验】。
/// 升级后：生成升级版（虚空化神+/虚空化形+/失败实验+）。
/// </summary>
public class Tame() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Rare,
    TargetType.Self)
{
    private const int GodThreshold = 5;
    private const int FormThreshold = 2;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<VoidGivenFocus>(IsUpgraded),
         HoverTipFactory.FromCard<VoidGivenForm>(IsUpgraded),
         HoverTipFactory.FromCard<FailedExperiment>(IsUpgraded),
         //HoverTipFactory.FromCard<TheVoidStatus>(false)
        ];

    //public override IEnumerable<CardKeyword> CanonicalKeywords =>
       // [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 消耗所有牌堆（手牌/抽牌堆/弃牌堆）中全部的【虚空】状态牌（表格 G32）
        List<CardModel> voids = CardPile.GetCards(Owner, PileType.Hand)
            .Concat(CardPile.GetCards(Owner, PileType.Draw))
            .Concat(CardPile.GetCards(Owner, PileType.Discard))
            .Where(c => c is TheVoidStatus)
            .ToList();

        foreach (CardModel v in voids)
        {
            await CardCmd.Exhaust(choiceContext, v);
        }

        // ≥5 化神；≥2 化形；否则失败实验（表格 G32：三档必得其一）
        CardModel made;
        if (voids.Count >= GodThreshold)
        {
            made = Owner.Creature.CombatState.CreateCard<VoidGivenFocus>(Owner);
        }
        else if (voids.Count >= FormThreshold)
        {
            made = Owner.Creature.CombatState.CreateCard<VoidGivenForm>(Owner);
        }
        else
        {
            made = Owner.Creature.CombatState.CreateCard<FailedExperiment>(Owner);
        }

        if (made != null)
        {
            // 升级后：生成升级版（虚空化神+/虚空化形+）
            if (IsUpgraded)
            {
                CardCmd.Upgrade(made, (CardPreviewStyle)1);
            }
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(made, PileType.Hand, Owner, (CardPilePosition)1),
                2.2f, (CardPreviewStyle)1);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：生成升级版牌（见 OnPlay 的 IsUpgraded 分支）
    }
}
