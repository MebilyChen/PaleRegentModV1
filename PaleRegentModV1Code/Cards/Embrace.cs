using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【拥抱瘟疫】（表格 C#13，20260725 新增）。
/// 1 灵魂 技能/Uncommon：抽 1 张牌。抽到【感染】时，为你添加 1 层【瘟疫】
/// ，并随机打出你抽牌堆里的 1 张攻击牌。
/// 升级：费用 1 → 0。
///
/// 实现说明：
/// - "抽到感染"判定：抽牌前记录手牌快照，抽牌后取差集检查是否为感染。
/// - "随机打出抽牌堆攻击牌"：随机取一张 CardType.Attack，用 CardCmd.AutoPlay
///   自动打出（随机目标，与 modstudy Havoc 式用法一致）。
/// </summary>
public class Embrace() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.None)
{
    /// <summary>抽牌数。</summary>
    private const int DrawCount = 1;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Infection>(false),
         HoverTipFactory.FromPower<PlaguePower>((int?)null)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 抽 1 张牌（抽牌前后对比手牌，判定是否抽到感染）
        HashSet<CardModel> before = CardPile.GetCards(Owner, PileType.Hand).ToHashSet();
        await CardPileCmd.Draw(choiceContext, DrawCount, cardPlay.Player);
        IEnumerable<CardModel> drawn =
            CardPile.GetCards(Owner, PileType.Hand).Where(c => !before.Contains(c));

        // 2. 抽到感染 → 为自己添加 1 层瘟疫
        foreach (CardModel card in drawn)
        {
            if (card is Infection)
            {
                await PowerCmd.Apply<PlaguePower>(
                    choiceContext, cardPlay.Player.Creature, 1,
                    cardPlay.Player.Creature, this);
                
                // 3. 随机打出抽牌堆里的 1 张攻击牌
                List<CardModel> attacks = CardPile.GetCards(Owner, PileType.Draw)
                    .Where(c => c.Type == CardType.Attack)
                    .ToList();
                if (attacks.Count > 0)
                {
                    CardModel pick = Owner.RunState.Rng.CombatTargets.NextItem(attacks);
                    await CardCmd.AutoPlay(choiceContext, pick, (Creature?)null, (AutoPlayType)1, false, false);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：费用 1 → 0
        EnergyCost.UpgradeBy(-1);
    }
}
