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
/// 【弃壳】技能牌（表格设计：蓄灵载体）。
/// X 灵魂 技能：结束你的回合；获得 X 层【蓄灵】（下回合开始时获得 X 灵魂）。消耗。
/// 升级后：蓄灵 X+1。
/// </summary>
public class CastOffShell() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    // 灵魂 X 费：打出时消耗当前全部灵魂作为 X（与【染色】同款写法）
    protected override bool HasEnergyCostX => true;

    /// <summary>升级后蓄灵额外 +1。</summary>
    private int _soulBonus;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 读取本次支付的 X（= 打出时的灵魂数，已被扣除）
        int x = ResolveEnergyXValue();
        int amount = x + _soulBonus;
        if (amount > 0)
        {
            await PowerCmd.Apply<SoulNextTurnPower>(choiceContext, Owner.Creature,
                amount, Owner.Creature, this);
        }

        // 结束你的回合
        await PlayerCmd.EndTurn(Owner, false, (Func<Task>)null);
    }

    protected override void OnUpgrade()
    {
        // 升级：蓄灵 X+1
        _soulBonus = 1;
    }
}
