using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空】状态牌（负面卡）。
/// 1 灵魂：无效果。【消耗】。不可升级。 0816更新：获得1点虚空。
///
/// 定位：由【试炼】等卡牌生成塞进弃牌堆的"垃圾牌"，
/// 占用抽牌与手牌位置；可用【再利用】（Recycle）转化为【聚焦】变废为宝。
///
/// 命名说明：类名叫 TheVoidStatus 而不是 Void，
/// 一是避免与 C# 关键字 void / 系统类型冲突，
/// 二是与副资源"虚空"（VoidResource）区分开。
/// 本地化 key：PALEREGENTMODV1-THE_VOID_STATUS。
/// </summary>
public class TheVoidStatus() : PaleRegentModV1Card(1,
    CardType.Status, CardRarity.Status,
    TargetType.None)
{
    // 不可升级（状态牌）
    public override int MaxUpgradeLevel => 0;
    private const int BaseVoid = 1;

    // 打出后消耗（防止无限循环打出垃圾牌）
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [ModHoverTips.VoidCounter];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /*protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 无任何效果——这张牌的"效果"就是浪费你 1 点灵魂和一个手牌位
        return Task.CompletedTask;
    }*/
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得1点虚空（能量）
        await VoidResource.Gain(Owner, BaseVoid);
        await VoidResource.SyncPower(choiceContext, Owner, this);
    }

    protected override void OnUpgrade()
    {
        // 不可升级，留空
    }
}
