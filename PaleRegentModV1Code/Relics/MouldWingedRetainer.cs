using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 【模具·有翼佣卫】遗物（表格 R#2 / N#9）。
/// 你的每回合开始时，生成并打出【有翼佣卫】。1 场战斗后失效。
/// </summary>
public class MouldWingedRetainer : MouldRelic
{
    public override Type MouldCardType => typeof(WingedRetainerCard);

    protected override CardModel CreateMouldCard()
    {
        return Owner.Creature.CombatState.CreateCard<WingedRetainerCard>(Owner);
    }
}
