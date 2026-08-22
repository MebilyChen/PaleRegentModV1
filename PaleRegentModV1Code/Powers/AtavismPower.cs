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
using PaleRegentModV1.PaleRegentModV1Code.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

public class AtavismPower : PaleRegentModV1Power, ISecondaryResourceHookListener
{
    // 上一次因“获得虚空”触发失心选择的战斗回合编号。
    // 不依赖回合开始 Hook，因此兼容当前项目 API。
    private int _lastVoidTriggerRound = int.MinValue;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 不是本 Power 所属玩家打出的卡，不触发。
        if (cardPlay.Player != Owner.Player)
        {
            return;
        }

        // 打出【返祖】本身不回复灵魂。
        // 这样首次打出返祖时，刚施加的返祖 Power 不会立刻回 1 灵魂；
        // 之后打出任意其他卡牌时仍会正常回复。
        if (cardPlay.Card is Atavism)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(Amount, cardPlay.Player);
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
