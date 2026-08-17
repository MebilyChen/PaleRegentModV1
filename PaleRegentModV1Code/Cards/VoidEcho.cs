using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Patches;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空回声】技能牌（表 C#64，0727 新增）。
/// 3 灵魂：生成等同于本场战斗中已生成过的虚空总量的虚空。消耗。为抽牌堆任意张牌添加[gold]失心[/gold]。
/// 升级后：2 灵魂（表格升级列未明示，按惯例降费处理，已在此备注，如需调整告知）。
/// </summary>
public class VoidEcho() : PaleRegentModV1Card(3,
    CardType.Skill, CardRarity.Rare,
    TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost, HoverTipFactory.FromPower<VoidPower>((int?)null)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先读取计数再 Gain，否则本次获得会被计入并导致数值翻倍。
        int amount = CombatCounters.VoidGainedThisCombat;
        if (amount > 0)
        {
            await VoidResource.Gain(cardPlay.Player, amount);
            await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
        }

        // 从抽牌堆中选择任意张可附加【失心】的牌。
        // MinSelect 为 0，因此可以不选择；MaxSelect 为合格牌数量，因此可全选。
        List<CardModel> eligibleCards = PileType.Draw.GetPile(cardPlay.Player).Cards
            .Where(c => CardTraits.CanApplyLost(c))
            .ToList();

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            eligibleCards,
            cardPlay.Player,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, eligibleCards.Count));

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyLost(card);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
