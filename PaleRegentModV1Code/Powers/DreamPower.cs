using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【入梦】buff（机制文档：新增正面效果，表格设计版——数值型免疫）。
/// 效果：免疫 [层数] 以内的伤害值（伤害 ≤ 层数 → 完全免疫，层数保留）；
/// 受到高于此值的伤害时，降低 [层数] 点伤害，然后解除该效果（移除全部层数）。
/// 例：5 层入梦，受 4 点伤害 → 免疫且层数保留；受 6 点伤害 → 实受 1 点并解除。
///
/// 与白根的区别：白根是次数型免疫 + 回合开始转化回血；入梦是数值型减免，
/// 可以给敌我任何生物挂。
///
/// 实现说明：ModifyHpLostAfterOstyLate 做数值判定并用 _pendingRemove 标记
/// 是否穿透；AfterModifyingHpLostAfterOsty 里按标记决定移除还是保留。
/// </summary>
public class DreamPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>本次伤害是否穿透入梦（伤害 &gt; 层数），穿透则事后解除。</summary>
    private bool _pendingRemove;

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m)
        {
            return amount;
        }
        if (amount <= Amount)
        {
            // 层数以内：完全免疫，层数保留
            _pendingRemove = false;
            return 0m;
        }
        // 高于层数：降低 [层数] 点伤害，随后解除
        _pendingRemove = true;
        return amount - Amount;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        Flash();
        if (_pendingRemove)
        {
            _pendingRemove = false;
            await PowerCmd.Remove(this);
        }
    }
}
