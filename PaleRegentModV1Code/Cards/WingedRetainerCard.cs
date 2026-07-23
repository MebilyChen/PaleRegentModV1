using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【有翼佣卫】生成牌（机制文档：造物流，能力牌"有翼佣卫"每回合生成）。
/// 0 灵魂 技能：获得 7 点格挡。消耗。
/// 造物牌：格挡额外 +【驾驭 Harness】层数（伤害类走 HarnessPower 钩子，
/// 格挡类没有按卡牌来源的统一修正钩子，所以在这里主动读层数）。
/// </summary>
public class WingedRetainerCard() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Special,
    TargetType.Self)
{
    private const int BaseBlock = 7;
    private const int UpgradeBlockBonus = 3;

    public override bool IsCreationCard => true;
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal block = DynamicVars.Block.BaseValue;
        // 驾驭加成：格挡类造物牌主动读持有者的 HarnessPower 层数
        PowerModel? harness = Owner.Creature.GetPower<HarnessPower>();
        if (harness != null)
        {
            block += harness.Amount;
        }
        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
