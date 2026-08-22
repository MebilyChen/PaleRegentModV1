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
/// 1 灵魂 能力：每当你生成一张【感染】，对场上所有生物施加 1 层【瘟疫】。将 1 张【感染】加入手牌。
/// 升级后：改为 2 层。将 2 张【感染】加入手牌
/// </summary>
public class PlagueSpread() : PaleRegentModV1Card(1,
    CardType.Power, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int PlaguePerInfection = 1;
    private const int BaseInfections = 1;
    private const int UpgradeInfectionsBonus = 1;

    private int InfectionsToGenerate =>
        BaseInfections + (IsUpgraded ? UpgradeInfectionsBonus : 0);

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PlagueSpreadPower>((int?)null),
         HoverTipFactory.FromCard<Infection>(false),
         HoverTipFactory.FromPower<PlaguePower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PlagueSpreadPower>(PlaguePerInfection)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 未升级：每张感染施加 1 层瘟疫；升级后：每张感染施加 2 层瘟疫。
        await PowerCmd.Apply<PlagueSpreadPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["PlagueSpreadPower"].BaseValue,
            Owner.Creature,
            this);

        // 未升级：生成 1 张感染；升级后：生成 2 张感染。
        await CardPileCmd.AddToCombatAndPreview<Infection>(
            Owner.Creature,
            PileType.Hand,
            InfectionsToGenerate,
            Owner);

        // 统计值必须与实际生成数量一致。
        await Infection.NotifyGenerated(Owner.Creature, InfectionsToGenerate);
        //await Infection.NotifyGenerated(Owner.Creature, BaseInfections);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PlagueSpreadPower"].UpgradeValueBy(1m);
    }
}
