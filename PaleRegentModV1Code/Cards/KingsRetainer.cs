using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【国王佣卫】生成牌（表格设计：造物流，"佣卫工厂"每回合生成）。
/// 0 灵魂 1 虚空 攻击：对随机敌人造成 5 点伤害。获得 1 驾驭。消耗。升级后 8 伤。获得 2 驾驭。
/// 造物牌：受【驾驭 Harness】加成（HarnessPower.ModifyDamageAdditive 自动生效）。
/// 20260725：已接入【模具】体系（IsMould，见 MouldHelper / 名词表 N#9）。
/// </summary>
public class KingsRetainer : PaleRegentModV1Card
{
    /// <summary>虚空费（表格：0灵魂+1虚空）。</summary>
    private const int VoidCost = 0;
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;
    private const int BaseHarness = 1;

    public KingsRetainer() : base(0,
        CardType.Attack, CardRarity.Token,
        TargetType.None)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Harness, ModHoverTips.CreationRule, ModHoverTips.Mould];

    public override bool IsCreationCard => true;

    /// <summary>模具牌标记（战斗结束按消耗数概率成为遗物，见名词表 N#9）。</summary>
    public override bool IsMould => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move), new PowerVar<HarnessPower>(BaseHarness)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(CombatState!, true)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        await PowerCmd.Apply<HarnessPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["HarnessPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        DynamicVars["HarnessPower"].UpgradeValueBy(1m);
    }
    
    //不进入奖励池
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<TokenCardPool>();
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;
}
