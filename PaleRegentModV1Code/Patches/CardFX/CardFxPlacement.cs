using Godot;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

/// <summary>
/// CardFX 的视口定位与运动参数。
/// </summary>
public sealed record CardFxPlacement
{
    /// <summary>起始位置依附的参照点。</summary>
    public CardFxAnchor Anchor { get; init; } = CardFxAnchor.Card;

    /// <summary>相对参照点的像素偏移；Absolute 模式下即为绝对坐标。</summary>
    public Vector2 Offset { get; init; } = Vector2.Zero;

    /// <summary>
    /// 目标显示尺寸。仅对 PNG/GIF 图片特效生效；Vector2.Zero 表示使用原图尺寸。
    /// </summary>
    public Vector2 Size { get; init; } = Vector2.Zero;

    /// <summary>节点缩放。粒子场景通常使用此参数控制整体大小。</summary>
    public Vector2 Scale { get; init; } = Vector2.One;

    /// <summary>顺时针旋转角度，单位为度。</summary>
    public float RotationDegrees { get; init; }

    /// <summary>CanvasItem 的 ZIndex。</summary>
    public int ZIndex { get; init; }

    /// <summary>是否从起点移动到终点。</summary>
    public bool MoveToEnd { get; init; }

    /// <summary>终点所依附的参照点。</summary>
    public CardFxAnchor EndAnchor { get; init; } = CardFxAnchor.Card;

    /// <summary>终点相对参照点的偏移；Absolute 模式下即为绝对坐标。</summary>
    public Vector2 EndOffset { get; init; } = Vector2.Zero;

    /// <summary>
    /// 起点到终点的移动时间。小于等于 0 时由特效播放时间决定。
    /// </summary>
    public float MoveDuration { get; init; }

    public Tween.TransitionType MoveTransition { get; init; } =
        Tween.TransitionType.Quad;

    public Tween.EaseType MoveEase { get; init; } =
        Tween.EaseType.Out;
}
