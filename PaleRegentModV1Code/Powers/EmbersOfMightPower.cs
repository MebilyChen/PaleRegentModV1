using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【余烬赐力】buff（效果表 P#31，卡牌 C#102 余烬赐力 施加，多人卡）。
/// 效果：每消耗 1 张牌，随机一名玩家获得 [层数] 点力量（单人时即自己）。
///
/// 实现说明：
/// - 挂 AfterCardExhausted（任意来源的消耗均计，含虚无 Ethereal）；
/// - "随机玩家"用运行随机数（RunState.Rng.CombatTargets，与 PlaguePower 口径一致）；
/// - 力量用原版 StrengthPower。
/// </summary>
public class EmbersOfMightPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        // 只统计持有者自己的牌被消耗（多人时不蹭队友的消耗）
        if (card.Owner != Owner.Player)
        {
            return;
        }

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }

        List<Creature> alivePlayers = combatState.PlayerCreatures
            .Where(c => c.IsAlive).ToList();
        if (alivePlayers.Count == 0)
        {
            return;
        }

        Creature target = alivePlayers.Count == 1
            ? alivePlayers[0]
            : Owner.Player!.RunState.Rng.CombatTargets.NextItem(alivePlayers);

        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(),
            target, Amount, Owner, null);
    }
}
