using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【聚焦】初始牌组功能牌。
/// 0 灵魂：获得 5 点格挡，获得 1 点灵魂（能量），抽 1 张牌，
/// 为至多一张手牌添加【苍白】。【保留】【消耗】。
/// 升级后：获得 7 点格挡，抽 2 张牌。
///
/// 定位：应急资源牌——保留在手，需要时白嫖格挡和一点灵魂，用完即弃（消耗）。
/// 也是【再利用】（Recycle）把状态牌“虚空”转化的目标牌。
///
/// 修改指南：
/// - 改格挡：BaseBlock / UpgradeBlockBonus 常量。
/// - 改灵魂回复量：EnergyGain 常量。
/// - 改选牌数量：OnPlay 中 CardSelectorPrefs 的最大选择数。
/// - 关键词在 CanonicalKeywords 里调整（Retain=保留 / Exhaust=消耗）。
/// </summary>
public class Focus() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Basic,
    TargetType.Self)
{
    /// <summary>基础格挡。</summary>
    private const int BaseBlock = 5;
    /// <summary>升级后格挡增加量。</summary>
    private const int UpgradeBlockBonus = 2;
    /// <summary>打出后获得的灵魂（能量）数。</summary>
    private const int EnergyGain = 1;
    /// <summary>基础抽牌数。</summary>
    private const int BaseDraw = 1;
    /// <summary>升级后抽牌增加量。</summary>
    private const int UpgradeDrawBonus = 1;

    // 带 Defend 标签：与“对防御牌生效”的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    public override bool GainsBlock => true;

    // 固有关键词：保留（回合结束不弃掉）+ 消耗（打出后移出战斗）
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Pale];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move), new DynamicVar("Draw", BaseDraw)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得格挡。
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 获得 1 点灵魂（能量）。
        await PlayerCmd.GainEnergy(EnergyGain, cardPlay.Player);

        // 3. 抽牌；随后再选牌，使本次抽到的牌也可以成为苍白目标。
        await CardPileCmd.Draw(
            choiceContext,
            (int)DynamicVars["Draw"].BaseValue,
            cardPlay.Player);

        // 4. 至多选择 1 张当前手牌附加【苍白】。
        // CardSelectorPrefs 的最小选择数为 0，因此可以直接取消；
        // CanApplyPale 会排除规范实例、已有苍白的牌和虚空 X 费牌。
        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1),
            card => card != this && CardTraits.CanApplyPale(card),
            this);

        foreach (CardModel card in selectedCards)
        {
            CardTraits.ApplyPale(card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
        DynamicVars["Draw"].UpgradeValueBy(UpgradeDrawBonus);
    }
}
