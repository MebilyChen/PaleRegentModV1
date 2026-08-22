using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 虚空之心（Void Heart）—— 欧洛巴斯之触将国王之魂替换后的终局遗物。
/// 每回合开始时获得等同于灵魂上限的虚空。
///
/// 虚空数值继续由 VoidResource 保存，但虚空之心持有者不再显示 VoidPower 图标；
/// 这是因为国王之魂已被替换，VoidPower 不再作为该遗物机制的可视化状态。
/// </summary>
public class VoidHeart : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <summary>
    /// 欧洛巴斯之触使用 RelicCmd.Replace 替换遗物时，新遗物也会触发此回调。
    /// 这里清除国王之魂时期残留的 VoidPower，隐藏其图标但不修改 VoidResource 的数值。
    /// </summary>
    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner.Creature != null)
        {
            await PowerCmd.Remove<VoidPower>(Owner.Creature);
        }
    }

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner)
        {
            return;
        }

        Flash();
        await VoidResource.Gain(player, player.PlayerCombatState?.MaxEnergy ?? 0);

        // 刻意不调用 VoidResource.SyncPower：
        // 虚空之心阶段不再显示 VoidPower 图标。
    }
}