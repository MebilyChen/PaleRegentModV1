using MegaCrit.Sts2.Core.Entities.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 虚空（Void）Power：作为虚空数量的可视化 buff 挂在玩家身上，仅做展示用。
///
/// 变更说明（遗物批次）：
/// “回合开始扣除等同虚空数量的灵魂”逻辑已从这里移到初始遗物
/// 苍白信物（PaleToken）/ 国王之魂（Kingsoul）的 AfterEnergyReset 中，
/// 因为机制文档定义该效果属于遗物（国王之魂还会少扣 1 点）。
/// 如果保留在这里会和遗物双重扣除。
///
/// 备忘：
/// 1. Power 的 Owner 是 Creature 类型而不是 Player，要通过 Owner.Player 访问玩家。
/// 2. Power 由 ModelDb 反射创建，必须保留无参构造函数；
///    数量通过 PowerCmd.Apply(power, amount, ...) 设置。
/// </summary>
public class VoidPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
