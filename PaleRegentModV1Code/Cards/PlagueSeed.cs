using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【疫种】技能牌（机制文档：瘟疫流）。
/// 0 灵魂 技能：将 2 张【感染】加入手牌，获得 1 点灵魂。
/// 升级后：获得 2 点灵魂。
/// （感染生成走 Infection.NotifyGenerated 统一入口，触发疫蔓。）
/// </summary>
public class PlagueSeed() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    private const int BaseInfections = 2;
    private const int BaseEnergyGain = 1;

    /// <summary>升级后额外灵魂。</summary>
    private int _energyBonus;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.AddToCombatAndPreview<Infection>(Owner.Creature, PileType.Hand, BaseInfections, Owner);
        await Infection.NotifyGenerated(Owner.Creature, BaseInfections);

        await PlayerCmd.GainEnergy(BaseEnergyGain + _energyBonus, cardPlay.Player);
    }

    protected override void OnUpgrade()
    {
        _energyBonus = 1;
    }
}
