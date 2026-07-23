using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【集火号令】技能牌（机制文档：瘟疫流"嘲讽/集中目标"，占位命名）。
/// 0 灵魂 技能：本回合【瘟疫】的随机攻击全部集中到目标敌人，
/// 并对其施加 1 层【瘟疫】。
/// 升级后：施加 2 层瘟疫。
/// </summary>
public class FocusFireEdict() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    private const int BasePlague = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PlaguePower>(BasePlague)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        // 本回合瘟疫集中目标（回合结束由 PlaguePower 复位）
        PlaguePower.FocusTarget = cardPlay.Target;
        await PowerCmd.Apply<PlaguePower>(choiceContext, cardPlay.Target,
            DynamicVars["PlaguePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PlaguePower"].UpgradeValueBy(1m);
    }
}
