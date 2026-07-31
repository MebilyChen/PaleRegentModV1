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
using PaleRegentModV1.PaleRegentModV1Code.Relics;

namespace PaleRegentModV1.PaleRegentModV1Code.Character;

public class PaleRegentModV1 : PlaceholderCharacterModel
{
    public const string CharacterId = "PaleRegent";
    public override Color MapDrawingColor => Colors.White;
    
    public static readonly Color Color = new("ffffff");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    //public override int StartingHp => 75;
    
    // ======= 初始牌组（10张，按机制文档）=======
    // 打击 x4 + 防御 x4 + 虚空攻击 x1 + 集中 x1
    // 这里的 ModelDb.Card<T>() 取的是"模板实例"，开局时游戏会自动为牌组克隆副本，直接这样写即可。
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<VoidStrike>(),
        ModelDb.Card<Focus>()
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

    // 初始遗物：苍白信物（灵魂上限+1；每回合开始只恢复[灵魂-虚空]点灵魂）
    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<PaleToken>()
    ];

    //public override CardPoolModel CardPool =>
        //RegentBase.CardPool;

    // ======= 重要修复（上一版角色选不上的根因）=======
    // 之前 RelicPool/PotionPool 指向的是原版 Regent 的池子，
    // 但我们的自定义遗物用 [Pool(typeof(PaleRegentModV1RelicPool))] 注册进了自定义池。
    // 自定义池没有被任何角色引用 → 游戏枚举"所有角色用到的池"时找不到苍白信物所属的池
    // → 选角色时抛异常 "Sequence contains no matching element" → 角色初始化中断，实际还是铁甲战士。
    // 修复：角色必须指向自定义池；自定义池内部再把原版 Regent 的内容合并进来（见各池类的 GenerateAllXxx）。
    public override RelicPoolModel RelicPool =>
        ModelDb.RelicPool<PaleRegentModV1RelicPool>();

    public override PotionPoolModel PotionPool =>
        ModelDb.PotionPool<PaleRegentModV1PotionPool>();

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
    /*
     * 【重要 · 请勿再注释掉这一行】
     *
     * PlaceholderCharacterModel 依赖 PlaceholderID 来决定
     * 用哪个原版角色作为资源"底座"。
     *
     * BaseLib 会先把这个原版角色的全套资源槽位取出来，
     * 再用下面的 Custom*Path / Custom*TexturePath 逐项替换。
     *
     * 一旦这一行被注释掉，PlaceholderID 返回基类默认值，
     * BaseLib 找不到底座角色 ->
     * 所有 Custom*Path 没有可挂载的目标 ->
     * 立绘、选人界面、商店、休息处的皮肤全部失效，
     * 而且不会抛出任何异常，日志里也不会有 ERROR。
     *
     * 2026-07-31 皮肤消失的事故就是由此引起（提交 c7101ea）。
     */
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