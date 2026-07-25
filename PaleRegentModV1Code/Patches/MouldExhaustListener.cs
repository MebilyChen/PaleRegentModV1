using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 【模具】消耗计数补丁（名词表 N#9，20260725 新增）。
///
/// 1. Hook.AfterCardExhausted：每当有卡牌被消耗，若为模具牌（IsMould）
///    则调用 MouldHelper.NoteExhaust 记 1 点计数。
///    （参考 modstudy CombatEndPatch 的 Hook patch 写法；
///    Hook 类的 AfterCardExhausted 签名与 PowerModel.AfterCardExhausted
///    一致：PlayerChoiceContext, CardModel, bool。若目标方法名/签名不匹配
///    导致 Harmony 报错，请把日志发我调整。）
/// 2. CombatState.AddPlayer（战斗开始）：清零计数，防止上一场残留。
/// </summary>
[HarmonyPatch(typeof(Hook), "AfterCardExhausted")]
internal static class MouldExhaustPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel card)
    {
        MouldHelper.NoteExhaust(card);
    }
}

/// <summary>战斗开始时清零模具计数。</summary>
[HarmonyPatch(typeof(CombatState), "AddPlayer")]
internal static class MouldCombatStartPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        MouldHelper.ResetCounts();
    }
}
