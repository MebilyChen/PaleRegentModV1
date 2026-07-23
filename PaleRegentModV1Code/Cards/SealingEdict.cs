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
/// 【封印之令】技能牌（机制文档：新增效果"纯粹封印"的载体，占位命名）。
/// 1 灵魂 技能：对目标敌人施加 1 层【纯粹封印】
/// （层数回合内其每回合第一次攻击伤害无效）。消耗。
/// 升级后：施加 2 层。
/// </summary>
public class SealingEdict() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Rare,
    TargetType.AnyEnemy)
{
    private const int BaseSeal = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PureSealPower>(BaseSeal)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<PureSealPower>(choiceContext, cardPlay.Target,
            DynamicVars["PureSealPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PureSealPower"].UpgradeValueBy(1m);
    }
}
