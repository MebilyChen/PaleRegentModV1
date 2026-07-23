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
/// 【疫种】技能牌（机制文档：瘟疫流，占位命名）。
/// 0 灵魂 技能：将 2 张【感染】加入手牌，获得 1 点虚空。
/// 升级后：改为 3 张感染。
/// （感染生成走 Infection.NotifyGenerated 统一入口，触发疫蔓。）
/// </summary>
public class PlagueSeed() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    private const int BaseInfections = 2;
    private const int VoidGain = 1;

    /// <summary>升级后额外感染数。</summary>
    private int _bonusInfections;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = BaseInfections + _bonusInfections;
        await CardPileCmd.AddToCombatAndPreview<Infection>(Owner.Creature, PileType.Hand, count, Owner);
        await Infection.NotifyGenerated(Owner.Creature, count);

        await VoidResource.Gain(Owner, VoidGain);
        await VoidResource.SyncPower(choiceContext, Owner, this);
    }

    protected override void OnUpgrade()
    {
        _bonusInfections = 1;
    }
}
