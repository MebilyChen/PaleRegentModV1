using MegaCrit.Sts2.Core.HoverTips;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Patches;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【异色】（表格 C#10，20260725 新增；表格未给英文 Codename，暂定 OffColor，已备注）。
/// 0 灵魂 0 虚空 攻击/Uncommon：造成 5(7) x N 的伤害，
/// N = 本回合你获得过的虚空能量数。
///
/// 实现说明：
/// - "本回合获得虚空数"由 VoidPowerListener.VoidGainedThisTurn 全局累计
///   （只统计正增量），每个玩家回合开始时由 PaleToken.AfterEnergyReset 清零。
/// - N 为 0 时不造成伤害（0 段）。
/// </summary>
public class OffColor() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    /// <summary>基础伤害（每段）。</summary>
    private const int BaseDamage = 5;

    /// <summary>升级后伤害增加量（5 → 7）。</summary>
    private const int UpgradeDamageBonus = 2;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.VoidCounter];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int hits = VoidPowerListener.VoidGainedThisTurn;
        for (int i = 0; i < hits; i++)
        {
            if (cardPlay.Target is not { IsAlive: true })
            {
                break;
            }
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
