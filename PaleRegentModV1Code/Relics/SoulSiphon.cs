using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 灵魂虹吸（Soul Siphon）。
/// 效果：持有者每实际支付 1 点能量，获得 1 点虚空。
/// </summary>
public class SoulSiphon : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    // AfterStarsSpent 仅在支付“Stars”这一独立资源时分发，
    // 常规卡牌能量费用走的是 AfterEnergySpent。
    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (card.Owner != Owner || amount <= 0)
        {
            return;
        }

        Flash();
        await VoidResource.Gain(Owner, amount);
        await VoidResource.SyncPower(new ThrowingPlayerChoiceContext(), Owner, null);
    }
}