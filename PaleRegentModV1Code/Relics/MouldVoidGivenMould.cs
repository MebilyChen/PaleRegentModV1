using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 【模具·虚空化模】遗物（表格 R#2 / N#9）。
/// 你的每回合开始时，生成并打出【虚空化模】。1 场战斗后失效。
/// 备注：虚空化模本身带 1 虚空费，自动打出时按 AutoPlay 规则免费结算
/// （与原版 Havoc 一致）；若游戏内验证发现仍扣虚空费，请反馈日志调整。
/// </summary>
public class MouldVoidGivenMould : MouldRelic
{
    public override Type MouldCardType => typeof(VoidGivenMould);

    protected override CardModel CreateMouldCard()
    {
        return Owner.Creature.CombatState.CreateCard<VoidGivenMould>(Owner);
    }
}
