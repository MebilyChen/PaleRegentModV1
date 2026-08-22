using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

public class AtavismPower : PaleRegentModV1Power, ISecondaryResourceHookListener
{
    // 上一次因“获得虚空”触发失心选择的战斗回合编号。
    // 不依赖回合开始 Hook，因此兼容当前项目 API。
    private int _lastVoidTriggerRound = int.MinValue;

    // 记录正在结算的卡牌：同一张牌即使同时支付普通能量与虚空，也只触发一次。
    private readonly HashSet<CardModel> _triggeredPaymentCards = [];

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // 每次实际支付普通能量（即“灵魂”）时触发一次。
    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (card.Owner != Owner.Player || amount <= 0)
        {
            return;
        }

        await TriggerForCardPayment(card);
    }

    // 每次实际支付虚空时触发一次；若为同一张已因普通能量支付触发的牌，则跳过。
    public async Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        if (context.Player != Owner.Player ||
            context.Definition.Id != VoidResource.Id)
        {
            return;
        }

        if (context.Card is { } card)
        {
            await TriggerForCardPayment(card);
            return;
        }

        // 非卡牌来源的虚空支付没有可用于合并的卡牌上下文，仍按一次支付触发一次。
        Flash();
        await PlayerCmd.GainEnergy(Amount, context.Player);
    }

    // 卡牌完整结算后移除去重标记，使该卡牌下次被打出时可再次触发。
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player == Owner.Player)
        {
            _triggeredPaymentCards.Remove(cardPlay.Card);
        }

        return Task.CompletedTask;
    }

    private async Task TriggerForCardPayment(CardModel card)
    {
        if (!_triggeredPaymentCards.Add(card))
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(Amount, card.Owner);
    }

    public Task AfterSecondaryResourceChanged(SecondaryResourceChangeContext context)
    {
        if (context.Player != Owner.Player ||
            context.Definition.Id != VoidResource.Id ||
            context.Reason != SecondaryResourceChangeReason.Gain ||
            context.Delta <= 0)
        {
            return Task.CompletedTask;
        }

        int currentRound = context.CombatState.RoundNumber;
        if (_lastVoidTriggerRound == currentRound)
        {
            return Task.CompletedTask;
        }

        // 本回合首次实际获得虚空：立即记录，避免同一回合后续获得虚空重复触发。
        _lastVoidTriggerRound = currentRound;
        Flash();

        // 资源 Hook 内不能等待选牌完成；只投递独立 Hook 动作，随后立即让原结算继续。
        _ = TaskHelper.RunSafely(ApplyLostInSeparateHook(context));

        return Task.CompletedTask;
    }

    private async Task ApplyLostInSeparateHook(SecondaryResourceChangeContext context)
    {
        var localNetId = LocalContext.NetId;
        if (!localNetId.HasValue)
        {
            return;
        }

        var choiceContext = new HookPlayerChoiceContext(
            this,
            localNetId.Value,
            context.CombatState,
            GameActionType.Combat);

        Task selectionTask = ApplyLostToHand(choiceContext, context.Player);
        bool completed = await choiceContext.AssignTaskAndWaitForPauseOrCompletion(selectionTask);

        if (!completed && choiceContext.GameAction is not null)
        {
            await choiceContext.GameAction.CompletionTask;
        }
    }

    private async Task ApplyLostToHand(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, Amount),
            CardTraits.CanApplyLost,
            this);

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyLost(card);
        }
    }
}
