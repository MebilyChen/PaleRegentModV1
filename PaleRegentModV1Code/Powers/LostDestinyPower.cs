using MegaCrit.Sts2.Core.Entities.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【失心诅咒】buff（由卡牌【失心诅咒 LostDestiny】施加，持续到战斗结束）。
/// 效果：你生成的牌获得【失心】。
///
/// 实现说明：
/// - 本 Power 自身没有主动逻辑，只作为标记存在；
///   实际附加失心的钩子在卡牌基类 PaleRegentModV1Card.AfterCardGeneratedForCombat：
///   任何 mod 卡被生成时，检查生成者是否拥有本 Power，是则 CardTraits.ApplyLost。
/// - X 费牌无法附加失心（CanApplyLost 校验），自动跳过。
/// - 注意：仅对本 mod 的卡（继承 PaleRegentModV1Card）生效；
///   原版无色牌等生成时不走 mod 基类钩子（备注：机制文档未涉及，如需覆盖需加 Harmony patch）。
/// </summary>
public class LostDestinyPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
