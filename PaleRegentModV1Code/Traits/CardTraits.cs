using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Utils;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 【失心】【苍白】【纯粹】自定义卡牌特质的实现。
///
/// ============ 机制说明（对应设计文档，20260729 需求变更） ============
/// 【失心】（Lost）：
///   - 取消这张牌的灵魂耗能，把灵魂费 1:1 转换并入虚空费。
///     例：1灵魂0虚空 → 0灵魂1虚空；2灵魂2虚空 → 0灵魂4虚空。
///   - 对 X 费牌无效（X 费牌无法附加失心）。
///   - 自动获得【重放1】（BaseReplayCount = 1，打出后额外重放一次）。
///   - 与【苍白】互斥：附加失心会移除苍白。
/// 【苍白】（Pale）：（20260729 变更：不再是"同时添加消耗和虚无"）
///   1. 取消【失心】，以及受失心影响而添加的"灵魂并入虚空消费"和【消耗】。
///      注意：如果这张牌没有失心就有【消耗】（自带/其他来源），不移除消耗。
///   2. 取消卡牌的虚空消耗（移除虚空费条目）。
///   3. 添加【虚无】。
/// 【纯粹】（Pure）：（20260729 变更：从"免疫感染转化"扩展为通用规则）
///   - 战斗中无法被变化，例如被变化为其他牌。
///     （免疫"感染"转化实现为不会被随机选中，见 Infection.OnTurnEndInHand 的过滤；
///      通用"无法被变化"由 Patches/PureTransformGuard 拦截 CardCmd.Transform 实现。）
///
/// ============ 实现思路（给未来的你/维护者） ============
/// STS2 原版的 CardKeyword 是枚举，mod 无法往里加新枚举值，
/// 所以"失心/苍白"不是真正的 CardKeyword，而是：
///   1. 用 RitsuLib 的 AttachedState 给"卡牌实例"挂一份附加数据（TraitState），
///      记录：是否失心 / 是否苍白 / 附加前的原始灵魂费和虚空费（用于还原）。
///      AttachedState 类似 C# 的 ConditionalWeakTable，数据跟着卡牌实例走，
///      卡牌实例销毁后数据自动回收，不会内存泄漏。
///   2. 费用修改：
///      - 灵魂费用 CardModel.EnergyCost.SetCustomBaseCost(int)（永久基础费）。
///      - 虚空费用 RitsuLib 的 card.SecondaryCosts().Set(VoidResource.Id, n)。
///   3. 关键词（重放/虚无/消耗）走原版 API：BaseReplayCount、AddKeyword/RemoveKeyword。
///   4. 牌面显示与 HoverTips（20260729 新增）：
///      失心/苍白会以"[gold]失心[/gold]。/[gold]苍白[/gold]。"的形式显示在牌面描述里，
///      并在悬停词条中追加失心/苍白词条（哪怕消耗/虚无/重放本身也会显示）。
///      实现见 Patches/TraitCardTextPatch（Harmony patch CardModel.GetDescriptionForPile
///      与 CardModel.HoverTips getter）。
///
/// ============ 修改指南 ============
/// - 想改"失心的灵魂→虚空换算比例"：改 ApplyLost 里的 newVoidCost 计算。
/// - 想改"失心送的重放层数"：改 LostReplayCount 常量。
/// - 想改牌面显示文字/词条：见 Patches/TraitCardTextPatch 与 static_hover_tips.json。
/// </summary>
public static class CardTraits
{
    /// <summary>失心自动附带的重放层数（重放1）。</summary>
    private const int LostReplayCount = 1;

    /// <summary>挂在卡牌实例上的特质数据。</summary>
    private sealed class TraitState
    {
        public bool IsLost;              // 是否有【失心】
        public bool IsPale;              // 是否有【苍白】
        public bool IsPureApplied;       // 是否被战斗中附加了【纯粹】（灵魂护佑等）
        public int OriginalEnergyCost;   // 附加特质前的灵魂费（用于苍白还原）
        public int OriginalVoidCost;     // 附加特质前的虚空费（用于还原）
        public bool OriginalHasVoidCost; // 附加特质前是否登记过虚空费条目（区分“虚空0”与“无虚空费”）//20260726
        public bool OriginalVoidCostsX;  // 附加特质前虚空费是否为 X 费（苍白还原时保留 X 属性）//20260726
        public bool CostSnapshotTaken;   // 是否已经记录过原始费用快照
        public bool ExhaustAddedByLost;  // 【消耗】是否是失心流程加上去的（20260729：苍白只移除失心加的消耗）
    }

    /// <summary>卡牌实例 → 特质数据 的弱引用表。</summary>
    private static readonly AttachedState<CardModel, TraitState> States =
        new(() => new TraitState());

    // ---------------- 查询 ----------------

    /// <summary>这张牌当前是否有【失心】。</summary>
    public static bool IsLost(CardModel card) =>
        States.TryGetValue(card, out TraitState? s) && s!.IsLost;

    /// <summary>这张牌当前是否有【苍白】。</summary>
    public static bool IsPale(CardModel card) =>
        States.TryGetValue(card, out TraitState? s) && s!.IsPale;

    /// <summary>
    /// 这张牌当前是否有【纯粹】（机制文档：名词表·纯粹）。
    /// 两个来源：1) 卡牌类自带（PaleRegentModV1Card.IsPure 重写为 true，如虚空化形/化神/纯粹容器）；
    ///          2) 战斗中被附加（灵魂护佑等，走 ApplyPure 的 TraitState）。
    /// 判定用途（20260729 变更）：带纯粹的牌"战斗中无法被变化"——
    ///   1) 感染的随机转化不会选中它（Infection.OnTurnEndInHand 过滤）；
    ///   2) 任何 CardCmd.Transform / TransformTo / TransformToRandom 都无法把它变为其他牌
    ///      （Patches/PureTransformGuard 全局拦截）。
    /// </summary>
    public static bool IsPure(CardModel card) =>
        (card as Cards.PaleRegentModV1Card)?.IsPure == true ||
        (States.TryGetValue(card, out TraitState? s) && s!.IsPureApplied);

    /// <summary>给一张牌附加【纯粹】（战斗内，跟随卡牌实例）。</summary>
    public static void ApplyPure(CardModel card)
    {
        States.GetOrCreate(card).IsPureApplied = true;
    }

    /// <summary>读取这张牌当前的虚空费（没有则为 0；X 费返回 0，请另行判断 CostsX）。</summary>
    public static int GetVoidCost(CardModel card)
    {
        SecondaryResourceCost? cost = card.SecondaryCosts().Get(VoidResource.Id);
        if (cost == null || cost.CostsX) return 0;
        return cost.Amount;
    }

    /// <summary>
    /// 这张牌是否“登记过虚空费条目”（即卡面上会显示虚空费，哪怕是 0 或 X）。//20260726
    /// 用于区分两类牌：
    ///   - 普通牌：从未登记虚空费，SecondaryCosts().Get 返回 null，卡面无虚空费显示；
    ///   - 虚空牌：登记过虚空费（含虚空 0 / 虚空 X），卡面显示虚空费。
    /// 规则“虚空费大于等于 0 的卡牌自动获得【消耗】”中的“虚空费≥ 0”
    /// 就是指本方法返回 true 的牌（登记过就算，无虚空费条目的普通牌不算）。
    /// </summary>
    public static bool HasVoidCost(CardModel card) =>
        card.SecondaryCosts().Get(VoidResource.Id) != null;

    /// <summary>这张牌能否附加【失心】：X 费牌（灵魂X或虚空X）无效。</summary>
    public static bool CanApplyLost(CardModel card)
    {
        if (card.EnergyCost.CostsX) return false;
        SecondaryResourceCost? vc = card.SecondaryCosts().Get(VoidResource.Id);
        if (vc != null && vc.CostsX) return false;
        return true;
    }

    // ---------------- 附加/移除 ----------------

    /// <summary>
    /// 给一张牌附加【失心】。
    /// 效果：灵魂费清零并 1:1 并入虚空费；获得重放1；取消苍白。
    /// 返回 false 表示这张牌不能附加（X 费牌）。
    /// </summary>
    public static bool ApplyLost(CardModel card)
    {
        // 防御：canonical（规范/不可变）实例禁止修改费用/关键词，否则游戏启动时
        // ModelDb 注册卡牌会抛 CanonicalModelException 直接崩溃（20260728 启动崩溃修复）。
        // "自带失心"请勿在构造器里调用本方法，改在 PaleRegentModV1Card.HasInnateLost 声明。
        if (card.IsCanonical) return false;
        if (!CanApplyLost(card)) return false;

        TraitState s = States.GetOrCreate(card);
        if (s.IsLost) return true; // 已经失心，无需重复处理

        // 若带苍白，先按"取消苍白"还原（苍白会清虚空费，需要先恢复）
        if (s.IsPale) RemovePale(card, s);

        EnsureCostSnapshot(card, s);

        // 换算：新虚空费 = 原虚空费 + 原灵魂费（1:1 转换）
        int currentEnergy = card.EnergyCost.GetWithModifiers(CostModifiers.None);
        int currentVoid = GetVoidCost(card);
        int newVoidCost = currentVoid + currentEnergy;

        card.EnergyCost.SetCustomBaseCost(0);                       // 灵魂费清零
        card.SecondaryCosts().Set(VoidResource.Id, newVoidCost);    // 并入虚空费（即使 0 也登记条目，成为虚空牌）//20260726
        card.BaseReplayCount = Math.Max(card.BaseReplayCount, LostReplayCount); // 重放1

        s.IsLost = true;
        SyncExhaustKeyword(card, s); // 虚空费≥0（登记过虚空费条目）自动获得【消耗】 //20260726
        return true;
    }

    /// <summary>
    /// 给一张牌附加【苍白】。（20260729 需求变更）
    /// 效果：
    ///   1. 取消【失心】，及受其影响而添加的"灵魂并入虚空消费"和【消耗】；
    ///      如果这张牌没有失心就有【消耗】（牌自带/其他来源），不移除消耗。
    ///   2. 取消卡牌的虚空消耗（移除虚空费条目）。
    ///   3. 添加【虚无】。
    /// </summary>
    public static void ApplyPale(CardModel card)
    {
        // 防御：同 ApplyLost，canonical 实例不可修改（20260728 启动崩溃修复）
        if (card.IsCanonical) return;
        TraitState s = States.GetOrCreate(card);
        if (s.IsPale) return;

        EnsureCostSnapshot(card, s);

        // 1) 取消失心：恢复原灵魂费、收回重放，并移除"失心加上去的消耗"
        if (s.IsLost)
        {
            card.EnergyCost.SetCustomBaseCost(s.OriginalEnergyCost);
            card.BaseReplayCount = 0; // 收回失心送的重放
            s.IsLost = false;
        }
        // 只移除"失心流程加上去的消耗"；牌本来就有的消耗保持不动（20260729 变更）
        // 注：即使这张牌当前没有失心（例如失心曾被其他途径取消），只要消耗是失心加的也一并清理。
        if (s.ExhaustAddedByLost)
        {
            card.RemoveKeyword(CardKeyword.Exhaust);
            s.ExhaustAddedByLost = false;
        }

        // 2) 取消卡牌的虚空消耗
        // 注意：这里用 Clear 而不是 Set(0)，把虚空费条目整个移除，
        // 卡面不再显示虚空费 → 不再命中“虚空费≥0 自动消耗”规则 //20260726
        card.SecondaryCosts().Clear(VoidResource.Id);

        // 3) 添加【虚无】
        card.AddKeyword(CardKeyword.Ethereal);

        s.IsPale = true;
        // 20260729 变更：苍白不再"顺带添加消耗"，也不做旧版 SyncExhaustKeyword 的
        // 保守增删——消耗的移除已在上面按 ExhaustAddedByLost 精确处理。
    }

    /// <summary>内部：移除苍白状态（供 ApplyLost 里互斥切换时调用）。</summary>
    private static void RemovePale(CardModel card, TraitState s)
    {
        card.RemoveKeyword(CardKeyword.Ethereal);
        // 恢复原虚空费条目（苍白 Clear 掉的部分）：
        // 只要原本登记过虚空费条目就恢复（含虚空 0 / 虚空 X），保持“虚空牌”身份 //20260726
        if (s.OriginalHasVoidCost)
        {
            if (s.OriginalVoidCostsX)
                card.SecondaryCosts().Set(VoidResource.Id, SecondaryResourceCost.X(1)); // 还原 X 费 //20260726
            else
                card.SecondaryCosts().Set(VoidResource.Id, s.OriginalVoidCost);
            SyncExhaustKeyword(card, s); // 重新成为虚空牌 → 自动【消耗】
        }
        s.IsPale = false;
    }

    // ---------------- 内部工具 ----------------

    /// <summary>第一次修改费用前，把原始费用记下来（用于日后还原）。</summary>
    private static void EnsureCostSnapshot(CardModel card, TraitState s)
    {
        if (s.CostSnapshotTaken) return;
        s.OriginalEnergyCost = card.EnergyCost.GetWithModifiers(CostModifiers.None);
        s.OriginalVoidCost = GetVoidCost(card);
        s.OriginalHasVoidCost = HasVoidCost(card); //20260726 记录原本是否登记过虚空费条目
        SecondaryResourceCost? vc = card.SecondaryCosts().Get(VoidResource.Id);
        s.OriginalVoidCostsX = vc != null && vc.CostsX; //20260726 记录原虚空费是否为 X 费
        s.CostSnapshotTaken = true;
    }

    /// <summary>
    /// 规则："虚空费大于等于 0 的卡牌自动获得【消耗】。" //20260726 规则由“大于0”改为“大于等于0”
    /// 这里的“虚空费 ≥ 0”指的是“卡面上登记/显示了虚空费条目”的牌（HasVoidCost），
    /// 即：虚空 0、虚空 X 也算虚空牌，自动带【消耗】；
    /// 而“卡面上根本没有虚空费”的普通牌（Get 返回 null）不受本规则影响。
    /// 20260729 变更：加消耗时记录 ExhaustAddedByLost（此前牌上没有消耗才算我们加的），
    /// 供苍白按"消耗是否失心所加"精确移除；不再在这里做移除。
    /// </summary>
    private static void SyncExhaustKeyword(CardModel card, TraitState s)
    {
        if (!HasVoidCost(card)) return;
        // 登记过虚空费条目（含虚空 0 / 虚空 X）→ 自动【消耗】 //20260726
        if (!card.Keywords.Contains(CardKeyword.Exhaust))
        {
            card.AddKeyword(CardKeyword.Exhaust);
            s.ExhaustAddedByLost = true; // 这次消耗是失心/虚空费流程加的（20260729）
        }
        // 牌上已有消耗（自带或其他来源）→ 不动，也不标记为失心所加
    }

    /// <summary>
    /// 给"有固定虚空费"的卡在构造时声明费用的统一入口。
    /// 卡牌构造器里调用：CardTraits.SetVoidCost(this, 7);
    /// 依赖 VoidResource.Register() 已在 MainFile.Initialize 里先执行
    /// （已确认注册早于 ModelDb 懒加载，构造期使用安全）。
    /// </summary>
    public static void SetVoidCost(CardModel card, int amount)
    {
        card.SecondaryCosts().Set(VoidResource.Id, amount);
    }

    /// <summary>给"虚空 X 费"的卡声明费用（打出时消耗全部虚空作为 X）。</summary>
    public static void SetVoidCostX(CardModel card, int multiplier = 1)
    {
        card.SecondaryCosts().Set(VoidResource.Id, SecondaryResourceCost.X(multiplier));
    }
}
