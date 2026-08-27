using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【誓卫】能力牌（机制文档：瘟疫流附属防御向）。
/// 0 灵魂 能力：每回合你第一次失去生命时，获得 3 点格挡
/// （Power 按层数给格挡，卡牌施加 3 层）。
/// 升级后：5 点格挡（施加 5 层）。
/// 20260725 批次：数值按表格 G42/H42 从 10/13 调为 3/5。
/// </summary>
public class BulwarkOath() : PaleRegentModV1Card(0,
    CardType.Power, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseStacks = 3;
    private const int UpgradedStacks = 5;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    //protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    //    [HoverTipFactory.FromPower<BulwarkOathPower>((int?)null)];
    
    // 带 Defend 标签：与"对防御牌生效"的效果联动（原版惯例）
    //protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BulwarkOathPower>(BaseStacks)];
    
    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BulwarkOathPower>(choiceContext, Owner.Creature,
            DynamicVars["BulwarkOathPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BulwarkOathPower"].UpgradeValueBy(UpgradedStacks - BaseStacks);
    }
}
