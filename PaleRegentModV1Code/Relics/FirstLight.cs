using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 第一道光（First Light）—— 事件/升级遗物占位（机制文档：欧洛巴斯之触升级线）。
/// 效果（占位）：每回合开始时，无视虚空，直接恢复至灵魂上限。
///
/// 实现说明：AfterEnergyReset 里把能量补回上限。因为苍白信物的
/// "按虚空扣灵魂"同样挂在 AfterEnergyReset，钩子按遗物获得顺序执行，
/// 本遗物获得时间一定晚于初始遗物，所以会在扣除之后再补满，净效果
/// 即"回合开始恢复的灵魂不再受虚空影响"。
/// 欧洛巴斯之触的具体升级流程（事件/条件）后续再接。
/// </summary>
public class FirstLight : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner)
        {
            return;
        }
        decimal missing =  (player.PlayerCombatState?.MaxEnergy ?? 0) - (player.PlayerCombatState?.Energy ?? 0);
        if (missing <= 0)
        {
            return;
        }
        Flash();
        await PlayerCmd.GainEnergy(missing, player);
    }
}
