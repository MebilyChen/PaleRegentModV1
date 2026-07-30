using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂护佑】（表格 C#14，20260725 新增）。
/// 1 灵魂 技能/Common：抽 2(3) 张牌，选择一张牌施加【纯粹】。若其中包含【感染】，额外获得3点格挡。
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
    /// <summary>基础格挡。</summary>
    private const int BaseBlock = 3;

    // 声明"这张牌提供格挡"，游戏会据此显示格挡预览等 UI
    public override bool GainsBlock => true;

    // 带 Defend 标签：与"对防御牌生效"的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    // BlockVar 声明格挡动态变量：卡面描述里的 !B! 会显示此数值（含敏捷等修正）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move), new DynamicVar("Draw", BaseDraw)];
    
    /// <summary>基础抽牌数（升级后 3）。</summary>
    private const int BaseDraw = 2;
    private const int UpgradedDraw = 3;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Pure, HoverTipFactory.FromCard<Infection>(false)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 抽牌
        HashSet<CardModel> before = CardPile.GetCards(Owner, PileType.Hand).ToHashSet();
        await CardPileCmd.Draw(choiceContext, (int)DynamicVars["Draw"].BaseValue, cardPlay.Player);
        IEnumerable<CardModel> drawn =
            CardPile.GetCards(Owner, PileType.Hand).Where(c => !before.Contains(c));
        // 2. 抽到感染 → 获得3点格挡
        foreach (CardModel card in drawn)
        {
            if (card is Infection)
            {
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            }
        }

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
