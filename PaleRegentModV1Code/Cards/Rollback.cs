using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【回退】罕见技能牌（虚空的"退出机制"）。
/// 0 灵魂：移除你的全部虚空，每移除 1 点获得 1 点灵魂。【消耗】。
///
/// 定位：攒了一堆虚空但不想继续欠债时的止损/爆发牌——
/// 把虚空债一次性变现为当回合灵魂。与【虚空转化】互为反向操作。
///
/// 修改指南：
/// - 想改兑换比例（如 2 虚空换 1 灵魂）：改 OnPlay 里 GainEnergy 的数值计算。
/// </summary>
public class Rollback() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    // 打出后消耗（防止一场战斗里反复无限转换）
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 读取当前全部虚空
        int n = VoidResource.Get(cardPlay.Player);
        if (n <= 0)
        {
            return; // 没有虚空则什么都不发生
        }

        // 2. 移除全部虚空（Spend 走"支付"语义，日志更清晰）
        await VoidResource.Spend(cardPlay.Player, n);

        // 3. 1:1 转换为灵魂
        await PlayerCmd.GainEnergy(n, cardPlay.Player);

        // 4. 同步 VoidPower 图标（清零后移除）
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
    }

    protected override void OnUpgrade()
    {
        // 升级方案待定：可以去掉【消耗】（RemoveKeyword(CardKeyword.Exhaust)）
        // 或改为"每移除 1 点虚空获得 1 点灵魂并抽 1 张牌"等。
    }
}
