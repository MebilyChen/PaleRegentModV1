using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【有翼佣卫】生成牌（表格设计：造物流，能力牌"有翼卫群"每回合生成）。
/// 0 灵魂 1 虚空 技能：获得 3 点格挡。消耗。升级后 6 点格挡。
/// 20260725：已接入【模具】体系（IsMould，见 MouldHelper / 名词表 N#9）。
/// 造物牌：格挡额外 +【驾驭 Harness】层数（伤害类走 HarnessPower 钩子，
/// 格挡类没有按卡牌来源的统一修正钩子，所以在这里主动读层数）。
/// </summary>
public class WingedRetainerCard : PaleRegentModV1Card
{
    /// <summary>虚空费（表格：0灵魂+1虚空）。</summary>
    private const int VoidCost = 1;
    private const int BaseBlock = 3;
    private const int UpgradeBlockBonus = 3;

    public WingedRetainerCard() : base(0,
        CardType.Skill, CardRarity.Token,
        TargetType.Self)
    {
        Traits.CardTraits.SetVoidCost(this, VoidCost);
    }

    public override bool IsCreationCard => true;

    /// <summary>模具牌标记（战斗结束按消耗数概率成为遗物，见名词表 N#9）。</summary>
    public override bool IsMould => true;

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal block = DynamicVars.Block.BaseValue;
        // 驾驭加成：格挡类造物牌主动读持有者的 HarnessPower 层数；
        // 模具遗物自动打出时不吃 Harness（表格 N#9：去除 Harness 临时效果）
        PowerModel? harness = Owner.Creature.GetPower<HarnessPower>();
        if (harness != null && !Relics.MouldRelic.MouldAutoPlayFlag)
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
