using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 永绽花（Everbloom Flower）—— 稀有遗物（机制文档：遗物区）。
/// 效果：每回合开始时，若你至少有 1 点虚空，将 1 点虚空转化为 1 点灵魂。
///
/// 挂点：AfterEnergyReset（能量重置之后），保证顺序在苍白信物的
/// "按虚空扣灵魂"之后——先扣后补，净效果即"1 虚空换 1 灵魂"。
/// </summary>
public class EverbloomFlower : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner)
        {
            return;
        }
        if (VoidResource.Get(player) < 1)
        {
            return;
        }
        Flash();
        await VoidResource.Spend(player, 1);
        await PlayerCmd.GainEnergy(1, player);
        await VoidResource.SyncPower(new ThrowingPlayerChoiceContext(), player, null);
    }
}
