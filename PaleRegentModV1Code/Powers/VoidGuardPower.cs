using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【虚空护卫】buff（机制文档：新增正面效果，纯粹容器提供）。
/// 效果：免疫 [层数] 次伤害；每次免疫时，对伤害来源反弹等量伤害，
/// 并对其施加等同于被免疫伤害的【虚空之触】。
///
/// 实现说明（占位版）：
/// - 免疫走 ModifyHpLostAfterOstyLate（同白根/入梦），返回 0；
/// - 反弹与施加虚空之触放在 BeforeDamageReceived 里记录来源，
///   在 AfterModifyingHpLostAfterOsty 中执行（此时才能确认真的免疫了）。
/// - "等量"按来袭伤害的 HP 损失前数值（_incomingAmount）计算。
/// </summary>
public class VoidGuardPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private Creature? _lastDealer;
    private decimal _incomingAmount;

    /// <summary>记录这次攻击的来源和数值，供免疫结算后反击用。</summary>
    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner)
        {
            return;
        }
        _lastDealer = dealer;
        _incomingAmount = amount;
        await Task.CompletedTask;
    }

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner)
        {
            return amount;
        }
        return 0m;
    }

    /// <summary>免疫成功：层数 -1，反弹并施加虚空之触。</summary>
    public override async Task AfterModifyingHpLostAfterOsty()
    {
        Flash();
        Creature? dealer = _lastDealer;
        decimal amount = _incomingAmount;
        _lastDealer = null;
        _incomingAmount = 0m;

        await PowerCmd.Decrement(this);

        if (dealer == null || dealer == Owner || !dealer.IsAlive || amount <= 0)
        {
            return;
        }
        // 反弹等量伤害（不可被力量加成，跳过受击动画防止反复触发闪烁）
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), dealer, amount, ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner);
        // 施加等量【虚空之触】
        if (dealer.IsAlive)
        {
            await PowerCmd.Apply<VoidTouchPower>(new ThrowingPlayerChoiceContext(), dealer, amount, Owner, null);
        }
    }
}
