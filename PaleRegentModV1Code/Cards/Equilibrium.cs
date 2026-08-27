using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Patches;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【调和】技能牌（表 C#74，0727 新增）。
/// 1 灵魂 + 1 虚空，保留：获得 7(10) 点格挡。
/// - 若虚空 < 灵魂：获得 2 点虚空；
/// - 若虚空 > 灵魂：获得 2 点灵魂；
/// - 若相等：获得 3 点虚空和 3 点灵魂。
/// 升级后：3 / 3 / 4+4。
/// 备注：比较发生在支付费用之后（打出时以当前实时数值判断）。
/// </summary>
public class Equilibrium : PaleRegentModV1Card
{
    private const int VoidCost = 1;

    /// <summary>格挡值（基础 7，升级 10）。</summary>
    private const int BaseBlock = 7;
    private const int UpgradedBlock = 10;

    /// <summary>不相等时的获得量（升级后 3）。</summary>
    private int _minorGain = 2;

    /// <summary>相等时双资源各自的获得量（升级后 4）。</summary>
    private int _equalGain = 3;

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    public override bool GainsBlock => true;

    public Equilibrium() : base(
        1,
        CardType.Skill,
        CardRarity.Uncommon,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VoidPower>((int?)null)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(
            cardPlay.Player.Creature,
            DynamicVars.Block,
            cardPlay);

        int voidAmount = VoidResource.Get(cardPlay.Player);
        int soulAmount = cardPlay.Player.PlayerCombatState?.Energy ?? 0;

        if (voidAmount < soulAmount)
        {
            await VoidResource.Gain(cardPlay.Player, _minorGain);
            await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
        }
        else if (voidAmount > soulAmount)
        {
            await PlayerCmd.GainEnergy(_minorGain, cardPlay.Player);
            CombatCounters.NotifySoulGain(cardPlay.Player, _minorGain);
        }
        else
        {
            await VoidResource.Gain(cardPlay.Player, _equalGain);
            await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
            await PlayerCmd.GainEnergy(_equalGain, cardPlay.Player);
            CombatCounters.NotifySoulGain(cardPlay.Player, _equalGain);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradedBlock - BaseBlock);
        _minorGain = 3;
        _equalGain = 4;
    }
}
