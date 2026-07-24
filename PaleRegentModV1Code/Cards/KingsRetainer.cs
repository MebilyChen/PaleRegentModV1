using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【国王佣卫】生成牌（表格设计：造物流，"佣卫工厂"每回合生成）。
/// 0 灵魂 1 虚空 攻击：对随机敌人造成 5 点伤害。消耗。升级后 8 伤。
/// 造物牌：受【驾驭 Harness】加成（HarnessPower.ModifyDamageAdditive 自动生效）。
/// （表格关键词含"模具"，模具体系未实现，暂不接。）
/// </summary>
public class KingsRetainer : PaleRegentModV1Card
{
    /// <summary>虚空费（表格：0灵魂+1虚空）。</summary>
    private const int VoidCost = 1;
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;

    public KingsRetainer() : base(0,
        CardType.Attack, CardRarity.Token,
        TargetType.None)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    public override bool IsCreationCard => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(CombatState!, true)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
