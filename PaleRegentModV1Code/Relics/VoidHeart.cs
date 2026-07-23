using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 虚空之心（Void Heart）—— 事件/升级遗物占位（机制文档：欧洛巴斯之触升级线）。
/// 效果（占位）：每回合开始时，获得等同于灵魂上限的虚空。
///
/// 注意：配合苍白信物的"按虚空扣灵魂"，这会让每回合灵魂几乎归零、
/// 全部转成虚空——即"彻底虚空化"的终局形态占位。
/// 欧洛巴斯之触的具体升级流程（事件/条件）后续再接。
/// </summary>
public class VoidHeart : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner)
        {
            return;
        }
        Flash();
        await VoidResource.Gain(player, player.PlayerCombatState?.MaxEnergy ?? 0);
        await VoidResource.SyncPower(new ThrowingPlayerChoiceContext(), player, null);
    }
}
