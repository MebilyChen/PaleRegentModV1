using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Relics;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 虚空资源全局监听器。
/// 统一追踪虚空数量变动、维护本回合虚空获得统计，并在非虚空之心阶段同步 VoidPower 图标。
/// </summary>
internal sealed class VoidPowerListener : ISecondaryResourceHookListener
{
    public static VoidPowerListener Instance { get; } = new VoidPowerListener();

    /// <summary>
    /// 本回合获得的虚空总量（只统计正增量）。
    /// 供【异色 OffColor】读取段数；每个玩家回合开始时由
    /// PaleToken.AfterEnergyReset 调用 ResetTurnGain() 清零。
    /// </summary>
    public static int VoidGainedThisTurn { get; private set; }

    /// <summary>清零本回合虚空获得计数（玩家回合开始时调用）。</summary>
    public static void ResetTurnGain()
    {
        VoidGainedThisTurn = 0;
    }

    public static void Init()
    {
        SecondaryResourceHook.RegisterGlobalListener(Instance);
    }

    public async Task AfterSecondaryResourceChanged(SecondaryResourceChangeContext context)
    {
        // 只处理虚空资源，其它 mod 的次级资源直接放过。
        if (context.Definition.Id != VoidResource.Id)
        {
            return;
        }

        Player? player = context.Player;
        if (player?.Creature == null)
        {
            return;
        }

        // 数值没有实际变化就不同步，避免无意义的命令。
        if (context.NewAmount == context.OldAmount)
        {
            return;
        }

        // 累计本回合获得的虚空（只计正增量，花费/损失不扣回）。
        if (context.NewAmount > context.OldAmount)
        {
            int gained = (int)(context.NewAmount - context.OldAmount);
            VoidGainedThisTurn += gained;
            CombatCounters.NotifyVoidGain(gained);
        }

        // 虚空之心代表终局形态：虚空资源继续存在，但不再用 VoidPower 图标展示。
        if (player.Relics.Any(relic => relic is VoidHeart))
        {
            return;
        }

        await VoidResource.SyncPower(new ThrowingPlayerChoiceContext(), player, null);
    }

    /// <summary>
    /// 记录某张牌本次打出实际支付的虚空，供失心后的 X 费结算读取。
    /// </summary>
    public Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        if (context.Definition.Id != VoidResource.Id)
        {
            return Task.CompletedTask;
        }

        if (context.Card == null)
        {
            return Task.CompletedTask;
        }

        CardTraits.RecordVoidSpent(context.Card, context.Amount);
        return Task.CompletedTask;
    }

    public async Task AfterSecondaryResourceReset(SecondaryResourceChangeContext context)
    {
        // 资源被内建策略重置（如战斗结束/回合开始策略）时也同步一次，保证图标清零。
        if (context.Definition.Id != VoidResource.Id)
        {
            return;
        }

        Player? player = context.Player;
        if (player?.Creature == null)
        {
            return;
        }

        // 虚空资源按战斗重置，此时同步清零战斗级计数器。
        CombatCounters.ResetCombat();

        // 虚空之心阶段不显示 VoidPower 图标。
        if (player.Relics.Any(relic => relic is VoidHeart))
        {
            return;
        }

        await VoidResource.SyncPower(new ThrowingPlayerChoiceContext(), player, null);
    }
}
