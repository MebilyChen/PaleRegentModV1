using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 诅咒特质工具（机制文档：新增卡牌名词 Folly / Shame / Regret / Doubt）。
/// 语义参考原版君王之剑（Sovereign Blade / ForgeCmd.Forge）：
/// 「此牌生成时，将你所有的 XX 加入手牌（若没有则添加一张）」——
/// 即战斗中若已存在该诅咒（未消耗），只把它们全部移回手牌，绝不重复生成；
/// 只有一张都不存在时才生成一张新的，避免满手诅咒。
/// </summary>
public static class CurseTraitHelper
{
    /// <summary>
    /// 召集诅咒：把玩家战斗内所有未消耗的 T 移回手牌；若一张都没有则生成一张加入手牌。
    /// </summary>
    public static async Task Summon<T>(Player player) where T : CardModel
    {
        if (player.PlayerCombatState == null || player.Creature.CombatState == null)
        {
            return;
        }

        // 参考 ForgeCmd.GetSovereignBlades：排除复制体与已消耗的
        List<T> existing = player.PlayerCombatState.AllCards
            .Where(c => !c.IsDupe && c.Pile?.Type != PileType.Exhaust)
            .OfType<T>()
            .ToList();

        if (existing.Count == 0)
        {
            // 一张都没有：生成一张新的加入手牌
            T card = player.Creature.CombatState.CreateCard<T>(player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            return;
        }

        // 已存在：把不在手牌里的全部移回手牌，不生成新的
        foreach (T card in existing.Where(c => c.Pile?.Type != PileType.Hand))
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
