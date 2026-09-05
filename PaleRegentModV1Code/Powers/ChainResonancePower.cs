using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【连锁共鸣】
///
/// 一个 Power，一个 Icon，一个总 Amount。
///
/// ============================================================
/// 【连连看 / ChainMatch】
/// ============================================================
///
/// - 仅本回合生效；
/// - 可以重复打出并叠加；
/// - 每张牌提供的初始数值都加入 Amount；
/// - 每张牌提供的 DamageAdd 也可以叠加；
/// - 每次成功触发连续同名牌时，
///   Amount 增加当前所有 ChainMatch 的 DamageAdd 总和；
/// - 上述 ChainMatch 提供的所有数值都属于“本回合临时值”；
/// - 回合结束后全部扣除；
/// - 如果没有 ChainReaction 永久部分，则整个 Power Remove。
///
/// ============================================================
/// 【连锁反应 / ChainReactionCard】
/// ============================================================
///
/// - 整场战斗生效；
/// - 可以重复打出并叠加；
/// - 5 + 5 = 10；
/// - 回合结束不会移除；
/// - ChainMatch 的临时成长不会永久污染 ChainReaction。
///
/// ============================================================
/// 两者共存
/// ============================================================
///
/// 例如：
///
/// ChainReaction 永久 = 10
/// ChainMatch 本回合 = 6
///
/// Amount / Icon = 16
///
/// ChainMatch 每次触发总共 +2：
///
/// 16 -> 18 -> 20
///
/// 回合结束后把 ChainMatch 的 10 点临时贡献全部扣掉：
///
/// Amount / Icon -> 10
///
/// Power 继续存在。
/// </summary>
public class ChainResonancePower : PaleRegentModV1Power
{
    // =========================================================
    // 状态
    // =========================================================

    /// <summary>
    /// 当前回合是否存在 ChainMatch / 连连看。
    /// </summary>
    private bool _activeThisTurn;


    /// <summary>
    /// 当前战斗是否已经存在 ChainReactionCard / 连锁反应。
    ///
    /// 一旦变成 true，本场战斗内不会主动设回 false。
    /// </summary>
    private bool _activeForCombat;


    // =========================================================
    // ChainMatch 临时账本
    // =========================================================

    /// <summary>
    /// 当前 Amount 中，有多少属于 ChainMatch 的本回合临时贡献。
    ///
    /// 包括：
    ///
    /// 1. 每张 ChainMatch 自己 Apply / Modify 进去的初始值；
    /// 2. 连锁触发产生的临时成长。
    ///
    /// 回合结束时全部扣掉。
    /// </summary>
    private decimal _temporaryDamageThisTurn;


    /// <summary>
    /// 当前回合每次连锁成功以后，
    /// ChainMatch 总共应该增加多少。
    ///
    /// 每张 ChainMatch 都正常叠加。
    ///
    /// 例如：
    ///
    /// 第一张 DamageAdd = 1
    /// 第二张 DamageAdd = 1
    ///
    /// 最终每次触发：
    /// +2
    /// </summary>
    private decimal _turnDamageAdd;


    // =========================================================
    // 连续同名牌
    // =========================================================

    /// <summary>
    /// 本回合上一张由持有者打出的牌。
    /// 相同 ModelId 视为同名牌。
    /// </summary>
    private ModelId? _lastCardId;


    // =========================================================
    // Power 基础属性
    // =========================================================

    public override PowerType Type =>
        PowerType.Buff;


    public override PowerStackType StackType =>
        PowerStackType.Counter;


    /// <summary>
    /// Power 只要存在就显示。
    ///
    /// 不再搞“隐藏 Power 但实例继续存在”的机制。
    ///
    /// 只有 ChainMatch：
    ///     回合结束直接 Remove。
    ///
    /// 有 ChainReaction：
    ///     回合结束 Power 继续存在。
    /// </summary>
    protected override bool IsVisibleInternal =>
        true;


    // =========================================================
    // ChainMatch
    // =========================================================

    /// <summary>
    /// ChainMatch.cs 每打出一张都会调用一次。
    ///
    /// 注意：
    ///
    /// 这里不负责给 Amount 增加 initialDamage。
    ///
    /// 因为 ChainMatch.cs 已经：
    ///
    /// Power 不存在：
    ///     PowerCmd.Apply(initialDamage)
    ///
    /// Power 已存在：
    ///     AddTemporaryDamageForTurn(initialDamage)
    ///
    /// 所以这里再次加 Amount 会造成双倍。
    ///
    /// 这里只负责：
    ///
    /// 1. 标记本回合启用；
    /// 2. 第一张 ChainMatch 创建 Power 时，
    ///    把已经 Apply 进去的初始 Amount 记入临时账本；
    /// 3. 累加 DamageAdd。
    /// </summary>
    public void ActivateForTurn(
        decimal initialDamage,
        decimal damageAdd)
    {
        // =====================================================
        // 特殊情况：
        //
        // Power 原本不存在。
        //
        // ChainMatch.cs 的顺序是：
        //
        // PowerCmd.Apply(initialDamage)
        // ↓
        // GetPower()
        // ↓
        // ActivateForTurn()
        //
        // 这种情况下不会经过 AddTemporaryDamageForTurn，
        // 所以必须在这里把第一次 Apply 进去的 Amount
        // 登记成临时值。
        // =====================================================

        if (!_activeThisTurn &&
            _temporaryDamageThisTurn <= 0m)
        {
            // 如果这是 ChainMatch 新创建出来的 Power，
            // 当前 Amount 就是它实际 Apply 成功后的数值。
            //
            // 使用 Amount 而不是单纯 initialDamage，
            // 可以避免未来有其他机制修改实际 Apply 数值时
            // 临时账本对不上。
            if (!_activeForCombat &&
                Amount > 0m)
            {
                _temporaryDamageThisTurn =
                    Amount;
            }
        }


        _activeThisTurn =
            true;


        // =====================================================
        // 每一张 ChainMatch 的 DamageAdd 都叠加。
        //
        // +1 +1 = 每次触发 +2
        // =====================================================

        if (damageAdd > 0m)
        {
            _turnDamageAdd +=
                damageAdd;
        }


        // 参数由现有卡牌接口传进来。
        // 第一张新建 Power 的实际 Amount 已经通过上面记录。
        _ = initialDamage;
    }


    // =========================================================
    // ChainReactionCard
    // =========================================================

    /// <summary>
    /// ChainReactionCard.cs 每打出一张都会调用。
    ///
    /// Amount 的增加已经由卡牌负责：
    ///
    /// 第一次：
    ///     PowerCmd.Apply(baseDamage)
    ///
    /// 之后：
    ///     PowerCmd.ModifyAmount(+baseDamage)
    ///
    /// 所以这里绝对不要再修改 Amount。
    ///
    /// 这里只负责把 Power 标记为：
    /// “本场战斗拥有永久部分”。
    /// </summary>
    public void ActivateForCombat(
        decimal initialDamage,
        decimal damageAdd)
    {
        _activeForCombat =
            true;


        // =====================================================
        // 不要在这里：
        //
        // SetAmount(...)
        // Math.Max(...)
        // += initialDamage
        //
        // 否则都会和 ChainReactionCard.cs
        // 自己的 Apply / ModifyAmount 重复。
        //
        // 现在：
        //
        // 第一张 5
        // 第二张 +5 = 10
        // 第三张 +5 = 15
        //
        // 完全由卡牌现有逻辑完成。
        // =====================================================

        _ = initialDamage;
        _ = damageAdd;
    }


    // =========================================================
    // ChainMatch 已存在 Power 时增加临时值
    // =========================================================

    /// <summary>
    /// ChainMatch.cs 在 Power 已经存在时调用。
    ///
    /// 例如：
    ///
    /// 当前 ChainReaction = 10
    ///
    /// 打一张 ChainMatch +3：
    ///
    /// Amount：
    ///     10 -> 13
    ///
    /// 临时账本：
    ///     0 -> 3
    ///
    /// 回合结束：
    ///     13 -> 10
    /// </summary>
    public async Task AddTemporaryDamageForTurn(
        PlayerChoiceContext choiceContext,
        decimal amount,
        CardModel? sourceCard)
    {
        if (amount <= 0m)
        {
            return;
        }


        _activeThisTurn =
            true;


        // =====================================================
        // 记录修改前 Amount。
        //
        // 这样即使以后 PowerCmd 对实际数值进行了调整，
        // 我们记录的也是“实际增加了多少”，
        // 避免回合结束时多扣。
        // =====================================================

        decimal amountBefore =
            Amount;


        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            amount,
            Owner,
            sourceCard
        );


        decimal actualAdded =
            Amount -
            amountBefore;


        if (actualAdded > 0m)
        {
            _temporaryDamageThisTurn +=
                actualAdded;
        }
    }


    // =========================================================
    // 打牌后检测连续同名牌
    // =========================================================

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // =====================================================
        // 只处理 Power 持有者自己的卡牌。
        // =====================================================

        if (cardPlay.Card.Owner.Creature !=
            Owner)
        {
            return;
        }


        // =====================================================
        // 判断是否连续同名。
        // =====================================================

        ModelId currentCardId =
            cardPlay.Card.Id;


        bool isChain =
            _lastCardId != null &&
            _lastCardId.Equals(
                currentCardId
            );


        // 无论是否触发，
        // 都记录这一张牌作为下一张的比较对象。
        _lastCardId =
            currentCardId;


        // =====================================================
        // 理论保险。
        //
        // Power 正常存在时至少有一种来源处于 Active。
        // =====================================================

        if (!_activeThisTurn &&
            !_activeForCombat)
        {
            return;
        }


        // 必须连续同名。
        if (!isChain)
        {
            return;
        }


        ICombatState? combatState =
            Owner.CombatState;


        if (combatState == null ||
            Amount <= 0m)
        {
            return;
        }


        List<Creature> enemies =
            combatState
                .GetOpponentsOf(Owner)
                .Where(
                    creature =>
                        creature.IsAlive
                )
                .ToList();


        Flash();


        // =====================================================
        // 本次伤害 / 格挡使用当前总 Amount。
        //
        // 永久 + 临时会自然合并。
        //
        // 例如：
        //
        // ChainReaction = 10
        // ChainMatch    = 6
        //
        // Amount = 16
        //
        // 本次：
        //     全体 16 伤害
        //     获得 16 格挡
        // =====================================================

        decimal currentDamage =
            Amount;


        // =====================================================
        // 全体伤害
        // =====================================================

        foreach (Creature enemy in
                 enemies)
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                currentDamage,
                ValueProp.Unpowered |
                ValueProp.SkipHurtAnim,
                Owner
            );
        }


        // =====================================================
        // 获得等量格挡
        // =====================================================

        await CreatureCmd.GainBlock(
            Owner,
            new BlockVar(
                currentDamage,
                ValueProp.Move
            ),
            cardPlay
        );


        // =====================================================
        // ChainMatch 触发成长
        //
        // 只有当前回合存在 ChainMatch 时才增长。
        //
        // ChainReaction 自己不提供成长。
        //
        //
        // 例如：
        //
        // 两张 ChainMatch：
        //
        // _turnDamageAdd = 2
        //
        // 当前 Amount = 16
        //
        // 触发以后：
        //     16 -> 18
        //
        // 新增的 2 属于 ChainMatch 临时值，
        // 所以同时加入 _temporaryDamageThisTurn。
        // =====================================================

        if (_activeThisTurn &&
            _turnDamageAdd > 0m)
        {
            decimal growth =
                _turnDamageAdd;


            decimal amountBefore =
                Amount;


            await PowerCmd.ModifyAmount(
                choiceContext,
                this,
                growth,
                Owner,
                cardPlay.Card
            );


            decimal actualGrowth =
                Amount -
                amountBefore;


            if (actualGrowth > 0m)
            {
                _temporaryDamageThisTurn +=
                    actualGrowth;
            }
        }
    }


    // =========================================================
    // 回合结束
    // =========================================================

    /// <summary>
    /// 持有者阵营回合结束：
    ///
    /// 1. 连续同名记录清空；
    ///
    /// 2. ChainMatch 本回合效果结束；
    ///
    /// 3. ChainMatch 的初始值和触发成长全部移除；
    ///
    /// 4. 如果没有 ChainReaction：
    ///        整个 Power Remove；
    ///
    /// 5. 如果有 ChainReaction：
    ///        Power 保留，只留下永久 Amount。
    /// </summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side !=
            Owner.Side)
        {
            return;
        }


        // 连续同名不能跨回合。
        _lastCardId =
            null;


        // =====================================================
        // 本回合没有 ChainMatch。
        //
        // 说明只有永久 ChainReaction。
        //
        // 什么都不用处理。
        // =====================================================

        if (!_activeThisTurn)
        {
            return;
        }


        decimal temporaryDamage =
            _temporaryDamageThisTurn;


        // =====================================================
        // ChainMatch 本回合结束。
        // =====================================================

        _activeThisTurn =
            false;


        _turnDamageAdd =
            0m;


        // =====================================================
        // 如果没有永久 ChainReaction：
        //
        // 当前整个 Power 都属于 ChainMatch。
        //
        // 直接 Remove。
        //
        // 不需要先把 Amount 减到 0。
        // =====================================================

        if (!_activeForCombat)
        {
            _temporaryDamageThisTurn =
                0m;


            await PowerCmd.Remove(
                this
            );


            return;
        }


        // =====================================================
        // 有永久 ChainReaction：
        //
        // Power 必须继续存在。
        //
        // 只扣除 ChainMatch 本回合所有临时贡献。
        // =====================================================

        if (temporaryDamage <= 0m)
        {
            _temporaryDamageThisTurn =
                0m;

            return;
        }


        decimal removableDamage =
            temporaryDamage >
            Amount
                ? Amount
                : temporaryDamage;


        if (removableDamage > 0m)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                this,
                -removableDamage,
                Owner,
                null
            );
        }


        _temporaryDamageThisTurn =
            0m;
    }
}
