using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Patches;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂双刃】攻击牌（表 C#57，0727 新增）。
/// 2 灵魂 + 2 虚空：造成等同于本场战斗中灵魂及虚空能量获得次数的伤害。
/// 升级后：1 灵魂 + 1 虚空。
/// 备注（不改表格原文）：灵魂获得次数只统计卡牌/能力主动获能（回合常规恢复不计），
/// 详见 CombatCounters 的统计口径说明。
/// </summary>
public class SoulBlades : PaleRegentModV1Card
{
    private const int BaseVoidCost = 2;

    public SoulBlades() : base(2,
        CardType.Attack, CardRarity.Rare,
        TargetType.AnyEnemy)
    {
        CardTraits.SetVoidCost(this, BaseVoidCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int damage = CombatCounters.TotalEnergyGainCount;
        if (damage <= 0)
        {
            return;
        }

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 2 费 2 虚空 → 1 费 1 虚空
        EnergyCost.UpgradeBy(-1);
        CardTraits.SetVoidCost(this, BaseVoidCost - 1);
    }
}
