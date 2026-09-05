using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【苦痛之路】debuff。
///
/// Amount 表示本回合还需要造成多少点“有效目标伤害”。
///
/// 多人规则：
///
/// 1. 只统计 Owner 对“曾经向 Owner 施加过本苦痛之路”的玩家造成的伤害。
///
/// 2. 如果只有一个玩家施加过苦痛之路：
///    只统计 Owner 对该玩家造成的累计伤害。
///
/// 3. 如果多个玩家都施加过苦痛之路：
///    每名施加者分别累计 Owner 对其造成的伤害，
///    苦痛之路只取这些累计伤害中的最高值。
///
/// 例如：
/// 初始苦痛之路为 20。
///
/// 玩家 A、玩家 B 都施加过苦痛之路。
///
/// Owner 对 A 累计造成 8 点伤害；
/// Owner 对 B 累计造成 13 点伤害。
///
/// 则苦痛之路进度使用 max(8, 13) = 13，
/// 显示剩余 7 层。
///
/// 不会把 8 + 13 = 21 相加。
///
/// 如果随后 Owner 又对 A 造成 7 点：
/// A 累计 = 15；
/// B 累计 = 13；
/// 取最大值 15；
/// 苦痛之路显示剩余 5 层。
///
/// 如果任意一个施加者本回合累计受到的伤害达到完整层数，
/// 苦痛之路立即移除。
///
/// 如果本回合 Owner 造成过伤害，但苦痛之路仍未清空，
/// Owner 在其阵营回合结束时受到等同于当前生命值的伤害。
///
/// 如果效果仍存在，则在 Owner 下一回合开始时，
/// 恢复本回合已经减少的层数。
///
/// UI：本 Power 不提供血条长度预览；存在时仅由
/// PathOfPainHealthBarSystem 在血条正中央显示 ⚠️ 图标。
/// </summary>
public class PathOfPainPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 所有曾经向 Owner 施加过这个苦痛之路 Power 的玩家 NetId。
    ///
    /// 使用 NetId 而不是直接保存 Creature 引用，
    /// 可以避免多人状态复制 / Creature clone 后引用发生变化的问题。
    /// </summary>
    private HashSet<ulong> _applierPlayerIds = new();

    /// <summary>
    /// 本回合 Owner 对每个苦痛之路施加者造成的累计伤害。
    ///
    /// key   = 玩家 NetId
    /// value = 本回合 Owner 对该玩家造成的累计伤害
    /// </summary>
    private Dictionary<ulong, int> _damageByApplierThisTurn = new();

    /// <summary>
    /// 本回合已经从 Amount 中扣除的进度。
    ///
    /// 注意：
    /// 这里不是所有玩家伤害之和，
    /// 而是所有施加者中“最高的单人累计伤害”。
    ///
    /// 下一回合开始时用于恢复 Amount。
    /// </summary>
    private int _amountReducedThisTurn;

    /// <summary>
    /// Owner 本回合是否造成过伤害事件。
    ///
    /// 保留原苦痛之路的行为：
    /// 只要 Owner 对其他目标发生过伤害结算，
    /// 即认为本回合攻击过。
    ///
    /// 但只有对苦痛之路施加者造成的伤害
    /// 才能够减少苦痛之路层数。
    /// </summary>
    private bool _attackedThisTurn;

    /// <summary>
    /// PowerModel 使用 MemberwiseClone。
    ///
    /// HashSet / Dictionary 属于引用类型，
    /// 如果不在这里重新复制，
    /// 多个 mutable clone 可能共享同一份集合。
    ///
    /// 因此每次 clone 时都创建独立集合。
    /// </summary>
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();

        _applierPlayerIds = new HashSet<ulong>(_applierPlayerIds);

        _damageByApplierThisTurn =
            new Dictionary<ulong, int>(_damageByApplierThisTurn);
    }

    /// <summary>
    /// 首次施加苦痛之路时，记录首次施加者。
    /// </summary>
    public override Task AfterApplied(
        Creature? applier,
        CardModel? cardSource)
    {
        RegisterApplier(applier);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 苦痛之路发生叠层时，记录新的施加者。
    ///
    /// 这一 Hook 对多人非常重要：
    ///
    /// Power 已经存在时，再次使用 PowerCmd.Apply
    /// 通常不会重新执行 AfterApplied，
    /// 而是修改现有 Power 的 Amount。
    ///
    /// 因此必须通过 AfterPowerAmountChanged
    /// 捕获后续玩家的 applier。
    /// </summary>
    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        // 只关心“这个 PathOfPainPower 自身”的 Amount 变化。
        if (power != this)
        {
            return Task.CompletedTask;
        }

        /*
         * 只有正向增加才代表玩家实际施加 / 追加了苦痛之路。
         *
         * 避免未来如果有某些效果通过 PowerCmd
         * 减少苦痛之路层数时，
         * 错误地把那个 Creature 注册成施加者。
         */
        if (amount > 0)
        {
            RegisterApplier(applier);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 注册一个苦痛之路施加者。
    /// 只接受玩家 Creature。
    /// </summary>
    private void RegisterApplier(Creature? applier)
    {
        if (applier == null || !applier.IsPlayer)
        {
            return;
        }

        /*
         * Player 在多人战斗中使用 NetId 作为稳定身份。
         *
         * HashSet 自动去重：
         * 同一个玩家重复施加苦痛之路不会产生重复记录。
         */
        ulong playerId = applier.Player!.NetId;

        _applierPlayerIds.Add(playerId);
    }

    /// <summary>
    /// Owner 造成伤害后：
    ///
    /// 只有目标属于“曾施加过苦痛之路的玩家”时，
    /// 该伤害才计入苦痛之路进度。
    ///
    /// 多个施加者分别累计，
    /// 最终只使用累计伤害最高的玩家。
    /// </summary>
    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        // 只处理 Power 持有者对其他目标造成的伤害。
        if (dealer != Owner || target == Owner)
        {
            return;
        }

        /*
         * 保留原版苦痛之路行为：
         *
         * 只要 Owner 发生了对其他目标的伤害结算，
         * 就认为本回合攻击过。
         *
         * 即使目标不是苦痛之路施加者，
         * 或者最终记录伤害为 0，
         * 回合结束时仍然可以触发惩罚。
         */
        _attackedThisTurn = true;

        /*
         * 苦痛之路现在只统计对“玩家”的伤害。
         *
         * Osty、召唤物、其他怪物等都不会
         * 帮 Owner 完成苦痛之路。
         */
        if (!target.IsPlayer)
        {
            return;
        }

        ulong targetPlayerId = target.Player!.NetId;

        /*
         * 目标必须曾经施加过这个苦痛之路。
         *
         * 例如：
         *
         * 玩家 A 施加了苦痛之路；
         * 玩家 B 没有施加。
         *
         * Owner 打 B 再多伤害都不会减少层数。
         */
        if (!_applierPlayerIds.Contains(targetPlayerId))
        {
            return;
        }

        /*
         * 保留你原代码的伤害口径：
         * 使用 DamageResult.TotalDamage。
         *
         * 如果以后你决定只统计实际穿透格挡造成的 HP 伤害，
         * 可以在这里改成对应的 UnblockedDamage。
         */
        int damage = decimal.ToInt32(result.TotalDamage);

        /*
         * 0 点伤害：
         * 已经通过 _attackedThisTurn 记录为攻击过，
         * 但是不会减少苦痛之路层数。
         */
        if (damage <= 0)
        {
            return;
        }

        /*
         * 更新“这个玩家”本回合受到的累计伤害。
         */
        _damageByApplierThisTurn.TryGetValue(
            targetPlayerId,
            out int previousDamageToTarget);

        int newDamageToTarget = previousDamageToTarget + damage;

        _damageByApplierThisTurn[targetPlayerId] =
            newDamageToTarget;

        /*
         * 多人苦痛之路的核心：
         *
         * 不把各玩家受到的伤害相加。
         *
         * 只取：
         *
         * max(
         *     玩家A本回合累计受伤,
         *     玩家B本回合累计受伤,
         *     玩家C本回合累计受伤...
         * )
         */
        int highestDamageToOneApplier =
            _damageByApplierThisTurn.Values.Max();

        /*
         * _amountReducedThisTurn 就是上一次已经计入
         * Amount 的“最高单人累计伤害”。
         *
         * 如果新的最高值没有超过旧最高值，
         * 苦痛之路显示层数不需要发生变化。
         *
         * 例如：
         *
         * A 已经累计受到 12 点；
         * B 现在从 0 受到 8 点；
         *
         * max 仍然是 12，
         * 所以 Amount 不变。
         */
        if (highestDamageToOneApplier <= _amountReducedThisTurn)
        {
            return;
        }

        /*
         * 当前完整要求 =
         *
         * 当前剩余 Amount
         * +
         * 已经扣掉的最高伤害
         *
         * 例如：
         *
         * 初始 20；
         * 当前最高伤害 8；
         * Amount = 12；
         *
         * fullRequirement = 12 + 8 = 20。
         *
         * 这样写还有一个好处：
         * 如果玩家在本回合中途又叠加了苦痛之路，
         * PowerCmd 增加了 Amount，
         * 完整要求也能够正确增加。
         */
        int fullRequirement =
            Amount + _amountReducedThisTurn;

        /*
         * 只要任意一个“苦痛之路施加者”
         * 本回合累计受到的伤害达到完整要求，
         * 立即清除苦痛之路。
         */
        if (highestDamageToOneApplier >= fullRequirement)
        {
            Flash();

            await PowerCmd.Remove(this);

            return;
        }

        /*
         * 更新已经扣除的最大单人伤害。
         */
        _amountReducedThisTurn =
            highestDamageToOneApplier;

        /*
         * Amount 显示为：
         *
         * 完整要求 - 当前最高单人累计伤害
         */
        int remainingAmount =
            fullRequirement - highestDamageToOneApplier;

        Flash();

        SetAmount(remainingAmount);
    }

    /// <summary>
    /// Owner 所在阵营回合结束。
    ///
    /// 如果本回合 Owner 造成过伤害，
    /// 但是苦痛之路依然存在，
    /// 则 Owner 受到等同于当前生命值的伤害。
    /// </summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 只处理 Owner 所在阵营的回合结束。
        if (side != Owner.Side || !participants.Contains(Owner))
        {
            return;
        }

        /*
         * 如果苦痛之路已经因为伤害达到要求而被移除，
         * 那么这个 Power 自然不会再收到这个 Hook。
         *
         * 能执行到这里说明苦痛之路仍存在。
         */
        if (_attackedThisTurn && Owner.IsAlive)
        {
            Flash();

            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                Owner.CurrentHp,
                ValueProp.Unpowered,
                null,
                cardPlay: null
            );
        }

        /*
         * 此处只重置攻击标记。
         *
         * 不清空：
         *
         * _amountReducedThisTurn
         * _damageByApplierThisTurn
         *
         * 因为下一次 Owner 回合开始时，
         * 还需要先恢复苦痛之路的完整 Amount。
         */
        _attackedThisTurn = false;
    }

    /// <summary>
    /// Owner 下一回合开始时：
    ///
    /// 1. 恢复上一回合已经减少的苦痛之路层数；
    /// 2. 清空各施加者的本回合伤害统计；
    /// 3. 开始新的统计周期。
    /// </summary>
    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        // 只处理 Owner 所在阵营的回合开始。
        if (side != Owner.Side || !participants.Contains(Owner))
        {
            return Task.CompletedTask;
        }

        /*
         * 恢复上一回合已经减少的层数。
         *
         * 例如：
         *
         * 完整要求 = 20
         *
         * A 累计受到 8；
         * B 累计受到 13；
         *
         * 使用最高值 13。
         *
         * 回合结束时：
         * Amount = 7
         * _amountReducedThisTurn = 13
         *
         * 下一回合：
         * 7 + 13 = 20
         */
        if (_amountReducedThisTurn > 0)
        {
            SetAmount(
                Amount + _amountReducedThisTurn
            );
        }

        /*
         * 新回合开始：
         * 清空各个玩家上一回合的伤害累计。
         *
         * 注意：
         * 不清空 _applierPlayerIds。
         *
         * 因为只要这个苦痛之路 Power 还存在，
         * 曾经施加过它的玩家就一直属于有效目标。
         */
        _damageByApplierThisTurn.Clear();

        _amountReducedThisTurn = 0;

        _attackedThisTurn = false;

        return Task.CompletedTask;
    }
}
