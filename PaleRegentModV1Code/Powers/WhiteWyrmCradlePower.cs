using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【白沃姆摇篮】buff（效果表 P#29，卡牌 C#97 白沃姆摇篮 施加）。
/// 效果（表 C#97）：
/// 1. 你的【蓄灵】不再在回合开始后消失（改为持续保留，每回合都供灵）；
/// 2. 你的回合开始时，获得 1 层【蓄灵】和 [层数] 层【白根】；
/// 3. 你受到未格挡的攻击伤害时，移除你的全部【蓄灵】。
///
/// 实现说明：
/// - "蓄灵不消失"：SoulNextTurnPower.AfterEnergyReset 会在供灵后自我移除，
///   已改为：持有者身上存在本 Power 时跳过自我移除
///   （见 SoulNextTurnPower 的 20260727 修改）；
/// - 白根层数 = 本 Power 层数（基础 1，升级 3，可叠加）；蓄灵固定每回合 +1 层；
/// - "未格挡的攻击伤害"：AfterDamageReceived 中 result.UnblockedDamage > 0
///   且伤害带攻击属性（props 含 ValueProp.Move，荆棘/中毒等非攻击伤害不触发，
///   已在条目后备注）。
/// </summary>
public class WhiteWyrmCradlePower : PaleRegentModV1Power
{
    /// <summary>每回合固定获得的蓄灵层数。</summary>
    private const int SoulChargePerTurn = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>回合开始（能量重置后）：+1 蓄灵、+[层数] 白根。</summary>
    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        ThrowingPlayerChoiceContext choiceContext = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<SoulNextTurnPower>(choiceContext, Owner, SoulChargePerTurn, Owner, null);
        await PowerCmd.Apply<WhiteRootPower>(choiceContext, Owner, Amount, Owner, null);
    }

    /// <summary>受到未格挡的攻击伤害：移除全部蓄灵。</summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
        {
            return;
        }
        // 只响应"攻击"伤害（荆棘/中毒等非攻击伤害不清蓄灵）
        if (!props.HasFlag(ValueProp.Move))
        {
            return;
        }

        PowerModel? soulCharge = Owner.GetPower<SoulNextTurnPower>();
        if (soulCharge == null)
        {
            return;
        }
        Flash();
        await PowerCmd.Remove(soulCharge);
    }
}
