using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【文明之光】buff（效果表 P#27，卡牌 C#87 文明之光 施加）。
/// 效果：灵魂（能量）大于 0 时，若手牌为空，抽 [层数] 张牌。
///
/// 实现说明：
/// - 触发时机：每当一张牌结算完毕（AfterCardPlayed）后检查手牌是否打空；
///   另在回合开始抽牌后不检查（开局必有手牌）；
/// - 防重复触发：每回合只触发一次（_triggeredThisTurn），否则“抽1张→打出→
///   又空→再抽”可能反复触发形成无限循环（已在条目后备注；
///   若希望每次手牌打空都触发，去掉该标记即可）；
/// - 层数 = 抽牌张数（基础 1，升级后叠到 2）。
/// </summary>
public class LightOfCivilizationPower : PaleRegentModV1Power
{
    /// <summary>本回合是否已触发过（防无限循环）。</summary>
    private bool _triggeredThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner.Player;
        if (player == null || cardPlay.Player != player || _triggeredThisTurn)
        {
            return;
        }

        // 灵魂（能量）> 0 且手牌为空
        if (player.PlayerCombatState == null
            || player.PlayerCombatState.Energy <= 0
            || player.PlayerCombatState.Hand.Cards.Count > 0)
        {
            return;
        }

        _triggeredThisTurn = true;
        Flash();
        await CardPileCmd.Draw(choiceContext, (int)Amount, player);
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, System.Collections.Generic.IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            _triggeredThisTurn = false;
        }
        return Task.CompletedTask;
    }
}
