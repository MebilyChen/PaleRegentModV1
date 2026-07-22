using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 虚空（Void）Power：作为虚空数量的可视化 buff 挂在玩家身上。
/// 机制：回合开始能量恢复完成后（AfterEnergyReset 钩子），
/// 扣除等同于当前虚空数量的灵魂（能量），最低扣到 0。
///
/// 注意：
/// 1. STS2 没有 STS1 的 AtStartOfTurn()，回合开始扣能量的官方标准挂点是
///    AfterEnergyReset(Player)，参考原版 EnergyNextTurnPower 的实现。
/// 2. Power 的 Owner 是 Creature 类型而不是 Player，
///    要通过 Owner.Player / Owner.IsPlayer 访问玩家。
/// 3. Power 由 ModelDb 反射创建，必须保留无参构造函数；
///    数量通过 PowerCmd.Apply(power, amount, ...) 设置。
/// </summary>
public class VoidPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        // 只处理该 Power 持有者本人的回合
        if (player != Owner.Player)
        {
            return;
        }

        // 以副资源中的虚空数量为准（Power 的 Amount 仅作展示，双轨时可能不同步）
        int voidAmount = VoidResource.Get(player);
        if (voidAmount <= 0)
        {
            return;
        }

        // 扣除等量灵魂（能量），PlayerCmd.LoseEnergy 内部会 Clamp 到 0
        await PlayerCmd.LoseEnergy(voidAmount, player);
    }
}
