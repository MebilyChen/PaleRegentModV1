using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 第一道光（First Light）—— 事件/升级遗物占位（机制文档：欧洛巴斯之触升级线）。
///
/// 效果：
/// 每回合开始时，无视虚空，直接恢复至灵魂上限。
/// 灵魂上限[blue]+1[/blue]。
///
/// 实现说明：
/// 1. ModifyMaxEnergy 使灵魂上限永久增加 1。
/// 2. AfterEnergyReset 在苍白信物扣除灵魂后，将灵魂恢复至上限。
/// 3. 同时移除 VoidPower，使虚空数量不再显示。
/// </summary>
public class FirstLight : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    /// <summary>
    /// 灵魂系统使用 Energy，因此最大能量 +1 即灵魂上限 +1。
    /// </summary>
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != Owner)
        {
            return amount;
        }

        return amount + 1m;
    }

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner)
        {
            return;
        }

        /*
         * VoidPower 本身就是显示在玩家状态栏中的虚空计数。
         * 第一道光获得时间晚于苍白信物，所以苍白信物先根据
         * VoidPower 扣除灵魂，然后这里再移除 VoidPower。
         */
        await PowerCmd.Remove<VoidPower>(player.Creature);

        decimal missing =
            (player.PlayerCombatState?.MaxEnergy ?? 0m)
            - (player.PlayerCombatState?.Energy ?? 0m);

        if (missing <= 0m)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(missing, player);
    }
}