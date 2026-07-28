using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【蓄灵】buff（弃壳等卡施加，名词表 N#10）。
/// 效果：下回合开始时获得 [层数] 点灵魂（能量），随后消失。
/// 挂点用 AfterEnergyReset（能量重置之后再加，才不会被重置吞掉），
/// 与 VoidPower / WhiteRootPower 一致。
/// 20260727：持有【白沃姆摇篮】（WhiteWyrmCradlePower，C#97）时，
/// 供灵后不再移除自身（蓄灵不消失，每回合持续供灵）。
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
        // 20260727：白沃姆摇篮在场时蓄灵不消失（C#97）
        if (Owner.HasPower<WhiteWyrmCradlePower>())
        {
            return;
        }
        await PowerCmd.Remove(this);
    }
}
