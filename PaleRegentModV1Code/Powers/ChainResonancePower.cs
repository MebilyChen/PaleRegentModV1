using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【连锁共鸣】buff。
///
/// Amount 表示“下一次触发时造成的伤害”，而不是层数。
///
/// 效果：
/// 连续打出同名牌时，从第 2 张起，对所有敌人造成 Amount 点伤害；
/// 每触发一次，Amount 增加 DamageAdd。
///
/// 两种启用方式：
/// - 【连连看 C#62】：仅本回合启用；
/// - 【连锁反应 C#71】：整场战斗启用。
///
/// 即使当前没有启用，Power 也不会被移除，因为 Amount 需要保存
/// 本场战斗内已经累计的伤害。
///
/// 修复记录（对照排查结论）：
/// 1. StackType 由 Single 改为 Counter：Single 会让游戏把这个 Power
///    当作“只有存在/不存在”的状态，不显示数字、也不会正确刷新。
///    Amount 语义上虽然是“下一次触发伤害”而不是层数，但对 UI 而言仍然
///    是一个需要显示、需要变化的数值，所以必须用 Counter。
/// 2. ActivateForTurn / ActivateForCombat 现在都需要传入 initialDamage，
///    并在 Amount 尚未初始化（<= 0）时把它写入，避免“触发前 Amount 恒为 0”
///    导致 AfterCardPlayed 静默 return 的问题。
/// 3. AfterCardPlayed 里用 cardPlay.Card.Owner.Creature != Owner 判断持牌人，
///    比 cardPlay.Player != Owner.Player 更稳（不依赖 Player 对象的构造/重建方式）。
/// 4. 记录 _lastCardId 的时机提前到 IsActive 判断之前：
///    这样即使某张牌打出时 Power 还没被激活，它依然会被记为“上一张牌”，
///    避免出现“连续打两张不触发、第三张才触发”的事件顺序问题。
/// 5. AfterSideTurnEnd 用 side != Owner.Side 判断，而不是
///    participants.Contains(Owner)，避免 participants 集合内容/对象身份
///    在多人或状态重建场景下不一致导致的问题。
/// </summary>
public class ChainResonancePower : PaleRegentModV1Power
{
    /// <summary>
    /// 【连连看】提供的本回合启用状态。
    /// </summary>
    private bool _activeThisTurn;

    /// <summary>
    /// 【连锁反应】提供的整场战斗启用状态。
    /// </summary>
    private bool _activeForCombat;

    /// <summary>
    /// 每次效果触发后增加的伤害。
    /// </summary>
    private decimal _damageAdd;

    /// <summary>
    /// 上一张由持有者打出的牌的 ModelId。
    /// 同一个 ModelId 视为同名牌。
    /// </summary>
    private ModelId? _lastCardId;

    public override PowerType Type => PowerType.Buff;

    // 改为 Counter：Amount 虽然语义上是“下一次触发伤害”而不是层数，
    // 但仍需要在 UI 上正常显示数字并可被 ModifyAmount 正确刷新。
    // 重复叠加的问题改由“不重复 Apply、只激活已存在实例”的调用方式来解决，
    // 而不是靠 StackType.Single 来强行阻止叠加（那样会连数字都不显示）。
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 当前是否应该监听连续同名牌。
    /// </summary>
    private bool IsActive => _activeThisTurn || _activeForCombat;

    /// <summary>
    /// 由【连连看】调用：本回合启用效果。
    ///
    /// 重复使用时不会重置上一张牌，也不会重置累计伤害；
    /// 因此连续打出两张【连连看】时，第二张本身也可以触发效果。
    ///
    /// initialDamage：仅在 Amount 尚未初始化（<= 0）时写入，
    /// 用于修复“Power 已挂载但 Amount 恒为 0，导致触发逻辑被
    /// Amount &lt;= 0 的判断静默拦截”的问题。
    /// </summary>
    public void ActivateForTurn(decimal initialDamage, decimal damageAdd)
    {
        _activeThisTurn = true;
        UpdateDamageAdd(damageAdd);
        EnsureDamageInitialized(initialDamage);
    }

    /// <summary>
    /// 由【连锁反应】调用：整场战斗启用效果。
    /// </summary>
    public void ActivateForCombat(decimal initialDamage, decimal damageAdd)
    {
        _activeForCombat = true;
        UpdateDamageAdd(damageAdd);
        EnsureDamageInitialized((int)initialDamage);
    }

    /// <summary>
    /// 确保 Amount 在首次启用时有一个大于 0 的初始值。
    ///
    /// 注意：SetAmount 是假设基类提供的写入 Amount 的方法名，
    /// 如果 PaleRegentModV1Power / Power 基类里实际方法名不同
    /// （例如 InitAmount，或者需要走 PowerCmd.SetAmount 命令），
    /// 请把这里替换成你项目里实际可用的写法。
    /// </summary>
    private void EnsureDamageInitialized(decimal initialDamage)
    {
        if (Amount <= 0m && initialDamage > 0m)
        {
            SetAmount((int)initialDamage);
        }
    }

    /// <summary>
    /// 更新每次触发后的伤害增量。
    ///
    /// 如果两张共用此 Power 的卡提供不同数值，
    /// 保留其中较高的值，避免较弱版本覆盖较强版本。
    /// </summary>
    private void UpdateDamageAdd(decimal damageAdd)
    {
        if (damageAdd > _damageAdd)
        {
            _damageAdd = damageAdd;
        }
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // 用卡牌的实际所有者判断，不依赖 cardPlay.Player 的构造方式。
        // 只统计持有者自己的牌，覆盖手动出牌、自动打出、重放等场景。
        if (cardPlay.Card.Owner.Creature != Owner)
        {
            return;
        }

        ModelId currentCardId = cardPlay.Card.Id;

        bool isChain =
            _lastCardId != null &&
            _lastCardId.Equals(currentCardId);

        // 无论 Power 当前是否启用，都记录持有者打出的这张牌，
        // 避免因为“启用牌本身的事件时序”导致第一张牌漏记。
        _lastCardId = currentCardId;

        // Power 虽然需要留下来保存累计伤害，
        // 但未启用时不触发效果。
        if (!IsActive || !isChain)
        {
            return;
        }

        ICombatState? combatState = Owner.CombatState;

        if (combatState == null || Amount <= 0)
        {
            return;
        }

        List<Creature> enemies = combatState
            .GetOpponentsOf(Owner)
            .Where(creature => creature.IsAlive)
            .ToList();

        if (enemies.Count == 0)
        {
            return;
        }

        Flash();

        // 所有敌人都使用触发前的 Amount。
        decimal currentDamage = Amount;

        foreach (Creature enemy in enemies)
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                currentDamage,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim,
                Owner);
        }

        // 伤害结算完成后，提高下一次触发的伤害。
        if (_damageAdd > 0)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                this,
                _damageAdd,
                Owner,
                cardPlay.Card);
        }
    }

    /// <summary>
    /// 持有者所在阵营回合结束：
    /// 1. 清空连续出牌记录；
    /// 2. 关闭【连连看】提供的临时启用状态；
    /// 3. 保留 Amount，确保累计伤害延续至本场战斗之后的回合。
    /// </summary>
    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 用 Owner 所在阵营是否结束回合来判断，
        // 而不是依赖 participants 集合的对象身份/内容。
        if (side != Owner.Side)
        {
            return Task.CompletedTask;
        }

        _lastCardId = null;
        _activeThisTurn = false;

        // 不移除 Power：
        // 即使当前没有常驻效果，也必须保留本场战斗累计的 Amount。
        return Task.CompletedTask;
    }
}

