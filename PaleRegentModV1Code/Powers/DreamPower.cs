using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【入梦】buff（机制文档：新增正面效果）。
/// 效果：免疫 [层数] 次伤害，每免疫一次层数 -1。
/// 与白根的区别：白根回合开始会把剩余层数转化为回血并消失，入梦是纯免疫、
/// 层数不会自动消失（用完为止），可以给敌我任何生物挂。
///
/// 实现仿原版 BufferPower / 本 mod WhiteRootPower 的免疫部分。
/// </summary>
public class DreamPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner)
        {
            return amount;
        }
        return 0m;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        Flash();
        await PowerCmd.Decrement(this);
    }
}
