using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 【国王印记 King's Brand】稀有遗物（机制表：遗物 R#9，0727 新增）。
/// 效果：每回合开始时，牌堆（抽牌堆、弃牌堆、消耗牌堆）中的一张随机造物牌
/// 回到你的手牌。
///
/// 实现说明：
/// - 挂 AfterPlayerTurnStart（同 MouldRelic / 原版 GamblingChip），
///   每个"你的回合"开始时都触发一次。
/// - "造物牌"的判定统一走 PaleRegentModV1Card.IsCreationCard 虚属性
///   （见卡牌基类注释）。当前 8 种：容器 Vessel / 纯粹容器 PureVessel /
///   失败容器 FailedVessel / 失败实验 FailedExperiment /
///   虚空化形 VoidGivenForm / 虚空化神 VoidGivenFocus /
///   国王俑卫 KingsRetainer / 有翼俑卫 WingedRetainerCard，
///   与表格效果栏列出的 8 种一一对应；以后新增造物牌只要 IsCreationCard
///   为 true 就会自动被本遗物覆盖，无需改这里。
/// - 随机源用 Owner.RunState.Rng.CombatTargets（战斗随机流，同 EliteRecall），
///   保证联机/回放一致性。
/// - 回手命令 CardPileCmd.Add(card, PileType.Hand, ...)（同 EliteRecall），
///   手牌满时由引擎按标准规则处理（溢出进弃牌堆）。
/// </summary>
public class KingsBrand : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    /// <summary>本遗物检索的三个牌堆（抽牌堆、弃牌堆、消耗牌堆）。</summary>
    private static readonly PileType[] SearchPiles =
    [
        PileType.Draw,
        PileType.Discard,
        PileType.Exhaust,
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        List<CardModel> creations = CardPile.GetCards(Owner, SearchPiles)
            .Where(c => c is PaleRegentModV1Card { IsCreationCard: true })
            .ToList();
        if (creations.Count == 0)
        {
            return;
        }

        Flash();
        CardModel pick = Owner.RunState.Rng.CombatTargets.NextItem(creations);
        await CardPileCmd.Add(pick, PileType.Hand, CardPilePosition.Top, null, false);
    }
}
