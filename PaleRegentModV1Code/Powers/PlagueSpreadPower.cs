using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【疫蔓】buff（能力牌"疫蔓"施加，机制文档：瘟疫流）。
/// 效果（表格设计版）：每当你生成一张【感染】，对场上所有生物
/// （含自己/队友/敌人/召唤物）施加 [层数] 层【瘟疫】。
///
/// 触发入口：Infection 卡的生成入口统一走 Infection.NotifyGenerated()
/// （见 Infection.cs），本 Power 提供 OnInfectionGenerated 给它调用。
/// </summary>
public class PlagueSpreadPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>感染生成时由 Infection 通知调用：对所有存活生物施加瘟疫。</summary>
    public async Task OnInfectionGenerated()
    {
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }
        Flash();
        foreach (Creature target in combatState.Creatures.Where(c => c.IsAlive).ToList())
        {
            await PowerCmd.Apply<PlaguePower>(new ThrowingPlayerChoiceContext(), target, Amount, Owner, null);
        }
    }
}
