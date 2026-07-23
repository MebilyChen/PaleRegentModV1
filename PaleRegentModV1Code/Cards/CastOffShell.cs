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
/// 【弃壳】技能牌（机制文档：弃壳系列，占位设计）。
/// 1 灵魂 技能：获得 8 点格挡；下回合开始时获得 1 灵魂
/// （蜕下旧壳，换取喘息）。消耗。
/// 升级后：格挡 +3。
/// </summary>
public class CastOffShell() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    private const int BaseBlock = 8;
    private const int UpgradeBlockBonus = 3;
    private const int SoulNextTurn = 1;

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<SoulNextTurnPower>(choiceContext, Owner.Creature, SoulNextTurn, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
