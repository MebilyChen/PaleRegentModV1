using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Relics;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【驾驭 Harness X】buff（机制文档：造物流关键词，占位实现）。
/// 效果：你的"造物牌"（佣卫/容器/虚空化形等生成牌）数值 +[层数]。
///
/// 占位实现说明：
/// - 攻击类造物牌：通过 ModifyDamageAdditive，当伤害来源卡牌 IsCreationCard
///   时 +Amount（等效于只对造物牌生效的力量）。
/// - 格挡类造物牌（如有翼佣卫）：在卡牌 OnPlay 里主动读取持有者的
///   HarnessPower 层数来加格挡（见 WingedRetainerCard），因为格挡没有
///   与卡牌来源关联的统一修正钩子。
/// - "造物牌"的判定见 PaleRegentModV1Card.IsCreationCard 虚属性。
/// </summary>
public class HarnessPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner)
        {
            return 0m;
        }
        if (!props.IsPoweredAttack())
        {
            return 0m;
        }
        // 模具遗物自动打出的牌不吃 Harness 加成（表格 N#9：去除 Harness 临时效果）
        if (MouldRelic.MouldAutoPlayFlag)
        {
            return 0m;
        }
        if (cardSource is PaleRegentModV1Card { IsCreationCard: true })
        {
            return Amount;
        }
        return 0m;
    }
}
