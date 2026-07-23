using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【疫蔓】buff（能力牌"疫蔓"施加，机制文档：瘟疫流）。
/// 效果：每当你生成一张【感染】，对一个随机敌人施加 [层数] 层【瘟疫】。
///
/// 占位说明：
/// - 文档原义是"随机敌我生物"，先简化为随机敌人（对自己上瘟疫的体验
///   比较劝退，等你确定方向再改，改法：把 HittableEnemies 换成 Creatures）。
/// - 触发入口：Infection 卡的生成入口统一走 Infection.NotifyGenerated()
///   （见 Infection.cs），本 Power 提供 OnInfectionGenerated 给它调用。
/// </summary>
public class PlagueSpreadPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>感染生成时由 Infection 通知调用。</summary>
    public async Task OnInfectionGenerated()
    {
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null || combatState.HittableEnemies.Count == 0 || Owner.Player == null)
        {
            return;
        }
        Flash();
        Creature target = Owner.Player.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
        await PowerCmd.Apply<PlaguePower>(new ThrowingPlayerChoiceContext(), target, Amount, Owner, null);
    }
}
