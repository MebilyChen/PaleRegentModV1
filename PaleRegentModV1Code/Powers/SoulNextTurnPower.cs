using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【蓄灵】buff（占位卡"弃壳"施加）。
/// 效果：下回合开始时获得 [层数] 点灵魂（能量），随后消失。
/// 挂点用 AfterEnergyReset（能量重置之后再加，才不会被重置吞掉），
/// 与 VoidPower / WhiteRootPower 一致。
/// </summary>
public class SoulNextTurnPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }
        Flash();
        if (Amount > 0)
        {
            await PlayerCmd.GainEnergy(Amount, player);
        }
        await PowerCmd.Remove(this);
    }
}
