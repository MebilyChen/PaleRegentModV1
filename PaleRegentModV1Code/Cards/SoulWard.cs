using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂护佑】（表格 C#14，20260725 新增）。
/// 1 灵魂 技能/Common：抽 2(3) 张牌，选择一张牌施加【纯粹】。
///
/// 实现说明：
/// - 【纯粹】特质（名词表）：带纯粹的牌不会被【感染】的疑虑效果变形
///   （见 Infection.OnTurnEndInHand 的 CardTraits.IsPure 过滤）。
/// - 施加方式：CardTraits.ApplyPure（附加状态，非 virtual IsPure）。
/// </summary>
public class SoulWard() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.None)
{
    /// <summary>基础抽牌数（升级后 3）。</summary>
    private const int BaseDraw = 2;
    private const int UpgradedDraw = 3;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Pure];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Draw", BaseDraw)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 抽牌
        await CardPileCmd.Draw(choiceContext, (int)DynamicVars["Draw"].BaseValue, cardPlay.Player);

        // 2. 选择一张手牌施加【纯粹】
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            (CardModel c) => c != this && !CardTraits.IsPure(c),
            this);
        foreach (CardModel card in selected)
        {
            CardTraits.ApplyPure(card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(UpgradedDraw - BaseDraw);
    }
}
