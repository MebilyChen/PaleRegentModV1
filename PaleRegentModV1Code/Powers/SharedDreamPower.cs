using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【共梦之誓】buff（效果表 P#30，卡牌 C#98 共梦之誓 施加，多人卡）。
/// 效果：你的回合开始时，你和所有盟友各获得 [层数] 层【入梦】（DreamPower）。
///
/// 实现说明：
/// - 挂 AfterEnergyReset（持有者回合开始）；
/// - "盟友" = 玩家侧除持有者外的其他存活生物（联机队友；单人时只有自己）；
/// - 入梦为本模组已有 DreamPower（守梦者 C#73 同款）。
/// </summary>
public class SharedDreamPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }

        Flash();
        ThrowingPlayerChoiceContext choiceContext = new ThrowingPlayerChoiceContext();
        foreach (Creature ally in combatState.PlayerCreatures.Where(c => c.IsAlive).ToList())
        {
            await PowerCmd.Apply<DreamPower>(choiceContext, ally, Amount, Owner, null);
        }
    }
}
