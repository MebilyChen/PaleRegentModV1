using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 【失心】追加效果：
/// 当失心卡牌真正被消耗时，在其拥有者的手牌中生成一张【虚空】状态牌。
///
/// 这里监听 AfterCardExhausted，而不是 AfterCardPlayed / OnPlay：
/// - 失心自带重放1，牌效会执行两次；
/// - 但卡牌最终只会进入一次消耗堆；
/// - 因而一次实际消耗只会生成一张虚空。
///
/// 如果卡牌之后从消耗堆被取回并再次消耗，则会再次生成一张虚空。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardExhausted))]
internal static class LostExhaustVoidPatch
{
    /// <summary>
    /// Hook.AfterCardExhausted 是返回 Task 的异步方法。
    ///
    /// Harmony 的普通 async Postfix 不一定会被原调用方等待，
    /// 所以这里通过 ref Task __result 包装原 Task，确保：
    /// 1. 先完整执行游戏原本的消耗后处理；
    /// 2. 再生成虚空；
    /// 3. 生成动作仍处于原异步动作链中。
    ///
    /// 参数使用 Harmony 的 __0 / __2 按位置取值，
    /// 避免游戏版本更新后仅修改了参数名称而导致补丁失效。
    /// __0 = CombatState
    /// __2 = CardModel
    /// </summary>
    private static void Postfix(
        CombatState __0,
        CardModel __2,
        ref Task __result)
    {
        CardModel exhaustedCard = __2;

        // 必须在当前时刻先记录。
        // 防止原版或其他 Mod 的 AfterCardExhausted 回调
        // 在异步流程中修改卡牌特质，导致后续判定失真。
        bool wasLostWhenExhausted = CardTraits.IsLost(exhaustedCard);

        if (!wasLostWhenExhausted)
            return;

        __result = AddVoidAfterOriginalExhaustProcessing(
            __result,
            __0,
            exhaustedCard);
    }

    /// <summary>
    /// 等待原版消耗后处理结束，然后向手牌生成一张虚空。
    /// </summary>
    private static async Task AddVoidAfterOriginalExhaustProcessing(
        Task originalTask,
        CombatState combatState,
        CardModel exhaustedCard)
    {
        // 先执行并等待原版 Hook 的所有逻辑。
        await originalTask;

        // 被正常消耗的战斗卡牌应当始终存在 Owner。
        // 这里仍然做防御性判断，避免异常生成/测试卡导致崩溃。
        if (exhaustedCard.Owner == null)
            return;

        CardModel voidCard =
            combatState.CreateCard<TheVoidStatus>(exhaustedCard.Owner);

        await CardPileCmd.AddGeneratedCardToCombat(
            voidCard,
            PileType.Hand,
            creator: exhaustedCard.Owner);
    }
}
