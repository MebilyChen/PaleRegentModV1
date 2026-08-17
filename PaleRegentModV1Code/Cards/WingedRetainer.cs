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
/// 【有翼卫群】能力牌（机制文档：造物流）。
/// 2 灵魂 0 虚空 能力：每回合开始时，将 1 张【有翼佣卫】（格挡造物牌）加入手牌。驾驭5。
/// 升级后：改为生成【有翼佣卫+】。
/// 注：类名仍为 WingedRetainer，卡牌标题改为"有翼卫群"。
/// </summary>
public class WingedRetainer : PaleRegentModV1Card
{
    /// <summary>虚空费（表格：2灵魂+0虚空）。</summary>
    private const int VoidCost = 0;
    /// <summary>每回合生成张数。</summary>
    private const int ForgePerTurn = 1;
    private const int BaseHarness = 5;

    public WingedRetainer() : base(2,
        CardType.Power, CardRarity.Uncommon,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<WingedRetainerCard>(IsUpgraded),
         ModHoverTips.Mould,
         ModHoverTips.Harness];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WingedForgePower>(ForgePerTurn), new PowerVar<HarnessPower>(BaseHarness)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WingedForgePower>(choiceContext, Owner.Creature,
            DynamicVars["WingedForgePower"].BaseValue, Owner.Creature, this);
        
        await PowerCmd.Apply<HarnessPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["HarnessPower"].BaseValue,
            Owner.Creature,
            this);

        // 升级后：让 Power 生成升级版【有翼佣卫+】
        if (IsUpgraded &&
            Owner.Creature.GetPower<WingedForgePower>() is WingedForgePower forge)
        {
            forge.MakeUpgraded = true;
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：生成【有翼佣卫+】（层数不变，见 OnPlay 里的 MakeUpgraded）
    }
}
