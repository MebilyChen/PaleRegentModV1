using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【异质增生】buff（效果表 P#21，卡牌 C#56 异质增生 施加）。
/// 效果：每生成 1 张状态牌，获得 [层数] 点力量。
///
/// 实现说明：
/// - 挂 AfterCardGeneratedForCombat 全局生成钩子，判定 card.Type == CardType.Status；
/// - "生成"的范围：本场战斗中新生成的状态牌（含敌人塞给你的、以及
///   感染/虚空等我方状态牌），不含开局牌组里原有的牌；
/// - 力量用原版 StrengthPower，与其他力量来源正常叠加。
/// </summary>
public class MutagenicGrowthPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card.Type != CardType.Status)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(),
            Owner, Amount, Owner, null);
    }
}
