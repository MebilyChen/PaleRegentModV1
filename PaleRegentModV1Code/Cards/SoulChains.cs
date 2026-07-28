using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂锁链】技能牌（表 C#79，0727 新增）。
/// 1 灵魂：获得 7 点格挡，获得 1 点虚空。
/// 升级后：10 点格挡。
/// </summary>
public class SoulChains() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    private const int BaseBlock = 7;
    private const int UpgradeBlockBonus = 3;
    private const int VoidGain = 1;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VoidPower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        await VoidResource.Gain(cardPlay.Player, VoidGain);
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
