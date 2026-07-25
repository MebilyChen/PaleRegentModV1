using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using PaleRegentModV1.PaleRegentModV1Code.Character;
using PaleRegentModV1.PaleRegentModV1Code.Extensions;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// This is the base class for your mod's cards, which is set up to load the card's images from your mod's resources.
/// When creating a card, right click the Cards folder and create a new file with the Custom Card template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
[Pool(typeof(PaleRegentModV1CardPool))]
public abstract class PaleRegentModV1Card(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    
    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190
    
    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /// <summary>
    /// 【造物牌】标记（机制文档：造物流）。
    /// 佣卫/容器/虚空化形等战斗中生成的牌重写为 true，
    /// 受【驾驭 Harness】（HarnessPower）数值加成。
    /// </summary>
    public virtual bool IsCreationCard => false;

    /// <summary>
    /// 【纯粹】特质标记（机制文档：纯粹关键词，占位实现）。
    /// 带纯粹的牌不受感染/变形类效果影响（具体判定在各效果处检查此标记）。
    /// </summary>
    public virtual bool IsPure => false;

    /// <summary>
    /// 【模具】特质标记（机制文档：名词表·模具）。
    /// 带模具的牌被消耗时计数，战斗结束后按【本场消耗同名此牌数/100】概率
    /// 获得对应的“模具·[卡牌名]”遗物（见 Traits/MouldHelper）。
    /// 带模具的牌：君王佣卫/有翼佣卫牌/虚空化模。
    /// 未升级与升级同名牌合并计数（按类型聚合，不会产生两个遗物）。
    /// </summary>
    public virtual bool IsMould => false;

    /// <summary>
    /// 统一生成钩子：任何牌在战斗中被生成时触发（card == this 表示是自己刚被生成）。
    /// 【失心诅咒 LostDestiny】：若生成者（或牌主）拥有 LostDestinyPower，
    /// 给这张新生成的牌附加【失心】（X 费牌无法附加，自动跳过）。
    /// 子类如果重写此方法（FailedVessel/PureVessel/VoidGivenFocus 等），
    /// 请务必先调用 await base.AfterCardGeneratedForCombat(card, creator)。
    /// </summary>
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        await base.AfterCardGeneratedForCombat(card, creator);
        if (card != this) return;

        Creature? creature = creator?.Creature ?? Owner?.Creature;
        if (creature?.GetPower<LostDestinyPower>() != null && CardTraits.CanApplyLost(this))
        {
            CardTraits.ApplyLost(this);
        }
    }
}