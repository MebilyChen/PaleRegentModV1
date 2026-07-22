using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using PaleRegentModV1.PaleRegentModV1Code.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Characters;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Character;

public class PaleRegentModV1 : PlaceholderCharacterModel
{
    public const string CharacterId = "PaleRegent";
    
    public static readonly Color Color = new("ffffff");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    //public override int StartingHp => 75;
    
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<Test>(),
        ModelDb.Card<Test>(),
        ModelDb.Card<StrikeRegent>(),
        ModelDb.Card<StrikeRegent>(),
        ModelDb.Card<StrikeRegent>(),
        ModelDb.Card<DefendRegent>(),
        ModelDb.Card<DefendRegent>(),
        ModelDb.Card<DefendRegent>(),
        ModelDb.Card<DefendRegent>(),
        ModelDb.Card<DefendRegent>()
    ];

    /*public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<DivineRight>()
    ];*/

    public override CardPoolModel CardPool => ModelDb.CardPool<PaleRegentModV1CardPool>();
    //public override RelicPoolModel RelicPool => ModelDb.RelicPool<PaleRegentModV1RelicPool>();
    //public override PotionPoolModel PotionPool => ModelDb.PotionPool<PaleRegentModV1PotionPool>();

    private static CharacterModel RegentBase =>
        ModelDb.Character<Regent>();

    public override int StartingHp =>
        RegentBase.StartingHp;

    //public override IEnumerable<CardModel> StartingDeck =>
        //RegentBase.StartingDeck;

    public override IReadOnlyList<RelicModel> StartingRelics =>
        RegentBase.StartingRelics;

    //public override CardPoolModel CardPool =>
        //RegentBase.CardPool;

    public override RelicPoolModel RelicPool =>
        RegentBase.RelicPool;

    public override PotionPoolModel PotionPool =>
        RegentBase.PotionPool;

    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets.
        These are just some of the simplest assets, given some placeholders to differentiate your character with.
        You don't have to, but you're suggested to rename these images. */
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string PlaceholderID => "regent";
    private const string ModRoot = "res://PaleRegentModV1";
    
    // Menu UI
    public override string CustomIconTexturePath => "character_icon_paleregent.png".CharacterUiPath();
    public override string CustomIconOutlineTexturePath => "character_icon_paleregent_outline.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_paleregent.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_paleregent_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_paleregent.png".CharacterUiPath();
    
    public override string CustomCharacterSelectBg => ModRoot + "/scenes/screens/char_select/char_select_bg_paleregent.tscn";
    
    // Rock Paper Scissors
    public override string CustomArmPaperTexturePath => "multiplayer_hand_paleregent_paper.png".CharacterUiPath();
    public override string CustomArmRockTexturePath => "multiplayer_hand_paleregent_rock.png".CharacterUiPath();
    public override string CustomArmScissorsTexturePath => "multiplayer_hand_paleregent_scissors.png".CharacterUiPath();
    public override string CustomArmPointingTexturePath => "multiplayer_hand_paleregent_point.png".CharacterUiPath();
    
    
    public override string CustomEnergyCounterPath =>  ModRoot + "/scenes/combat/paleregent_energy_counter.tscn";
    public override string CustomVisualPath => ModRoot + "/scenes/creature_visuals/paleregent.tscn";
    public override string CustomRestSiteAnimPath => ModRoot + "/scenes/rest_site/characters/paleregent_rest_site.tscn";
    public override string CustomMerchantAnimPath => ModRoot + "/scenes/merchant/characters/paleregent_merchant.tscn";
}