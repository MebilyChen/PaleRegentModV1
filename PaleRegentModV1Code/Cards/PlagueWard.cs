using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【疫佑】能力牌（机制文档：瘟疫流）。
/// 2 灵魂 2 虚空 能力：【瘟疫】的随机攻击不再命中你和你的队友。为你添加5层瘟疫。
/// 升级后：1 灵魂 1 虚空。
/// </summary>
public class PlagueWard : PaleRegentModV1Card
{
    private const int BaseVoidCost = 1;
    private const int BasePlague = 5;

    public PlagueWard() : base(2,
        CardType.Power, CardRarity.Rare,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, BaseVoidCost);
    }
    
    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PlaguePower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PlaguePower>(BasePlague)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PlagueWardPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        await PowerCmd.Apply<PlaguePower>(choiceContext, cardPlay.Player.Creature,
            DynamicVars["PlaguePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        //CardTraits.SetVoidCost(this, BaseVoidCost - 1);
    }
}
