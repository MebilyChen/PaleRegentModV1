using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 【模具·国王佣卫】遗物（表格 R#2 / N#9）。
/// 你的每回合开始时，生成并打出【国王佣卫】。1 场战斗后失效。
/// </summary>
public class MouldKingsRetainer : MouldRelic
{
    public override Type MouldCardType => typeof(KingsRetainer);

    protected override CardModel CreateMouldCard()
    {
        return Owner.Creature.CombatState.CreateCard<KingsRetainer>(Owner);
    }
}
