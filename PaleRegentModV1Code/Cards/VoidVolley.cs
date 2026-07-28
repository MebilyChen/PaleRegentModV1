using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空连射】攻击牌（表 C#89，0727 新增）。
/// X 灵魂：造成 X 次 7 点伤害，并在手牌中生成 X 张【虚空】状态牌。
/// 升级后：每段 10 点伤害，X+1 次（生成张数仍为 X）。
/// 备注：表格升级栏"10x(X+1)"按"段数 X+1、生成 X 张"实现；
/// 若生成张数也要 X+1 请告知。
/// </summary>
public class VoidVolley() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int DamagePerHit = 7;

    protected override bool HasEnergyCostX => true;

    /// <summary>升级后的额外段数（X+1）。</summary>
    private int _bonusHits;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(DamagePerHit, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<TheVoidStatus>(false)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int x = ResolveEnergyXValue();
        if (x <= 0)
        {
            return;
        }

        // 1. X（升级 X+1）段伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(x + _bonusHits)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 手牌生成 X 张虚空状态牌（写法同 VoidBacklash）
        await CardPileCmd.AddToCombatAndPreview<TheVoidStatus>(Owner.Creature, PileType.Hand, x, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        _bonusHits = 1;
    }
}
