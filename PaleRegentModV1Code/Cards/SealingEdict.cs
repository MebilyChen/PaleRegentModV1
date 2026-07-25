using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【封印之令】技能牌（机制文档：卡牌表 C#40）。
/// 2 灵魂 技能：获得 5 点格挡，对目标敌人施加 1 层【纯粹封印】
/// （层数回合内其每回合第一次攻击伤害置 0）。
/// 升级后：格挡 10。
/// 20260725 批次：灵魂费用 1→2（表格 I44 高亮）。
/// </summary>
public class SealingEdict() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Common,
    TargetType.AnyEnemy)
{
    private const int BaseBlock = 5;
    private const int UpgradeBlockBonus = 5;
    private const int BaseSeal = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move), new PowerVar<PureSealPower>(BaseSeal)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<PureSealPower>(choiceContext, cardPlay.Target,
            DynamicVars["PureSealPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
