using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 技能牌。
/// 2 灵魂：预知{IfUpgraded:show:5|3}张牌。选择其中{IfUpgraded:show:2|1}张牌放入手牌，为其添加保留。
///
/// </summary>
public class Prescience() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    // <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [ModHoverTips.PrescienceRule
    ];
    private const int BasePreviewAmount = 3;
    private const int PreviewUpgradeBonus = 2;

    private const int BaseTakeAmount = 1;
    private const int TakeUpgradeBonus = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PreviewAmount", BasePreviewAmount),
        new DynamicVar("TakeAmount", BaseTakeAmount)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int previewAmount =
            (int)DynamicVars["PreviewAmount"].BaseValue;

        int takeAmount =
            (int)DynamicVars["TakeAmount"].BaseValue;

        await ScryTraitHelper.PreviewTopAndTake(
            choiceContext,
            Owner,
            previewAmount,
            takeAmount,
            new CardSelectorPrefs(
                SelectionScreenPrompt,
                takeAmount));

        // 后续需要对被选择的牌生效时，可以使用 chosen。
        // if (chosen != null)
        // {
            // 例如：chosen.SetToFreeThisTurn();
            // }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PreviewAmount"].UpgradeValueBy(PreviewUpgradeBonus);
        DynamicVars["TakeAmount"].UpgradeValueBy(TakeUpgradeBonus);
    }
}
