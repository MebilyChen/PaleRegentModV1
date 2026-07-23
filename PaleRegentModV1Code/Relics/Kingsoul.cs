namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 国王之魂（Kingsoul）—— 苍白信物的升级形态（由王后信物触发替换获得）。
/// 效果：
/// 1. 灵魂（能量）上限 +2。
/// 2. 你的每回合开始时，只能恢复［灵魂上限 - 虚空 + 1］数量的灵魂
///    （比苍白信物多恢复 1 点，即少扣 1 点虚空惩罚）。
///
/// 直接继承 PaleToken 复用全部逻辑，只改两个数值。
/// Rarity 仍为 Starter：它是初始遗物的替换形态，不会出现在遗物奖励池里
/// （Starter 稀有度的遗物不会被加入普通掉落池）。
/// </summary>
public class Kingsoul : PaleToken
{
    protected override int MaxEnergyBonus => 2;
    protected override int RecoveryBonus => 1;
}
