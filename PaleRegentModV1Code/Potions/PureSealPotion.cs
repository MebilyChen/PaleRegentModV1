using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Potions;

/// <summary>
/// 【纯粹封印药水】罕见药水（机制文档：药水区，占位设计）。
/// 战斗中对一个敌人使用：施加 1 层【纯粹封印】
/// （层数回合内，其每回合第一次攻击伤害降为 0）。
/// </summary>
public class PureSealPotion : PaleRegentModV1Potion
{
    /// <summary>施加的纯粹封印层数。</summary>
    private const int SealTurns = 1;

    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    // 占位图：先复用虚空精华的图，正式图做好后替换路径即可
    public override string CustomPackedImagePath =>
        "res://PaleRegentModV1/images/potions/pure_seal_potion.png";
    public override string CustomPackedOutlinePath =>
        "res://PaleRegentModV1/images/potions/pure_seal_potion.png";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<PureSealPower>(choiceContext, target!, SealTurns, null, null);
    }
}
