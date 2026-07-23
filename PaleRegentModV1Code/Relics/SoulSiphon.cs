using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 灵魂虹吸（Soul Siphon）—— 稀有遗物（机制文档未命名，占位名）。
/// 效果：你每花费 1 点灵魂（能量），获得 1 点虚空。
///
/// 实现说明：挂 AfterStarsSpent 钩子（stars = 灵魂/能量），
/// 打牌、事件等所有灵魂花费入口都会走这里。
/// 注意：虚空增加会让下回合恢复的灵魂变少（苍白信物机制），
/// 这个遗物本质是"透支未来换当前虚空"，数值强度你后面自己调。
/// </summary>
public class SoulSiphon : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterStarsSpent(int amount, Player spender)
    {
        if (spender != Owner || amount <= 0)
        {
            return;
        }
        Flash();
        await VoidResource.Gain(Owner, amount);
        await VoidResource.SyncPower(new ThrowingPlayerChoiceContext(), Owner, null);
    }
}
