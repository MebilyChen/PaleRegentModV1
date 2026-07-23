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
/// 【苦痛之路】技能牌（机制文档：新增效果"苦痛之路"的载体，占位设计）。
/// 2 灵魂 技能：对目标敌人施加 3 层【苦痛之路】
/// （其每次因攻击受到未格挡伤害时，额外失去等量生命，每层触发一次）。消耗。
/// 升级后：施加 4 层。
/// </summary>
public class PathOfPain() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Rare,
    TargetType.AnyEnemy)
{
    private const int BaseAmount = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PathOfPainPower>(BaseAmount)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<PathOfPainPower>(choiceContext, cardPlay.Target,
            DynamicVars["PathOfPainPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PathOfPainPower"].UpgradeValueBy(1m);
    }
}
