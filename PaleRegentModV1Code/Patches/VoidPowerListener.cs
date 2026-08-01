using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
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

    /// <summary>
    /// 记录“某张牌本次打出实际支付了多少虚空”。//20260801
    ///
    /// 为什么需要它：
    /// 【失心】把“灵魂X”的牌转成“0灵魂 / X虚空”之后，卡牌效果里的 X
    /// 不能再取原版的 <c>CardEnergyCost.CapturedXValue</c>——那是灵魂侧捕获的值，
    /// 失心后灵魂支付量恒为 0，拿到的 X 也就不对了
    /// （这就是“实际打出只有 2 点效果”那类现象的根因）。
    /// 真正的 X 应该是 RitsuLib 在虚空侧实际扣掉的量，也就是本回调的
    /// <c>context.Amount</c>。存进 CardTraits 后，由
    /// Patches/LostEnergyCostPatch 在 CardModel.ResolveEnergyXValue 的 Postfix 中
    /// 把 X 改写为这个值。
    ///
    /// 说明：
    /// 1. 只处理虚空资源，其它 mod 的次级资源直接放过。
    /// 2. context.Card 为 null 的情况（遗物/药水等非卡牌消耗）不需要记录。
    /// 3. CardTraits.RecordVoidSpent 内部只对已建立特质数据的牌写入，
    ///    不会给普通牌白白创建状态对象。
    /// 4. 本回调不做任何异步动作，图标同步仍由
    ///    AfterSecondaryResourceChanged 负责，职责不重叠。
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

    /// <summary>
    /// 【苍白】牌的虚空费最终裁决：永远为 0。//20260801
    ///
    /// 为什么需要这个钩子（问题：虚空X 的牌无法被加上苍白）：
    /// 虚空 X 费的牌（虚空必杀 / 回溯 / 虚空实验）把 X 费写在 **构造器** 里
    /// （<c>CardTraits.SetVoidCostX</c>），在 RitsuLib 里属于 Permanent 层；
    /// RitsuLib 会在卡牌降级/克隆/重建实例时用 <c>ResetPermanentLayersFrom</c>
    /// 把 canonical 的 Permanent 层重新灌回来。于是苍白里那一句
    /// <c>SecondaryCosts().Clear(...)</c> 会被回灌推翻，虚空 X 费又回来了。
    ///
    /// 本钩子是“双保险”的第二道：在 RitsuLib 常规费用修正阶段之后（Late），
    /// 只要这张牌带【苍白】，就把虚空费直接裁决为 0；
    /// 同时调用 <c>EnforcePaleVoidCostCleared</c> 把被回灌的费用条目真正清除，
    /// 以便卡面（由 NSecondaryResourceCardCostUi 自动读 SecondaryCosts 渲染）
    /// 也同步不再显示虚空费。
    ///
    /// 这样无论回灌发生多少次，“苍白 = 不消耗虚空”都成立。
    ///
    /// 备忘：Modify* 系列钩子是 **同步** 方法，返回 decimal（不是 Task）。
    /// </summary>
    public decimal ModifySecondaryResourceCostLate(SecondaryResourceCostContext context, decimal cost)
    {
        // 只处理虚空资源，其它 mod 的次级资源原样放过
        if (context.Definition.Id != VoidResource.Id)
        {
            return cost;
        }

        if (context.Card == null || !CardTraits.IsPale(context.Card))
        {
            return cost;
        }

        // 把被 Permanent 层回灌的虚空费条目真正移除（修卡面显示）
        CardTraits.EnforcePaleVoidCostCleared(context.Card);
        // 并且本次结算不论如何都不扣虚空（修实际消耗）
        return 0m;
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
