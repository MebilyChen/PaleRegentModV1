using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【失败容器】生成牌（机制文档：造物流，容器吸收感染不足时孕育）。
/// 0 灵魂 技能：获得 4 点格挡，获得 1 点虚空（残次品也有残次品的用处）。消耗。
/// </summary>
public class FailedVessel() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Token,
    TargetType.Self)
{
    private const int BaseBlock = 4;
    private const int UpgradeBlockBonus = 2;
    private const int VoidGain = 1;

    public override bool IsCreationCard => true;
    public override bool GainsBlock => true;

    /// <summary>
    /// Shame 特质（君王之剑式）：此牌生成时，将你所有的 Shame 加入手牌（若没有则生成一张）。
    /// </summary>
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card == this)
        {
            await CurseTraitHelper.Summon<Shame>(Owner);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await VoidResource.Gain(Owner, VoidGain);
        await VoidResource.SyncPower(choiceContext, Owner, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
