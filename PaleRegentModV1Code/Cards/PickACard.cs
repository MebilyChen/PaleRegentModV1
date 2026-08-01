using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【点选卡牌】技能牌（表 C#94，0727 新增）。
/// 1 灵魂：获得 5 点格挡，从抽牌堆中选择 1 张牌加入手牌。
/// 升级后：8 点格挡，选择 2 张。
/// </summary>
public class PickACard() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseBlock = 5;
    private const int UpgradeBlockBonus = 3;

    private int _seekCount = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];
    // 声明"这张牌提供格挡"，游戏会据此显示格挡预览等 UI
    public override bool GainsBlock => true;

    // 带 Defend 标签：与"对防御牌生效"的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 从抽牌堆检索牌入手
        CardPile draw = PileTypeExtensions.GetPile(PileType.Draw, Owner);
        if (!draw.Cards.Any())
        {
            return;
        }

        List<CardModel> selected = (await CardSelectCmd.FromCombatPile(
            choiceContext, draw, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, _seekCount),
            (Func<CardModel, bool>)((CardModel _) => true))).ToList();

        foreach (CardModel c in selected)
        {
            await CardPileCmd.Add(c, PileType.Hand, CardPilePosition.Top, null, false);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
        _seekCount = 2;
    }
}
