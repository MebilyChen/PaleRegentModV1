using MegaCrit.Sts2.Core.Entities.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【疫佑】buff（能力牌"疫佑"施加，机制文档：瘟疫流，占位实现）。
/// 效果：你的【瘟疫】随机攻击不再命中你和你的队友（只打敌方）。
///
/// 实现方式：本 Power 只作为标记存在，实际判定在 PlaguePower 的随机
/// 选目标逻辑里（见 PlaguePower.PickRandomAliveCreature 对
/// HasPlagueWard 的检查）。
/// </summary>
public class PlagueWardPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
