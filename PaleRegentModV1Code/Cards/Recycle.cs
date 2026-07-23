using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【再利用】罕见技能牌（垃圾回收）。
/// 2 灵魂：选择手牌中的一张状态牌【虚空】，将它转化为【聚焦】。【消耗】。
///
/// 定位：把【试炼】等塞进来的"虚空"状态牌变废为宝
/// （聚焦 = 0费 5格挡 +1灵魂 保留/消耗，见 Cards/Focus.cs）。
///
/// 机制要点：
/// - CardSelectCmd.FromHand 用 filter 只允许选 TheVoidStatus；
///   手牌里没有"虚空"时选择列表为空，选牌界面直接跳过，牌照常打出（只亏 2 费）。
/// - CardCmd.TransformTo&lt;Focus&gt;：战斗内变形（仅本场战斗有效）。
///
/// 修改指南：
/// - 想改转化目标：把 TransformTo&lt;Focus&gt; 的泛型参数换成别的卡牌类。
/// - 选牌提示文案：cards.json 的 PALEREGENTMODV1-RECYCLE.selectionScreenPrompt。
/// </summary>
public class Recycle() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 从手牌选择 1 张"虚空"状态牌（filter 限定 TheVoidStatus 类型）
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            (CardModel c) => c is TheVoidStatus,
            this);

        // 把选中的牌变形为【聚焦】
        foreach (CardModel card in selected.ToList())
        {
            await CardCmd.TransformTo<Focus>(card);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级方案待定：可降费（2→1）或去掉【消耗】。
    }
}
