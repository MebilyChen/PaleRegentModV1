using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【虚空化模】buff（由卡牌【虚空化模 VoidGivenMould】施加，持续到战斗结束）。
/// 效果：消耗为 0 灵魂的卡牌伤害 +[层数]（基础 5，升级 7）。
///
/// 实现说明：
/// - 用 ModifyDamageAdditive 钩子：持有者造成的攻击伤害，
///   若来源卡牌当前灵魂费为 0（含 X 费以外的 0 费、失心转虚空费的牌），伤害 +Amount。
/// - 判定"0 灵魂"只看灵魂费（EnergyCost），不看虚空费——
///   与表格 P 列"0灵魂的卡牌伤害+5"一致。
/// - StackType = Counter：重复打出可叠加（5+5=10），如需不可叠加改 Single。
/// </summary>
public class VoidGivenMouldPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner || !props.IsPoweredAttack() || cardSource == null)
        {
            return 0m;
        }

        // X 费牌不算 0 灵魂费
        if (cardSource.EnergyCost.CostsX)
        {
            return 0m;
        }

        int energyCost = cardSource.EnergyCost.GetWithModifiers(CostModifiers.None);
        return energyCost == 0 ? Amount : 0m;
    }
}
