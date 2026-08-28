using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空崇拜】技能牌（表 C#69，0727 新增）。
/// 1 灵魂：获得 2 点虚空。为1张手牌添加[gold]失心[/gold]。
/// 升级后：0灵魂。
/// </summary>
public class VoidWorship() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    /// <summary>获得的虚空数量（升级后 3）。</summary>
    private int _voidGain = 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost, HoverTipFactory.FromPower<VoidPower>((int?)null)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await VoidResource.Gain(cardPlay.Player, _voidGain);
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
        // 从手牌选择牌附加【失心】
        // filter：过滤掉不能失心的牌（X 费牌）和自己
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1),
            (CardModel c) => c != this && CardTraits.CanApplyLost(c),
            this);

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyLost(card);
        }
    }

    protected override void OnUpgrade()
    {
        //_voidGain = 3;
        // 升级：费用 1→0（UpgradeBy 是升级语义的标准降费 API，卡面会显示绿色费用）
        EnergyCost.UpgradeBy(-1);
    }
}
