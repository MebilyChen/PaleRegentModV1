using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【容器计划】能力牌（机制文档：造物流）。
/// 3 灵魂 + 3 虚空 能力：你的每回合开始时，将 1 张【容器】加入手牌。
/// 升级后：生成【容器+】。
/// </summary>
public class VesselPlan : PaleRegentModV1Card
{
    private const int VesselPerTurn = 1;

    public VesselPlan() : base(2,
        CardType.Power, CardRarity.Uncommon,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, 2);
    }

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VesselPlanPower>((int?)null),
         HoverTipFactory.FromCard<Vessel>(IsUpgraded)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VesselPlanPower>(VesselPerTurn)];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VesselPlanPower>(choiceContext, Owner.Creature,
            DynamicVars["VesselPlanPower"].BaseValue, Owner.Creature, this);

        // 升级后：标记 Power 生成升级版【容器+】
        if (IsUpgraded &&
            Owner.Creature.GetPower<VesselPlanPower>() is VesselPlanPower plan)
        {
            plan.MakeUpgraded = true;
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：生成【容器+】（见 OnPlay 的 IsUpgraded 分支）
    }
}
