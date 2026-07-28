using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 【王者之翼 Monarch Wings】稀有遗物（机制表：遗物 R#10，0727 新增）。
/// 效果：战斗开始时，预知 5，选择 1 张牌加入手牌。
///
/// 实现说明：
/// - 复用 ScryTraitHelper.PreviewTopAndTake（与【先见 Prescience】同一套
///   预知工具）：查看抽牌堆顶 5 张，选 1 张加入手牌，未选中的留在原位。
/// - "战斗开始"的挂点用 AfterPlayerTurnStart + TurnNumber &lt;= 1
///   （同原版 GamblingChip）：需要玩家选牌交互，必须有 choiceContext，
///   而 BeforeCombatStart 钩子不带 choiceContext；放在第 1 回合开始时
///   触发，正好在起始抽牌之后，与"战斗开始"体验一致。
/// - 选牌提示语走 RelicModel.SelectionScreenPrompt
///   （relics.json 的 .selectionScreenPrompt 条目）。
/// </summary>
public class MonarchWings : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    /// <summary>悬停展示【预知】规则词条（与 Prescience 卡一致）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.PrescienceRule];

    private const int PreviewAmount = 5;
    private const int TakeAmount = 1;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.PlayerCombatState.TurnNumber > 1)
        {
            return;
        }

        Flash();
        await ScryTraitHelper.PreviewTopAndTake(
            choiceContext,
            Owner,
            PreviewAmount,
            TakeAmount,
            new CardSelectorPrefs(SelectionScreenPrompt, TakeAmount));
    }
}
