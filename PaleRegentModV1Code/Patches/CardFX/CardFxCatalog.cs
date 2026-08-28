using Godot;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

/// <summary>
/// 所有“卡牌 + 状态 + 特效”的集中关联目录。
///
/// 新卡特效只需要在 RegisterAll 中追加一段注册，不必修改状态测试机或播放中枢。
/// </summary>
public static class CardFxCatalog
{
    private const string SovereignFxRoot =
        "res://PaleRegentModV1/images/vfx/sovereign_blade/";

    private const string CommonGlowPath =
        "res://PaleRegentModV1/scenes/vfx/energy/common_glow_transparent.png";

    private static bool _registered;

    public static void RegisterAll()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;

        RegisterTestCard();
    }

    private static void RegisterTestCard()
    {
        string[] starFrames =
        [
            SovereignFxRoot + "sovereign_blade_star_center.png",
            SovereignFxRoot + "sovereign_blade_star_center2.png"
        ];

        string[] hoverFrames =
        [
            CommonGlowPath
        ];

        CardFxRegistry.For<Strike>()
            .On(
                CardFxState.HoverEnter,
                new PngSequenceCardFx(
                    hoverFrames,
                    durationSeconds: 0.8f,
                    placement: new CardFxPlacement
                    {
                        Anchor = CardFxAnchor.Card,
                        Size = new Vector2(180.0f, 180.0f),
                        ZIndex = 1
                    },
                    loop: true)
                {
                    Persistent = true,
                    FadeInSeconds = 0.12f,
                    FadeOutSeconds = 0.18f,
                    Modulate = new Color(1.0f, 1.0f, 1.0f, 0.8f)
                },
                slot: "test_hover_star")
            .StopOn(
                CardFxState.HoverExit,
                slot: "test_hover_star")
            .On(
                CardFxState.Selected,
                new PngSequenceCardFx(
                    starFrames,
                    durationSeconds: 0.35f,
                    placement: new CardFxPlacement
                    {
                        Anchor = CardFxAnchor.Card,
                        Size = new Vector2(260.0f, 260.0f),
                        ZIndex = 2
                    })
                {
                    FadeOutSeconds = 0.12f,
                    Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f)
                },
                slot: "test_selected_star")
            .On(
                CardFxState.Cancelled,
                new PngSequenceCardFx(
                    starFrames.Reverse(),
                    durationSeconds: 0.3f,
                    placement: new CardFxPlacement
                    {
                        Anchor = CardFxAnchor.Card,
                        Size = new Vector2(150.0f, 150.0f),
                        ZIndex = 2
                    })
                {
                    FadeOutSeconds = 0.2f,
                    Modulate = new Color(1.0f, 1.0f, 1.0f, 0.7f)
                },
                slot: "test_cancel_star")
            .On(
                CardFxState.Played,
                new GodotParticleCardFx(
                    "res://PaleRegentModV1/card_fx/particles/" +
                    "sovereign_star_burst.tscn",
                    durationSeconds: 1.4f,
                    placement: new CardFxPlacement
                    {
                        Anchor = CardFxAnchor.Pointer,
                        Scale = new Vector2(0.85f, 0.85f),
                        ZIndex = 5
                    })
                {
                    FadeOutSeconds = 0.25f,
                    OneShot = true
                },
                slot: "test_played_burst",
                replayPolicy: CardFxReplayPolicy.Parallel);
    }
}
