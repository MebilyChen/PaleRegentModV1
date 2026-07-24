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
/// 【灵魂格挡】普通技能牌（带回收的防御）。
/// 1 灵魂：获得 7 点格挡，将 1 张牌从你的弃牌堆放回手牌。
/// 升级后：获得 10 点格挡。
///
/// 修改指南：
/// - 格挡：BaseBlock / UpgradeBlockBonus 常量。
/// - 回收张数：RetrieveCount 常量；选牌提示文案 cards.json 的 selectionScreenPrompt。
/// </summary>
public class SoulBlock() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    /// <summary>基础格挡。</summary>
    private const int BaseBlock = 7;
    /// <summary>升级后格挡增加量（7→10，表格设计）。</summary>
    private const int UpgradeBlockBonus = 3;
    /// <summary>从弃牌堆放回手牌的张数。</summary>
    private const int RetrieveCount = 1;

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 从弃牌堆选 1 张牌放回手牌（写法参考 modstudy HornetAscendantGrip）
        CardPile discard = PileTypeExtensions.GetPile(PileType.Discard, Owner);
        if (discard.Cards.Any())
        {
            List<CardModel> selected = (await CardSelectCmd.FromCombatPile(
                choiceContext, discard, Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, RetrieveCount),
                (Func<CardModel, bool>)((CardModel _) => true))).ToList();
            foreach (CardModel c in selected)
            {
                await CardPileCmd.Add(c, PileType.Hand, CardPilePosition.Top, null, false);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
