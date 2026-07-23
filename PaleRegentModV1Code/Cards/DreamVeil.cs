using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【入梦】技能牌（机制文档：新增效果"入梦"的载体，占位命名）。
/// 1 灵魂 技能：获得 1 层【入梦】（免疫下一次受到的伤害）。消耗。
/// 升级后：不再消耗。
/// </summary>
public class DreamVeil : PaleRegentModV1Card
{
    private const int DreamAmount = 1;

    /// <summary>升级后移除消耗。</summary>
    private bool _noExhaust;

    public DreamVeil() : base(1,
        CardType.Skill, CardRarity.Rare,
        TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        _noExhaust ? [] : [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DreamPower>(DreamAmount)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DreamPower>(choiceContext, Owner.Creature,
            DynamicVars["DreamPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        _noExhaust = true;
    }
}
