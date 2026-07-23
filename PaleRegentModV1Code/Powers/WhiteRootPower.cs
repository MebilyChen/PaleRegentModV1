using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 白根（White Root）buff：
/// 1. 免疫等同于层数次数的伤害（每免疫一次伤害减少一层），参考原版 BufferPower：
///    - ModifyHpLostAfterOstyLate 把即将受到的 HP 损失改为 0（用 Late 是为了让其他减伤先结算，
///      如果别的效果已把伤害降到 0，本 Power 就不会白白消耗层数）；
///    - AfterModifyingHpLostAfterOsty 在实际拦截了一次伤害后扣一层。
/// 2. 你的回合开始时，恢复等同于当前层数的生命，然后整个 buff 消失
///    （即"上回合没被破防，下回合就把剩余层数转化为回血"）。
///    挂点用 AfterEnergyReset（玩家回合开始能量恢复后），与 VoidPower 一致。
/// </summary>
public class WhiteRootPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // ---- 免疫伤害部分（仿 BufferPower）----
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

    // ---- 回合开始回血后消失部分 ----
    public override async Task AfterEnergyReset(Player player)
    {
        // 只在持有者本人的回合开始时触发
        if (player != Owner.Player)
        {
            return;
        }

        if (Amount > 0 && !Owner.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(Owner, Amount);
        }

        await PowerCmd.Remove(this);
    }
}
