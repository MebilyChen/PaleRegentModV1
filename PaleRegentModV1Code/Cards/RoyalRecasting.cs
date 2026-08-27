using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【模具实验】技能牌。
/// 0 灵魂 + X 虚空：
/// 为你随机生成【国王俑卫】或【有翼俑卫】，共 X 张。
/// 选择手牌中的 X 张牌，为其施加【纯粹】。
///
/// 升级后：
/// 为你随机生成【国王俑卫】或【有翼俑卫】，共 X+1 张。
/// 选择手牌中的 X+1 张牌，为其施加【纯粹】。
/// </summary>
public class RoyalRecasting : PaleRegentModV1Card
{
    public RoyalRecasting() : base(
        0,
        CardType.Skill,
        CardRarity.Uncommon,
        TargetType.Self)
    {
        CardTraits.SetVoidCostX(this);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<KingsRetainer>(false),
        HoverTipFactory.FromCard<WingedRetainerCard>(false),ModHoverTips.Pure
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        /*
         * ============================================================
         * 读取本次实际支付的虚空 X
         * 与【虚空实验】一致
         * ============================================================
         */

        int x = 0;
        if (cardPlay.TryGetSecondaryResources(
                out SecondaryResourcePlayLedger ledger))
        {
            x = ledger.Spent(VoidResource.Id);
        }

        await VoidResource.SyncPower(
            choiceContext,
            cardPlay.Player,
            this);

        // 普通版 X 张，升级版 X+1 张。
        int count = x + (IsUpgraded ? 1 : 0);

        /*
         * ============================================================
         * 随机生成【国王俑卫】或【有翼俑卫】
         * ============================================================
         */

        // 0 = 国王俑卫
        // 1 = 有翼俑卫
        List<int> retainerTypes = [0, 1];

        for (int i = 0; i < count; i++)
        {
            // 使用官方战斗随机数流。
            int retainerType =
                Owner.RunState.Rng.CombatTargets.NextItem(retainerTypes);

            CardModel generated;

            if (retainerType == 0)
            {
                generated =
                    Owner.Creature.CombatState
                        .CreateCard<KingsRetainer>(Owner);
            }
            else
            {
                generated =
                    Owner.Creature.CombatState
                        .CreateCard<WingedRetainerCard>(Owner);
            }

            // 与【有翼卫群】完全相同的生成牌流程。
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(
                    generated,
                    PileType.Hand,
                    Owner,
                    (CardPilePosition)1),
                2.2f,
                (CardPreviewStyle)1);
        }

        /*
         * ============================================================
         * 选择手牌中的 count 张牌，为其施加【纯粹】
         * ============================================================
         */

        CardPile hand =
            PileTypeExtensions.GetPile(
                PileType.Hand,
                Owner);

        List<CardModel> eligibleCards =
            hand.Cards
                .Where(card =>
                    card != this &&
                    CardTraits.CanApplyPure(card))
                .ToList();

        if (eligibleCards.Count == 0)
        {
            return;
        }

        // 若合法目标不足 count 张，则选择所有剩余合法牌，
        // 避免要求选择一个不可能达到的固定数量。
        int requiredCount =
            System.Math.Min(count, eligibleCards.Count);

        if (requiredCount <= 0)
        {
            return;
        }

        List<CardModel> selected =
            (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(
                    SelectionScreenPrompt,
                    requiredCount,
                    requiredCount),
                card =>
                    card != this &&
                    CardTraits.CanApplyPure(card),
                this))
            .ToList();

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyPure(card);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级不改变费用。
        // OnPlay 中将效果数量从 X 提升为 X+1。
    }
}
