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
///
/// 2 灵魂 + 2 虚空：
/// 造成等同于本场战斗中，当前玩家主动获得的
/// 【灵魂点数 + 虚空点数】总和的伤害。
///
/// 例如：
///   获得 3 灵魂，再获得 2 灵魂，再获得 4 虚空
///   → 伤害 = 3 + 2 + 4 = 9。
///
/// 多人模式下只统计本牌所属玩家自己的资源获得，
/// 不统计其他玩家。
///
/// 灵魂侧只统计卡牌/能力主动获得的能量，
/// 回合开始时的常规能量恢复不计。
///
/// 升级后：1 灵魂 + 1 虚空。
/// </summary>
public class SoulBlades : PaleRegentModV1Card
{
    private const int BaseVoidCost = 2;

    public SoulBlades() : base(
        2,
        CardType.Attack,
        CardRarity.Rare,
        TargetType.AnyEnemy)
    {
        CardTraits.SetVoidCost(this, BaseVoidCost);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(
            cardPlay.Target,
            "cardPlay.Target");

        // 20260816：
        // 不再读取 CombatCounters.TotalEnergyGainCount。
        //
        // 旧值统计的是“获得次数”，并且多人共用一份 static 总账。
        //
        // 灵魂双刃现在读取专用的按玩家计数器：
        // 本场获得灵魂点数 + 本场获得虚空点数。
        int damage =
            SoulBladesEnergyTracker.GetTotal(Owner);

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
        // 2 灵魂 + 2 虚空
        // →
        // 1 灵魂 + 1 虚空
        EnergyCost.UpgradeBy(-1);

        CardTraits.SetVoidCost(
            this,
            BaseVoidCost - 1);
    }
}