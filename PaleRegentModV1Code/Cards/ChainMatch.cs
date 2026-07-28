using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【连锁引信】技能牌（表 C#62，0727 新增）。
/// 1 灵魂：获得 3 层【连锁共鸣】（每当你连续打出与上一张相同的牌，
/// 消耗 1 层，对所有敌人造成等同于该牌伤害的伤害）。
/// 升级后：5 层。
/// </summary>
public class ChainMatch() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseStacks = 3;
    private const int UpgradeStacksBonus = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ChainResonancePower>(BaseStacks)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ChainResonancePower>(choiceContext, Owner.Creature,
            DynamicVars["ChainResonancePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ChainResonancePower"].UpgradeValueBy(UpgradeStacksBonus);
    }
}
