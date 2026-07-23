using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空转化】普通技能牌（攒虚空的主力手段）。
/// X 灵魂：获得 X 点虚空。
///
/// 机制要点：
/// - 灵魂 X 费：HasEnergyCostX = true，打出时消耗全部灵魂，
///   实际的 X 值在 OnPlay 里用 ResolveEnergyXValue() 读取（原版 Whirlwind 同款写法）。
/// - 注意这是"灵魂→虚空"的转换器；反向的"虚空→灵魂"是【回退】（Rollback）。
///
/// 修改指南：
/// - 想改成"获得 X+1 点虚空"（升级效果）：在 OnPlay 里给 x 加上升级加成。
/// </summary>
public class VoidConversion() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    // 灵魂 X 费：打出时消耗当前全部灵魂作为 X
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 读取本次支付的 X（= 打出时的灵魂数，已被扣除）
        int x = ResolveEnergyXValue();
        if (x <= 0)
        {
            return;
        }

        // 获得 X 点虚空并同步展示层
        await VoidResource.Gain(cardPlay.Player, x);
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
    }

    protected override void OnUpgrade()
    {
        // 升级方案待定：常见做法是"获得 X+1 点虚空"，
        // 实现方式：加一个 int 字段记录加成，OnPlay 里 x += bonus。
    }
}
