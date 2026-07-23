using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【纯粹容器】生成牌（机制文档：造物流，容器吸收 3+ 张感染孕育的完全体）。
/// 0 灵魂 技能：获得 12 点格挡，获得 1 层【入梦】（免疫下一次伤害）。
/// 纯粹。消耗。
/// </summary>
public class PureVessel() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Special,
    TargetType.Self)
{
    private const int BaseBlock = 12;
    private const int UpgradeBlockBonus = 4;
    private const int DreamAmount = 1;

    public override bool IsCreationCard => true;
    public override bool IsPure => true;
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<DreamPower>(choiceContext, Owner.Creature, DreamAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
