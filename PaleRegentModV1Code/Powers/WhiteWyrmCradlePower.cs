using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【白沃姆摇篮】buff（效果表 P#29，卡牌 C#97 白沃姆摇篮施加）。
/// 效果：
/// 1. 你的【蓄灵】触发后不再消失；
/// 2. 你的回合开始时，获得 1 层【蓄灵】；
/// 3. 每回合第一次受到未被格挡的攻击时，移除你的全部【蓄灵】，并获得
///    [层数] 层【白根】。
/// </summary>
public class WhiteWyrmCradlePower : PaleRegentModV1Power
{
    /// <summary>每回合固定获得的蓄灵层数。</summary>
    private const int SoulChargePerTurn = 1;

    /// <summary>
    /// 当前回合是否已处理过首次未格挡攻击。
    /// 在玩家回合开始时重置，覆盖本回合及后续敌方回合。
    /// </summary>
    private bool _firstUnblockedAttackHandledThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 每个玩家回合开始时重置“首次受击”标记。
    /// 使用 Early 阶段，确保其余回合开始效果开始结算前标记已刷新。
    /// </summary>
    public override Task AfterPlayerTurnStartEarly(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner.Player)
        {
            _firstUnblockedAttackHandledThisTurn = false;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 你的回合开始时获得 1 层蓄灵。
    /// 此处不再获得白根；白根改由首次未格挡攻击触发。
    /// </summary>
    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<SoulNextTurnPower>(
            choiceContext,
            Owner,
            SoulChargePerTurn,
            Owner,
            null);
    }

    /// <summary>
    /// 每回合第一次受到未格挡的攻击伤害时：清空全部蓄灵，并获得 Amount 层白根。
    /// </summary>
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner ||
            _firstUnblockedAttackHandledThisTurn ||
            result.UnblockedDamage <= 0 ||
            !props.HasFlag(ValueProp.Move))
        {
            return;
        }

        _firstUnblockedAttackHandledThisTurn = true;
        Flash();

        // 无论当前是否已有蓄灵，首次未格挡攻击均会给予白根。
        await PowerCmd.Remove<SoulNextTurnPower>(Owner);

        if (Amount > 0)
        {
            await PowerCmd.Apply<WhiteRootPower>(
                choiceContext,
                Owner,
                Amount,
                Owner,
                null);
        }
    }
}
