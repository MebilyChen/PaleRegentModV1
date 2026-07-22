using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 双能量系统演示卡：打出后获得 2 点虚空。
/// - VoidResource.Gain：增加虚空副资源（战斗界面能量球旁的计数器）
/// - PowerCmd.Apply&lt;VoidPower&gt;：给玩家挂上虚空 Power 图标，回合开始时
///   由 VoidPower.AfterEnergyReset 扣除等量灵魂（能量）。
/// 两者数量保持同步：Power 的 Amount 仅用于展示，实际结算以副资源数值为准。
/// </summary>
public class Test() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Basic,
    TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    private const int VoidGain = 2;

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay play)
    {
        // 1. 增加虚空副资源（传入 this 作为来源，方便日志追踪）
        await VoidResource.Gain(play.Player, VoidGain);

        // 2. 同步施加/叠加 VoidPower（buff 图标 + 回合开始扣能量的挂点）
        // 注意：STS2 的 Player 不继承 Creature（和 STS1 不同），
        // PowerCmd.Apply 的 target/applier 参数要传 play.Player.Creature。
        await PowerCmd.Apply<VoidPower>(choiceContext, play.Player.Creature, VoidGain, play.Player.Creature, this);
    }

    protected override void OnUpgrade()
    {

    }
}