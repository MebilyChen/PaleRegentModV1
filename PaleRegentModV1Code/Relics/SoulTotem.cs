using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 【偶像图腾 Soul Totem】普通遗物（机制表：遗物 R#12，0727 新增）。
/// 效果：每场战斗你的灵魂第一次降为 0 时，获得 1 点灵魂。
///
/// 实现说明（"灵魂降为 0"的两个入口都要覆盖）：
/// 1. 打牌花费灵魂：CardModel.SpendEnergy → Hook.AfterEnergySpent。
///    重写 AfterEnergySpent，花费后当前灵魂恰好为 0 → 触发。
/// 2. 回合开始虚空反噬扣灵魂：苍白信物在 AfterEnergyReset 里
///    PlayerCmd.LoseEnergy（该命令本身无钩子）。这里用 AfterEnergyResetLate
///    兜底——引擎保证 Late 版在所有模型的 AfterEnergyReset 之后运行，
///    此时反噬已扣完，若灵魂为 0 → 触发。
///    （虚空 ≥ 灵魂上限时开局就会被扣到 0，也算"降为 0"。）
/// 3. 每场战斗只触发一次：_usedThisCombat 标记，BeforeCombatStart 重置
///    （遗物实例是整局 run 级别的，必须每场战斗重置）。
/// 4. 触发后获得 1 点灵魂用 PlayerCmd.GainEnergy —— 会正常走
///    CombatCounters.NotifySoulGain 之外的引擎流程；此处不调用
///    NotifySoulGain，因为该计数器语义是"卡牌效果获得灵魂"（共鸣一击），
///    遗物触发是否计入，等你在表格里明确后再调（备注，不改表格原文）。
///
/// 备注（机制表 R#12 备注栏）：图片用国王小雕像 —— 当前先用占位图
/// soul_totem.png，等你提供国王小雕像素材后直接替换同名文件即可。
/// </summary>
public class SoulTotem : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    private const int SoulGain = 1;

    /// <summary>本场战斗是否已触发过（每场战斗仅第一次生效）。</summary>
    private bool _usedThisCombat;

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    /// <summary>入口 1：打牌花费灵魂后，恰好降为 0。</summary>
    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (card.Owner != Owner || amount <= 0)
        {
            return;
        }
        await TryTrigger();
    }

    /// <summary>入口 2：回合开始能量重置 + 虚空反噬扣除完成后，灵魂为 0。</summary>
    public override async Task AfterEnergyResetLate(Player player)
    {
        if (player != Owner)
        {
            return;
        }
        await TryTrigger();
    }

    private async Task TryTrigger()
    {
        if (_usedThisCombat ||
            Owner.PlayerCombatState == null ||
            Owner.PlayerCombatState.Energy > 0)
        {
            return;
        }

        _usedThisCombat = true;
        Flash();
        await PlayerCmd.GainEnergy(SoulGain, Owner);
    }
}
