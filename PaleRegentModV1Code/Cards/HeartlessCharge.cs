using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【失心冲锋】攻击牌（表 C#90，0727 新增）。
/// 1 灵魂 + 1 虚空：造成 10 点伤害。动量 5（原版 Momentum 附魔：每次打出
/// 后本场战斗伤害 +5）。失心。
/// 升级后：15 点伤害。动量 7
/// 备注：按原版 Momentum 附魔语义实现（amount=5），即每次打出后后续
/// 伤害累加 5；在进入战斗时自动挂附魔。
/// </summary>
public class HeartlessCharge : PaleRegentModV1Card
{
    private const int BaseDamage = 10;
    private const int UpgradeDamageBonus = 5;
    private const int VoidCost = 1;
    private const int MomentumAmount = 5;
    private const int UpgradeMomentumAmount = 7;
    
    private int MomentumToApply =>
        IsUpgraded ? UpgradeMomentumAmount : MomentumAmount;

    public HeartlessCharge() : base(1,
        CardType.Attack, CardRarity.Uncommon,
        TargetType.AnyEnemy)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    /// <summary>自带失心（20260728 修复：禁止构造器 ApplyLost，改由基类在入战时施加）。</summary>
    public override bool HasInnateLost => true;

    /// <summary>自身进入战斗时自动挂上动量 5 附魔（原版 Momentum）。</summary>
    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        await base.AfterCardEnteredCombat(card);

        if (card == this && Enchantment == null)
        {
            CardCmd.Enchant<Momentum>(this, MomentumToApply);
        }
    }

    /// <summary>
    /// 战斗开始时也检查一次动量附魔（20260728 备注：战斗开始时卡组牌通过
    /// PopulateCombatState 克隆进抽牌堆，不触发 AfterCardEnteredCombat，
    /// 故补充此钩子确保开局卡组里的本牌也能挂上动量；Enchantment==null 防重复）。
    /// 基类 BeforeCombatStart 会先施加自带失心。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();

        if (IsMutable && Enchantment == null)
        {
            CardCmd.Enchant<Momentum>(this, MomentumToApply);
        }
    }
    

    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[] { ModHoverTips.Lost }
            .Concat(HoverTipFactory.FromEnchantment<Momentum>(MomentumToApply));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
