using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 虚空资源全局监听器（修复：花费虚空后 VoidPower 图标不同步移除）。
///
/// 问题根因：
/// 之前 VoidPower 的同步完全依赖各张卡在 OnPlay 里手动调用
/// VoidResource.SyncPower(...)。但对于"固定虚空费"的卡（通过
/// card.SecondaryCosts().Set(...) 声明费用），支付是 RitsuLib 在出牌
/// 结算时自动完成的，不会经过我们的 OnPlay 代码——
/// 一部分卡忘了调、药水/遗物等其它消耗途径也调不到，导致图标层数滞留。
///
/// 修复方案（参考 ）：
/// 实现 ISecondaryResourceHookListener 并注册为进程级监听器。
/// RitsuLib 在虚空资源发生任何数量变化（Gain/Spend/Lose/Set/Reset）后
/// 都会回调 AfterSecondaryResourceChanged，我们在这里统一把 VoidPower
/// 层数同步为最新资源值。各张卡里原有的手动 SyncPower 调用是幂等的，
/// 保留也不会重复叠加，作为兜底。
///
/// 备忘：
/// 1. ISecondaryResourceHookListener 的全部方法都有默认实现（C#8 DIM，
///    已用 DLL 元数据核实），所以只需实现我们关心的两个回调。
/// 2. 监听器上下文里没有 PlayerChoiceContext，参考 SilkPowerListener
///    用 new ThrowingPlayerChoiceContext()。
/// 3. 必须在 MainFile.Initialize 里调用 Init() 注册。
/// </summary>
internal sealed class VoidPowerListener : ISecondaryResourceHookListener
{
    public static VoidPowerListener Instance { get; } = new VoidPowerListener();

    /// <summary>
    /// 本回合获得的虚空总量（只统计正增量）。
    /// 供【异色 OffColor】读取段数；每个玩家回合开始时由
    /// PaleToken.AfterEnergyReset 调用 ResetTurnGain() 清零。
    /// 20260725 批次新增（表格卡牌 C#10 异色）。
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
        // 只处理虚空资源，其它 mod 的次级资源直接放过
        if (context.Definition.Id != VoidResource.Id)
        {
            return;
        }

        Player? player = context.Player;
        if (player?.Creature == null)
        {
            return;
        }

        // 数值没有实际变化就不同步，避免无意义的命令
        if (context.NewAmount == context.OldAmount)
        {
            return;
        }

        // 累计本回合获得的虚空（只计正增量，花费/损失不扣回）
        if (context.NewAmount > context.OldAmount)
        {
            int gained = (int)(context.NewAmount - context.OldAmount);
            VoidGainedThisTurn += gained;
            // 20260727 批次：战斗级统计埋点（灵魂双刃 C#57 / 虚空回声 C#64 / 共鸣一击 C#63）
            CombatCounters.NotifyVoidGain(gained);
        }

        await VoidResource.SyncPower(new ThrowingPlayerChoiceContext(), player, null);
    }

    public async Task AfterSecondaryResourceReset(SecondaryResourceChangeContext context)
    {
        // 资源被内建策略重置（如战斗结束/回合开始策略）时也同步一次，保证图标清零
        if (context.Definition.Id != VoidResource.Id)
        {
            return;
        }

        Player? player = context.Player;
        if (player?.Creature == null)
        {
            return;
        }

        // 20260727 批次：虚空资源按战斗重置，此时同步清零战斗级计数器
        CombatCounters.ResetCombat();

        await VoidResource.SyncPower(new ThrowingPlayerChoiceContext(), player, null);
    }
}
