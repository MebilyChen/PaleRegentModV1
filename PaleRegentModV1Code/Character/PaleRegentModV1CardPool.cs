using BaseLib.Abstracts;
using PaleRegentModV1.PaleRegentModV1Code.Extensions;
using Godot;

namespace PaleRegentModV1.PaleRegentModV1Code.Character;

/// <summary>
/// 苍白之王专属卡池。
/// 自制卡靠卡牌基类的 [Pool(typeof(PaleRegentModV1CardPool))] 自动注入，不需要手写。
/// 这里【没有】合并原版 Regent 的卡：战斗奖励/商店只会出自制卡，更符合自定义角色定位。
/// 如果你希望奖励里也能开出原版君主的卡，取消下面注释即可：
/// <code>
/// protected override CardModel[] GenerateAllCards()
/// {
///     return ModelDb.CardPool&lt;MegaCrit.Sts2.Core.Models.CardPools.RegentCardPool&gt;().AllCards.ToArray();
/// }
/// </code>
/// </summary>
public class PaleRegentModV1CardPool : CustomCardPoolModel
{
    public override string Title => PaleRegentModV1.CharacterId; //This is not a display name.
    
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/paleregent_energy_icon.png".ImagePath();


    /* These HSV values will determine the color of your card back.
    They are applied as a shader onto an already colored image,
    so it may take some experimentation to find a color you like.
    Generally they should be values between 0 and 1. */
    public override float H => 0f; //Hue; changes the color.
    public override float S => 0f; //Saturation
    public override float V => 1.5f; //Brightness
    
    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load PaleRegentModV1/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => new("ffffff");
    
    public override bool IsColorless => false;
}