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
/// 【连锁共鸣】buff。
///
/// Amount 表示“下一次触发时造成的伤害与获得的格挡”，而不是层数。
///
/// 效果：
/// 连续打出同名牌时，从第 2 张起，对所有敌人造成 Amount 点伤害，
/// 并获得 Amount 点格挡；每触发一次，Amount 增加 DamageAdd。
///
/// 两种启用方式：
/// - 【连连看 C#62】：仅本回合启用；
/// - 【连锁反应 C#71】：整场战斗启用。
///
/// 即使当前没有启用，Power 也不会被移除，因为 Amount 需要保存
/// 本场战斗内已经累计的伤害与格挡数值。
/// </summary>
public class ChainResonancePower : PaleRegentModV1Power
{
    /// <summary>【连连看】提供的本回合启用状态。</summary>
    private bool _activeThisTurn;

    /// <summary>【连锁反应】提供的整场战斗启用状态。</summary>
    private bool _activeForCombat;

    /// <summary>每次效果触发后增加的伤害与格挡。</summary>
    private decimal _damageAdd;

    /// <summary>
    /// 当前回合通过重复使用【连连看】临时增加的伤害与格挡总量。
    /// 回合结束时会从 Amount 中扣除，不影响连锁触发带来的永久成长。
    /// </summary>
    private decimal _temporaryDamageThisTurn;

    /// <summary>最近一次提供临时伤害与格挡的卡牌。</summary>
    private CardModel? _temporaryDamageSource;

    /// <summary>上一张由持有者打出的牌的 ModelId；同一个 ModelId 视为同名牌。</summary>
    private ModelId? _lastCardId;

    public override PowerType Type => PowerType.Buff;

    // Amount 需要在 UI 上正常显示数字，并能通过 ModifyAmount 正确刷新。
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>当前是否应该监听连续同名牌。</summary>
    private bool IsActive => _activeThisTurn || _activeForCombat;

    /// <summary>
    /// 仅在效果启用时显示图标。
    /// 【连连看】回合结束后会将 _activeThisTurn 设为 false，因而图标消失，
    /// 但 Power 实例及其 Amount 仍会保留，以保存本场战斗的累计数值。
    /// 若【连锁反应】已启用 _activeForCombat，图标会持续显示。
    /// </summary>
    protected override bool IsVisibleInternal => IsActive;

    /// <summary>由【连连看】调用：本回合启用效果。</summary>
    public void ActivateForTurn(decimal initialDamage, decimal damageAdd)
    {
        _activeThisTurn = true;
        UpdateDamageAdd(damageAdd);
        EnsureDamageInitialized(initialDamage);
    }

    /// <summary>由【连锁反应】调用：整场战斗启用效果。</summary>
    public void ActivateForCombat(decimal initialDamage, decimal damageAdd)
    {
        _activeForCombat = true;
        UpdateDamageAdd(damageAdd);
        EnsureDamageInitialized(initialDamage);
    }

    /// <summary>确保 Amount 在首次启用时有一个大于 0 的初始值。</summary>
    private void EnsureDamageInitialized(decimal initialDamage)
    {
        if (Amount <= 0m && initialDamage > 0m)
        {
            SetAmount((int)initialDamage);
        }
    }

    /// <summary>更新每次触发后的伤害与格挡增量，始终保留较高的值。</summary>
    private void UpdateDamageAdd(decimal damageAdd)
    {
        if (damageAdd > _damageAdd)
        {
            _damageAdd = damageAdd;
        }
    }

    /// <summary>
    /// 在现有 Amount 上增加仅持续到本回合结束的临时伤害与格挡数值。
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

        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            amount,
            Owner,
            sourceCard);

        _temporaryDamageThisTurn += amount;
        _temporaryDamageSource = sourceCard;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // 用卡牌的实际所有者判断，不依赖 cardPlay.Player 的构造方式。
        if (cardPlay.Card.Owner.Creature != Owner)
        {
            return;
        }

        ModelId currentCardId = cardPlay.Card.Id;
        bool isChain =
            _lastCardId != null &&
            _lastCardId.Equals(currentCardId);

        // 无论 Power 当前是否启用，都记录持有者打出的这张牌。
        _lastCardId = currentCardId;

        // Power 未启用或未连续打出同名牌时不触发。
        if (!IsActive || !isChain)
        {
            return;
        }

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null || Amount <= 0m)
        {
            return;
        }

        List<Creature> enemies = combatState
            .GetOpponentsOf(Owner)
            .Where(creature => creature.IsAlive)
            .ToList();

        Flash();

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

        // 每次连续同名牌触发，在造成全体伤害后获得与 Amount 等额的格挡。
        await CreatureCmd.GainBlock(
            Owner,
            new BlockVar(currentDamage, ValueProp.Move),
            cardPlay);

        // 每触发一次，Amount 增加 DamageAdd；该增量同时影响后续伤害与格挡。
        if (_damageAdd > 0m)
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
    /// 持有者所在阵营回合结束：清空连续出牌记录、关闭本回合临时启用状态，
    /// 并移除【连连看】在本回合提供的临时伤害与格挡数值。
    /// </summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
        {
            return;
        }

        _lastCardId = null;
        _activeThisTurn = false;

        decimal temporaryDamage = _temporaryDamageThisTurn;
        CardModel? temporaryDamageSource = _temporaryDamageSource;

        if (temporaryDamage > 0m)
        {
            decimal removableDamage =
                temporaryDamage > Amount
                    ? Amount
                    : temporaryDamage;

            if (removableDamage > 0m)
            {
                if (temporaryDamageSource is not null)
                {
                    await PowerCmd.ModifyAmount(
                        choiceContext,
                        this,
                        -removableDamage,
                        Owner,
                        temporaryDamageSource);
                }
                else
                {
                    SetAmount((int)(Amount - removableDamage));
                }
            }
        }

        _temporaryDamageThisTurn = 0m;
        _temporaryDamageSource = null;
    }
}
