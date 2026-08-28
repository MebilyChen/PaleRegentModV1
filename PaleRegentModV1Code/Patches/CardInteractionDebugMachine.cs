using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace PaleRegentModV1.PaleRegentModV1Code.Debug;

/// <summary>
/// 卡牌交互状态测试机。
///
/// 这版不再 Patch TryManualPlay / Cleanup / CancelPlayCard。
///
/// 核心做法：
/// 在 NMouseCardPlay.Start() 时，直接订阅当前 NMouseCardPlay
/// 自己的 Finished(bool success) Godot 信号。
///
/// NCardPlay.Cleanup(bool isFinished) 会：
///     EmitSignal(Finished, isFinished)
///
/// 因此：
///     Finished(false) = 真正取消
///     Finished(true)  = 真正成功确认打出
///
/// 这样可以避开：
/// 1. CancelPlayCard 在某些流程中被“调用但实际没有取消”的情况
/// 2. TryManualPlay 这种很小的方法 Harmony Patch 没有命中的情况
/// </summary>
public enum CardInteractionState
{
    None,
    Hover,
    Selected,
    Played
}

public static class CardInteractionDebugMachine
{
    private static CardInteractionState _state = CardInteractionState.None;
    private static CardModel? _activeCard;

    private static CanvasLayer? _debugLayer;
    private static PanelContainer? _stateBox;
    private static Label? _stateLabel;


    // =========================================================
    // 监听范围
    // =========================================================

    /// <summary>
    /// 默认只监听 PaleRegentModV1 自己 DLL 里的卡牌。
    ///
    /// 如果想测试所有卡：
    ///     return true;
    /// </summary>
    public static bool ShouldTrack(CardModel? card)
    {
        if (card == null)
            return false;

        return card.GetType().Assembly ==
               typeof(CardInteractionDebugMachine).Assembly;
    }


    // =========================================================
    // Hover
    // =========================================================

    public static void OnHover(
        CardModel? card,
        bool hovered)
    {
        if (!ShouldTrack(card) || card == null)
            return;


        if (hovered)
        {
            // Selected 的优先级高于 Hover。
            if (_state == CardInteractionState.Selected &&
                ReferenceEquals(_activeCard, card))
            {
                return;
            }

            _activeCard = card;
            _state = CardInteractionState.Hover;

            ShowStateBox(
                card,
                CardInteractionState.Hover
            );

            GD.Print(
                $"[CardInteraction] {card.Title} -> HOVER"
            );

            return;
        }


        // Unhover 只能清理 Hover。
        // 不能把 Selected 擦掉。
        if (_state == CardInteractionState.Hover &&
            ReferenceEquals(_activeCard, card))
        {
            _activeCard = null;
            _state = CardInteractionState.None;

            HideStateBox();

            GD.Print(
                $"[CardInteraction] {card.Title} -> HOVER END"
            );
        }
    }


    // =========================================================
    // Selected
    // =========================================================

    public static void OnSelected(
        NMouseCardPlay cardPlay)
    {
        CardModel? card =
            cardPlay.Holder?.CardModel;

        if (!ShouldTrack(card) || card == null)
            return;


        _activeCard = card;
        _state = CardInteractionState.Selected;


        ShowStateBox(
            card,
            CardInteractionState.Selected
        );


        GD.Print(
            $"[CardInteraction] {card.Title} -> SELECTED"
        );


        // =====================================================
        // 关键修复：
        //
        // 不再尝试 Harmony Patch TryManualPlay。
        //
        // 直接监听当前 NMouseCardPlay 节点原生的 Finished 信号。
        //
        // Cleanup(true)  -> Finished(true)
        // Cleanup(false) -> Finished(false)
        //
        // 节点结束后本来就会 QueueFree，所以不需要长期保存引用。
        // =====================================================

        Callable finishedCallable =
            Callable.From<bool>(
                success =>
                {
                    OnCardPlayFinished(
                        cardPlay,
                        card,
                        success
                    );
                }
            );


        cardPlay.Connect(
            NCardPlay.SignalName.Finished,
            finishedCallable
        );


        GD.Print(
            $"[CardInteraction] {card.Title} -> FINISHED SIGNAL CONNECTED"
        );
    }


    // =========================================================
    // Finished
    // =========================================================

    private static void OnCardPlayFinished(
        NMouseCardPlay cardPlay,
        CardModel card,
        bool success)
    {
        GD.Print(
            $"[CardInteraction] {card.Title} -> FINISHED SIGNAL success={success}"
        );


        // 保险：
        // 如果这已经不是我们当前正在跟踪的牌，
        // 不让旧信号覆盖新的状态。
        if (!ReferenceEquals(_activeCard, card))
        {
            GD.Print(
                $"[CardInteraction] {card.Title} -> " +
                "FINISHED IGNORED (not active card)"
            );

            return;
        }


        // =====================================================
        // 真正取消
        // =====================================================

        if (!success)
        {
            _activeCard = null;
            _state = CardInteractionState.None;

            HideStateBox();

            GD.Print(
                $"[CardInteraction] {card.Title} -> CANCELLED"
            );

            return;
        }


        // =====================================================
        // 真正成功出牌
        // =====================================================

        Vector2 mousePosition =
            GetMousePosition(cardPlay);


        _state = CardInteractionState.Played;


        ShowStateBox(
            card,
            CardInteractionState.Played,
            mousePosition
        );


        SpawnPlayedMarker(
            card,
            mousePosition
        );


        GD.Print(
            $"[CardInteraction] {card.Title} -> PLAYED " +
            $"Mouse=({mousePosition.X:0},{mousePosition.Y:0})"
        );


        // 已经完成本次交互。
        //
        // 注意：
        // 左上角仍然保留 Played 状态，
        // 只是 activeCard 清掉，避免下一次交互串状态。
        _activeCard = null;
    }


    // =========================================================
    // 鼠标位置
    // =========================================================

    private static Vector2 GetMousePosition(
        NMouseCardPlay cardPlay)
    {
        // 优先从当前 CardPlay 自己拿 Viewport。
        if (GodotObject.IsInstanceValid(cardPlay))
        {
            Viewport? viewport =
                cardPlay.GetViewport();

            if (viewport != null &&
                GodotObject.IsInstanceValid(viewport))
            {
                return viewport.GetMousePosition();
            }
        }


        // 兜底从 CombatRoom 拿。
        NCombatRoom? room =
            NCombatRoom.Instance;

        if (room != null &&
            GodotObject.IsInstanceValid(room))
        {
            Viewport? viewport =
                room.GetViewport();

            if (viewport != null &&
                GodotObject.IsInstanceValid(viewport))
            {
                return viewport.GetMousePosition();
            }
        }


        GD.PrintErr(
            "[CardInteraction] Could not obtain mouse position."
        );

        return Vector2.Zero;
    }


    // =========================================================
    // Debug UI
    // =========================================================

    private static bool EnsureDebugUi()
    {
        NCombatRoom? room =
            NCombatRoom.Instance;

        if (room == null ||
            !GodotObject.IsInstanceValid(room))
        {
            return false;
        }


        // 当前战斗已经创建过 Debug Layer。
        if (_debugLayer != null &&
            GodotObject.IsInstanceValid(_debugLayer))
        {
            return true;
        }


        // 新战斗：
        // 清掉上一场已经失效的 Godot 引用。
        _debugLayer = null;
        _stateBox = null;
        _stateLabel = null;


        _debugLayer = new CanvasLayer
        {
            Name = "CardInteractionDebugLayer",
            Layer = 100
        };


        room.AddChild(_debugLayer);


        _stateBox = CreateTextBox(
            "",
            out _stateLabel
        );


        _stateBox.Position =
            new Vector2(
                20,
                20
            );


        _stateBox.Visible = false;


        _debugLayer.AddChild(
            _stateBox
        );


        return true;
    }


    private static void ShowStateBox(
        CardModel card,
        CardInteractionState state,
        Vector2? position = null)
    {
        if (!EnsureDebugUi())
            return;


        if (_stateBox == null ||
            _stateLabel == null ||
            !GodotObject.IsInstanceValid(_stateBox) ||
            !GodotObject.IsInstanceValid(_stateLabel))
        {
            return;
        }


        string extra = "";


        if (position.HasValue)
        {
            Vector2 p =
                position.Value;


            extra =
                $"\nMouse: ({p.X:0}, {p.Y:0})";
        }


        _stateLabel.Text =
            $"Card: {card.Title}\n" +
            $"State: {state}" +
            extra;


        _stateBox.Visible = true;
    }


    private static void HideStateBox()
    {
        if (_stateBox == null ||
            !GodotObject.IsInstanceValid(_stateBox))
        {
            return;
        }


        _stateBox.Visible = false;
    }


    // =========================================================
    // Played 落点文本占位符
    // =========================================================

    private static void SpawnPlayedMarker(
        CardModel card,
        Vector2 mousePosition)
    {
        if (!EnsureDebugUi())
            return;


        if (_debugLayer == null ||
            !GodotObject.IsInstanceValid(_debugLayer))
        {
            return;
        }


        PanelContainer box =
            CreateTextBox(
                $"PLAYED\n{card.Title}",
                out _
            );


        // 稍微偏移，避免正好被鼠标指针挡住。
        box.Position =
            mousePosition +
            new Vector2(
                15,
                15
            );


        _debugLayer.AddChild(
            box
        );


        // 1.5 秒以后淡出。
        Tween tween =
            box.CreateTween();


        tween.TweenInterval(
            1.5
        );


        tween.TweenProperty(
            box,
            "modulate:a",
            0.0f,
            0.35f
        );


        tween.TweenCallback(
            Callable.From(
                () =>
                {
                    if (GodotObject.IsInstanceValid(box))
                    {
                        box.QueueFree();
                    }
                }
            )
        );
    }


    // =========================================================
    // 创建简单 Debug 文本框
    // =========================================================

    private static PanelContainer CreateTextBox(
        string text,
        out Label label)
    {
        PanelContainer panel =
            new()
            {
                MouseFilter =
                    Control.MouseFilterEnum.Ignore
            };


        StyleBoxFlat background =
            new()
            {
                BgColor =
                    new Color(
                        0.03f,
                        0.03f,
                        0.03f,
                        0.90f
                    ),

                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,

                BorderColor =
                    new Color(
                        1.0f,
                        1.0f,
                        1.0f,
                        0.65f
                    ),

                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6
            };


        panel.AddThemeStyleboxOverride(
            "panel",
            background
        );


        label =
            new Label
            {
                Text = text,

                CustomMinimumSize =
                    new Vector2(
                        260,
                        65
                    ),

                MouseFilter =
                    Control.MouseFilterEnum.Ignore
            };


        label.AddThemeFontSizeOverride(
            "font_size",
            24
        );


        label.AddThemeColorOverride(
            "font_color",
            Colors.White
        );


        label.AddThemeColorOverride(
            "font_outline_color",
            Colors.Black
        );


        label.AddThemeConstantOverride(
            "outline_size",
            6
        );


        panel.AddChild(
            label
        );


        return panel;
    }
}


// =====================================================================
// PATCH 1
//
// 手牌 Hover
// =====================================================================

[HarmonyPatch(
    typeof(NHandCardHolder),
    "DoCardHoverEffects"
)]
internal static class CardInteractionHoverPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        NHandCardHolder __instance,
        bool isHovered)
    {
        CardInteractionDebugMachine.OnHover(
            __instance.CardModel,
            isHovered
        );
    }
}


// =====================================================================
// PATCH 2
//
// 鼠标开始拿牌
//
// 只需要 Patch Start。
// 后续 Played / Cancel 不再靠 Harmony Patch，
// 而是直接监听这个 NMouseCardPlay 实例的 Finished(bool success) 信号。
// =====================================================================

[HarmonyPatch(
    typeof(NMouseCardPlay),
    nameof(NMouseCardPlay.Start)
)]
internal static class CardInteractionSelectedPatch
{
    [HarmonyPrefix]
    private static void Prefix(
        NMouseCardPlay __instance)
    {
        CardInteractionDebugMachine.OnSelected(
            __instance
        );
    }
}
