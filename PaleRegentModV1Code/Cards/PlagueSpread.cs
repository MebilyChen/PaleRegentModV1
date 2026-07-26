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
/// 【疫蔓】能力牌（机制文档：瘟疫流）。
/// 1 灵魂 能力：每当你生成一张【感染】，对场上所有生物施加 1 层【瘟疫】。
/// 升级后：改为 3 层。
/// </summary>
public class PlagueSpread() : PaleRegentModV1Card(1,
    CardType.Power, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int PlaguePerInfection = 1;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PlagueSpreadPower>((int?)null),
         HoverTipFactory.FromCard<Infection>(false),
         HoverTipFactory.FromPower<PlaguePower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PlagueSpreadPower>(PlaguePerInfection)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PlagueSpreadPower>(choiceContext, Owner.Creature,
            DynamicVars["PlagueSpreadPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PlagueSpreadPower"].UpgradeValueBy(2m);
    }
}
