using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 技能牌。
/// 1 灵魂：抽{IfUpgraded:show:2|1}张牌。获得{Block:diff()}点格挡。选择手牌中1张牌放入抽牌堆顶部，为其添加保留。
///
/// </summary>
public class AwaitDrawpile() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    /// <summary>基础格挡。</summary>
    private const int BaseBlock = 3;
    /// <summary>升级后格挡增加量。</summary>
    private const int UpgradeBlockBonus = 2;

    // 声明"这张牌提供格挡"，游戏会据此显示格挡预览等 UI
    public override bool GainsBlock => true;

    // 带 Defend 标签：与"对防御牌生效"的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    // BlockVar 声明格挡动态变量：卡面描述里的 !B! 会显示此数值（含敏捷等修正）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(BaseBlock, ValueProp.Move),
        new DynamicVar("Draw", BaseDraw)
    ];
    
    /// <summary>基础抽牌数。</summary>
    private const int BaseDraw = 1;
    private const int UpgradedDraw = 2;
    
    private async Task<CardModel?> SelectOneCardFromHand(
        PlayerChoiceContext choiceContext)
    {
        return (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            null,
            this
        )).FirstOrDefault();
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block,
            cardPlay);

        await CardPileCmd.Draw(
            choiceContext,
            DynamicVars["Draw"].IntValue,
            Owner);

        CardModel? selected = await SelectOneCardFromHand(choiceContext);

        if (selected == null)
            return;
        
        // 仅在该牌原本没有“保留”时添加，避免重复操作。
        if (!selected.Keywords.Contains(CardKeyword.Retain))
        {
            selected.AddKeyword(CardKeyword.Retain);
        }

        // 按你的本地游戏版本补齐 CardPileCmd.Add 的参数。
        await CardPileCmd.Add(
            selected,
            PileType.Draw,
            CardPilePosition.Top);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
        DynamicVars["Draw"].UpgradeValueBy(UpgradedDraw - BaseDraw);
    }
}
