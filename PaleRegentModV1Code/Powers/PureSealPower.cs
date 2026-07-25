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
/// 【纯粹封印】debuff（机制文档：效果表 P#3）。
/// 效果：[层数] 回合内，持有者每回合的第一次攻击不造成伤害
///       （注意：是把攻击者的伤害设置为 0，不是格挡）。
/// 每经过持有者一方的回合结束，层数 -1；层数归零后消失。
///
/// ============ BUG 修复说明（20260725 批次） ============
/// 旧实现在 ModifyDamageMultiplicative 里直接修改 _sealedThisTurn 状态，
/// 但该钩子会在"伤害预览"阶段被反复调用（悬停卡牌显示预期伤害时也会算），
/// 导致预览就把"本回合已封印"标记消耗掉，实际结算时封不住/封错段。
/// 新实现改为"只读 + 结算后置位"模式：
/// - ModifyDamageMultiplicative 只读取 _usedThisTurn，不修改任何状态
///   （未触发时返回 0：预览和结算都显示伤害为 0，符合玩家预期）；
/// - AfterDamageGiven（只在真正结算后触发，预览不会调用）里把
///   _usedThisTurn 置 true —— 第一段伤害结算完立即生效，
///   因此多段攻击只有第一段被封为 0，后续段正常，与设计一致。
/// </summary>
public class PureSealPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>本回合是否已经封印过一次攻击（只在 AfterDamageGiven 里置位）。</summary>
    private bool _usedThisTurn;

    /// <summary>
    /// 只读判定：本回合尚未封印过 → 持有者的攻击伤害 ×0。
    /// 不要在这里修改任何字段（预览阶段会反复调用此钩子）。
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner)
        {
            return 1m;
        }
        if (!props.IsPoweredAttack())
        {
            return 1m;
        }
        return _usedThisTurn ? 1m : 0m;
    }

    /// <summary>
    /// 伤害真正结算后触发（预览不会调用）：
    /// 持有者的第一次攻击结算完毕 → 标记"本回合已封印"。
    /// 多段攻击的第一段结算后立即置位，后续段在 Modify 里返回 1m 正常结算。
    /// </summary>
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (_usedThisTurn || dealer != Owner || !props.IsPoweredAttack())
        {
            return;
        }
        _usedThisTurn = true;
        Flash();
        await Task.CompletedTask;
    }

    /// <summary>持有者一方回合结束：层数 -1，复位"本回合已封印"标记。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }
        _usedThisTurn = false;
        await PowerCmd.Decrement(this);
    }
}
