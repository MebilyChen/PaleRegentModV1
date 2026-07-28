using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【王命改铸】技能牌（表 C#93，0727 新增）。
/// 0 灵魂 + 2 虚空：选择手牌中任意数量的牌，将其变化为【国王俑卫】。
/// 升级后：0 灵魂 + 1 虚空。
/// 备注：变化=原牌移出战斗（消耗不触发消耗联动），在手牌生成等量国王俑卫。
/// </summary>
public class RoyalRecasting : PaleRegentModV1Card
{
    private const int VoidCost = 2;

    public RoyalRecasting() : base(0,
        CardType.Skill, CardRarity.Uncommon,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<KingsRetainer>(false)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardPile hand = PileTypeExtensions.GetPile(PileType.Hand, Owner);
        // 排除自己（打出中不在手牌，稳妥起见过滤）
        if (!hand.Cards.Any(c => c != this))
        {
            return;
        }

        // 任意数量：0 到当前手牌数
        List<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0,
                hand.Cards.Count(c => c != this)),
            (Func<CardModel, bool>)((CardModel c) => c != this), this)).ToList();

        foreach (CardModel card in selected)
        {
            // 变化为国王俑卫（原牌移出战斗，原位生成替换牌）
            await CardCmd.TransformTo<KingsRetainer>(card);
        }
    }

    protected override void OnUpgrade()
    {
        CardTraits.SetVoidCost(this, VoidCost - 1);
    }
}
