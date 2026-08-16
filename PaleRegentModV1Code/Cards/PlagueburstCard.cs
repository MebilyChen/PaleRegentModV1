using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【瘟疫爆发】能力牌（表 C#60，0727 新增）。
/// 3 灵魂：每当你生成一张【感染】，对所有敌人造成 5 点伤害。生成1张感染。
/// 升级后：8 点伤害。
/// </summary>
public class PlagueburstCard() : PaleRegentModV1Card(3,
    CardType.Power, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseDamage = 3;
    private const int UpgradeDamageBonus = 2;
    private const int BaseInfections = 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Infection>(false)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PlagueburstPower>(BaseDamage)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PlagueburstPower>(choiceContext, Owner.Creature,
            DynamicVars["PlagueburstPower"].BaseValue, Owner.Creature, this);
        //生成1张
        await CardPileCmd.AddToCombatAndPreview<Infection>(Owner.Creature, PileType.Hand, BaseInfections, Owner);
        await Infection.NotifyGenerated(Owner.Creature, BaseInfections);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PlagueburstPower"].UpgradeValueBy(UpgradeDamageBonus);
    }
}
