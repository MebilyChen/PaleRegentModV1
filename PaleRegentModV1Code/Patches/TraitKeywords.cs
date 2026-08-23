using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 【纯粹】【失心】【苍白】的原生式自定义 CardKeyword。
/// 注册后会像“保留 / 虚无 / 消耗”一样进入 CardModel.Keywords，
/// 并由 RitsuLib 负责牌面关键词文字。
///
/// 注意：这个类不能声明为 static；RegisterOwnedCardKeyword 的自动注册要求普通 class。
/// </summary>
[RegisterOwnedCardKeyword(
    nameof(Pure),
    //LocKeyPrefix = "PALEREGENTMODV1-PURE",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(
    nameof(Lost),
    //LocKeyPrefix = "PALEREGENTMODV1-LOST",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(
    nameof(Pale),
    //LocKeyPrefix = "PALEREGENTMODV1-PALE",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
public class TraitKeywords
{
    private const string ModId = "PaleRegentModV1";

    public static readonly CardKeyword Pure =
        ModContentRegistry.GetQualifiedKeywordId(ModId, nameof(Pure)).GetModCardKeyword();

    public static readonly CardKeyword Lost =
        ModContentRegistry.GetQualifiedKeywordId(ModId, nameof(Lost)).GetModCardKeyword();

    public static readonly CardKeyword Pale =
        ModContentRegistry.GetQualifiedKeywordId(ModId, nameof(Pale)).GetModCardKeyword();
}
