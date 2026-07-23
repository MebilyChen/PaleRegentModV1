using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【返祖】buff（由卡牌【返祖】施加）。
/// 效果：你每打出一张牌，回复层数点灵魂（能量）。
///
/// 说明：
/// - StackType = Counter：重复打【返祖】卡可叠层，叠 2 层就是每张牌回 2 灵魂。
/// - 灵魂回复发生在"卡牌打出结算完成之后"（AfterCardPlayed 钩子），
///   所以打出卡牌本身的耗能先扣，然后再回复——净效果是"每张牌便宜 Amount 点"。
/// - 注意与失心牌联动：失心牌灵魂费为 0，返祖照样回灵魂（净赚），
///   这是设计文档预期的 combo，如果觉得太强可以在这里加判断排除 0 费牌。
/// </summary>
public class AtavismPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 挂点：任意一张牌完成打出之后。
    /// cardPlay.Player == Owner.Player 确保只对"本 Power 持有者打出的牌"触发
    /// （排除多人模式下队友打牌的情况）。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner.Player)
        {
            return;
        }

        Flash(); // 图标闪烁提示触发
        await PlayerCmd.GainEnergy(Amount, cardPlay.Player);
    }
}
