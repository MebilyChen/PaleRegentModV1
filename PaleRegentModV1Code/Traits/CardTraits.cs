using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Utils;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 【失心】【苍白】两个自定义卡牌特质的实现。
///
/// ============ 机制说明（对应设计文档） ============
/// 【失心】（Lost）：
///   - 取消这张牌的灵魂耗能，把灵魂费 1:1 转换并入虚空费。
///     例：1灵魂0虚空 → 0灵魂1虚空；2灵魂2虚空 → 0灵魂4虚空。
///   - 对 X 费牌无效（X 费牌无法附加失心）。
///   - 自动获得【重放1】（BaseReplayCount = 1，打出后额外重放一次）。
///   - 与【苍白】互斥：附加失心会移除苍白。
/// 【苍白】（Pale）：
///   - 取消【失心】（恢复原本的灵魂费），并清空这张牌的虚空费。
///   - 自动获得【虚无】（Ethereal，回合结束未打出则消耗）。
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
///   4. 卡牌描述里的动态提示：本地化文本无法动态改，先在卡牌描述
///      （cards.json）里写明规则；将来可以用 Enchantment/描述后缀系统美化。
///
/// ============ 修改指南 ============
/// - 想改"失心的灵魂→虚空换算比例"：改 ApplyLost 里的 newVoidCost 计算。
/// - 想改"失心送的重放层数"：改 LostReplayCount 常量。
/// - 想给特质加图标/描述后缀：需要研究 STS2 的 Enchantment 系统（后续批次）。
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
        public bool CostSnapshotTaken;   // 是否已经记录过原始费用快照
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
    /// 判定用途：带纯粹的牌不会被感染（Infection 的疑虑变形）等效果影响。
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
        card.SecondaryCosts().Set(VoidResource.Id, newVoidCost);    // 并入虚空费
        card.BaseReplayCount = Math.Max(card.BaseReplayCount, LostReplayCount); // 重放1

        s.IsLost = true;
        SyncExhaustKeyword(card); // 虚空费>0 自动获得【消耗】
        return true;
    }

    /// <summary>
    /// 给一张牌附加【苍白】。
    /// 效果：取消失心（恢复灵魂费）；清空虚空费；获得【虚无】。
    /// </summary>
    public static void ApplyPale(CardModel card)
    {
        TraitState s = States.GetOrCreate(card);
        if (s.IsPale) return;

        EnsureCostSnapshot(card, s);

        // 取消失心：恢复原灵魂费
        if (s.IsLost)
        {
            card.EnergyCost.SetCustomBaseCost(s.OriginalEnergyCost);
            card.BaseReplayCount = 0; // 收回失心送的重放
            s.IsLost = false;
        }

        // 清空虚空费（苍白 = 不再欠虚空）
        card.SecondaryCosts().Clear(VoidResource.Id);

        // 自动获得【虚无】
        card.AddKeyword(CardKeyword.Ethereal);

        s.IsPale = true;
        SyncExhaustKeyword(card); // 虚空费清零后应移除自动【消耗】
    }

    /// <summary>内部：移除苍白状态（供 ApplyLost 里互斥切换时调用）。</summary>
    private static void RemovePale(CardModel card, TraitState s)
    {
        card.RemoveKeyword(CardKeyword.Ethereal);
        // 恢复原虚空费（苍白清掉的部分）
        if (s.OriginalVoidCost > 0)
            card.SecondaryCosts().Set(VoidResource.Id, s.OriginalVoidCost);
        s.IsPale = false;
    }

    // ---------------- 内部工具 ----------------

    /// <summary>第一次修改费用前，把原始费用记下来（用于日后还原）。</summary>
    private static void EnsureCostSnapshot(CardModel card, TraitState s)
    {
        if (s.CostSnapshotTaken) return;
        s.OriginalEnergyCost = card.EnergyCost.GetWithModifiers(CostModifiers.None);
        s.OriginalVoidCost = GetVoidCost(card);
        s.CostSnapshotTaken = true;
    }

    /// <summary>
    /// 规则："虚空费大于等于 0 的卡牌自动获得【消耗】。"
    /// 每次特质变化后调用，保证 Exhaust 关键词与虚空费同步。
    /// 注意：如果这张牌本来（CanonicalKeywords）就带消耗，不要移除它——
    /// 用 TraitState 无法区分，这里采用保守策略：只增不减，
    /// 除非是失心/苍白流程中我们自己加上去的。
    /// </summary>
    private static void SyncExhaustKeyword(CardModel card)
    {
        if (GetVoidCost(card) >= 0) //20260726 虚空费大于【等于】 0 的卡牌自动获得【消耗】。
            card.AddKeyword(CardKeyword.Exhaust);
        // 苍白清零虚空费后：若原虚空费为 0（即消耗是我们加的），移除
        else if (States.TryGetValue(card, out TraitState? s) && s!.OriginalVoidCost == 0) 
            card.RemoveKeyword(CardKeyword.Exhaust);
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
