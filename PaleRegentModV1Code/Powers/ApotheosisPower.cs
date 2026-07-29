using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using MegaCrit.Sts2.Core.Entities.Players;


namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【化神】buff（由卡牌【化神】施加，持续到战斗结束）。
/// 效果：你的每个回合开始时，获得 [层数] 点虚空，然后选择 [层数] 张手牌附加【失心】。
/// 层数由卡牌施加：基础 1（虚空+1，选 1 张），升级 2（虚空+2，选 2 张）。
/// 20260725 批次：选牌张数与层数联动（表格 H10/Q10：升级后选 2 张）。
///
/// 说明：
/// - StackType = Single：不叠层，重复打【化神】不会翻倍触发。
/// - 挂点用 BeforeSideTurnStart（带 PlayerChoiceContext 的版本），
///   因为"选择一张手牌"需要弹选牌界面，必须有 choiceContext 才能等待玩家操作；
///   AfterSideTurnStart 没有 choiceContext 参数，无法做交互。
///   注意：BeforeSideTurnStart 在抽牌之前触发，所以选的是"上回合留下的手牌"
///   （保留牌等）；如果希望在抽完牌后选，需要换成别的钩子（后续可调）。
///
/// 修改指南：
/// - 每回合虚空获得量 = Amount（在卡牌 Apotheosis 的 PowerVar 里改）。
/// - 选牌提示文案：powers.json 里对应的描述 + cards.json 的 selectionScreenPrompt
///   （Power 没有 SelectionScreenPrompt 属性，这里直接用本地化字符串）。
/// </summary>
public class ApotheosisPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    /// 玩家回合开始：能量恢复并完成抽牌后触发。
    /// 此时可以选择本回合刚抽到的手牌。
    /// </summary>
    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        // 只处理该 Power 持有者自己的回合
        if (player != Owner.Player)
        {
            return;
        }

        Flash();

        // 1. 获得虚空并同步展示层
        await VoidResource.Gain(player, Amount);
        await VoidResource.SyncPower(choiceContext, player, null);

        // 2. 获取当前手牌中可以附加【失心】的牌
        List<CardModel> selectableCards = CardPile
            .GetCards(player, PileType.Hand)
            .Where(CardTraits.CanApplyLost)
            .ToList();

        if (selectableCards.Count == 0)
        {
            return;
        }

        // 避免升级后要求选 2 张，但手中实际只有 1 张可选，导致选牌无法完成
        int selectCount = System.Math.Min(
            (int)Amount,
            selectableCards.Count);

        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(SelectCardPrompt, selectCount),
            CardTraits.CanApplyLost,
            this);

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyLost(card);
        }
    }


    /// <summary>
    /// 选牌界面顶部的提示文案。
    /// PowerModel 没有 CardModel 那个自动拼 key 的 SelectionScreenPrompt 属性，
    /// 所以这里手动构造 LocString，文案写在 powers.json 的
    /// PALEREGENTMODV1-APOTHEOSIS_POWER.selectionScreenPrompt 条目里。
    /// </summary>
    private static LocString SelectCardPrompt =>
        new LocString("powers", "PALEREGENTMODV1-APOTHEOSIS_POWER.selectionScreenPrompt");
}
