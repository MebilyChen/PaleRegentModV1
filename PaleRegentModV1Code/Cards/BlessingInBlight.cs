using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【祸中取福】技能牌。
/// 获得格挡；战斗中全牌堆每有 1 张诅咒，恢复对应生命。
/// </summary>
public class BlessingInBlight() : PaleRegentModV1Card(
    1,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseBlock = 7;
    private const int UpgradeBlockBonus = 3;

    /// <summary>每张诅咒牌恢复的生命（升级后 2）。</summary>
    private int _healPerCurse = 1;

    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(BaseBlock, ValueProp.Move),
        new CurrentHealAmountVar()
    ];

    /// <summary>
    /// 统计当前战斗抽牌堆、手牌、弃牌堆与消耗堆中的全部诅咒牌。
    /// 非战斗界面（牌组、奖励、事件等）没有 combat pile，因此返回 0。
    /// </summary>
    private static int GetCurseCount(CardModel? card)
    {
        if (card == null || !card.IsMutable || card.Owner == null)
        {
            return 0;
        }

        try
        {
            return CardPile
                .GetCards(card.Owner, PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust)
                .Count(c => c.Type == CardType.Curse);
        }
        catch (InvalidOperationException)
        {
            // DynamicVar 在牌组/奖励/事件等非战斗界面也会刷新。
            // 此时访问 Draw/Hand/Discard/Exhaust 会抛 “Tried to get Draw pile while out of combat”。
            // 非战斗预览回退为 0；战斗内仍实时读取四个 combat pile。
            return 0;
        }
    }

    /// <summary>
    /// 牌面与实际结算共同使用的最终治疗量。
    /// 战斗中随四个 combat pile 的诅咒数量实时变化；
    /// 非战斗预览显示 0，且不会中断卡牌网格渲染。
    /// </summary>
    private static int GetCurrentHealAmount(CardModel? card)
    {
        if (card is not BlessingInBlight blessingInBlight || !blessingInBlight.IsMutable)
        {
            return 0;
        }

        return GetCurseCount(blessingInBlight) * blessingInBlight._healPerCurse;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 与牌面 {Amount} 使用同一个计算公式。
        int healAmount = GetCurrentHealAmount(this);
        if (healAmount > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, healAmount);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus); // 7 -> 10
        _healPerCurse = 2;                                      // 1 -> 2
    }

    /// <summary>
    /// 为本地化中的 {Amount} 提供当前最终治疗量。
    /// </summary>
    private sealed class CurrentHealAmountVar : DynamicVar
    {
        public CurrentHealAmountVar() : base("Amount", 0m)
        {
        }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = GetCurrentHealAmount(card);
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return GetCurrentHealAmount(_owner as CardModel);
        }

        public override string ToString()
        {
            return GetCurrentHealAmount(_owner as CardModel).ToString();
        }
    }
}