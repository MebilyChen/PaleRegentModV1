using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Potions;

/// <summary>
/// 【容器药水】稀有药水（机制文档：药水区，占位设计）。
/// 战斗中使用：将 2 张【容器】加入手牌。
/// </summary>
public class VesselPotion : PaleRegentModV1Potion
{
    /// <summary>生成的容器数量。</summary>
    private const int VesselCount = 2;

    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    // 占位图：先复用统一占位，正式图做好后替换路径即可
    public override string CustomPackedImagePath =>
        "res://PaleRegentModV1/images/potions/vessel_potion.png";
    public override string CustomPackedOutlinePath =>
        "res://PaleRegentModV1/images/potions/vessel_potion.png";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await CardPileCmd.AddToCombatAndPreview<Vessel>(target!, PileType.Hand, VesselCount, target!.Player);
    }
}
