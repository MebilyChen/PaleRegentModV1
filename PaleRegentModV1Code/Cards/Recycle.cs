using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【再利用】罕见技能牌（垃圾回收）。
/// 2 灵魂：将手牌中的全部状态牌变化为【集中】。【消耗】。
/// 升级后：变化为【集中+】。
///
/// 定位：把【回避】塞进来的"虚空"、感染等状态牌变废为宝
/// （集中 = 0费 5格挡 抽1 +1灵魂 保留/消耗，见 Cards/Focus.cs）。
///
/// 机制要点：
/// - 不选牌：直接遍历手牌中 CardType.Status 的牌全部转化（表格："全部状态牌"）。
/// - CardCmd.TransformTo&lt;Focus&gt;：战斗内变形；升级后再 CardCmd.Upgrade 变为集中+
///   （写法参考 modstudy HornetDeepElegy/ArchitectPower）。
///
/// 修改指南：
/// - 想改转化目标：把 TransformTo&lt;Focus&gt; 的泛型参数换成别的卡牌类。
/// </summary>
public class Recycle() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Focus>(IsUpgraded)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 手牌中全部状态牌（不选牌，全部转化；排除自身以防意外）
        List<CardModel> statuses = CardPile.GetCards(Owner, PileType.Hand)
            .Where((CardModel c) => c.Type == CardType.Status && c != this)
            .ToList();

        // 把状态牌变形为【集中】（升级后为集中+）
        foreach (CardModel card in statuses)
        {
            await CardCmd.TransformTo<Focus>(card);
        }

        // 升级后：把刚变出来的集中升级为集中+
        if (IsUpgraded)
        {
            foreach (CardModel card in CardPile.GetCards(Owner, PileType.Hand)
                         .Where((CardModel c) => c is Focus && !c.IsUpgraded))
            {
                CardCmd.Upgrade(card, (CardPreviewStyle)1); // 枚举值同 modstudy 反编译写法
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：转化目标变为【集中+】（OnPlay 里用 IsUpgraded 判断）
    }
}
