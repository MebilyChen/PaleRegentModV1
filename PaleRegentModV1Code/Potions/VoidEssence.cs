using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Potions;

/// <summary>
/// 【虚空精华】普通药水。
/// 战斗中使用：获得 5 点虚空。
///
/// 修改指南：
/// - 获得量：VoidGain 常量（描述文案在 potions.json 里同步改）。
/// </summary>
public class VoidEssence : PaleRegentModV1Potion
{
    /// <summary>使用后获得的虚空数量。</summary>
    private const int VoidGain = 5;

    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    // 占位图：直接用 mod 内图片路径（正式图做好后替换 png 即可）
    public override string CustomPackedImagePath =>
        "res://PaleRegentModV1/images/potions/void_essence.png";
    public override string CustomPackedOutlinePath =>
        "res://PaleRegentModV1/images/potions/void_essence.png";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);

        // Assert 已保证 target 非空，用 ! 消除 CS8604 可空警告
        await VoidResource.Gain(target!.Player, VoidGain);
        await VoidResource.SyncPower(choiceContext, target.Player, null);
    }
}
