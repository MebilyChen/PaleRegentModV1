namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

/// <summary>
/// CardFX 可响应的卡牌交互状态。
/// </summary>
public enum CardFxState
{
    HoverEnter,
    HoverExit,
    Selected,
    Cancelled,
    Played
}

/// <summary>
/// 特效坐标所依附的参照点。
/// </summary>
public enum CardFxAnchor
{
    /// <summary>当前卡牌在视口中的中心位置。</summary>
    Card,

    /// <summary>当前鼠标位置。</summary>
    Pointer,

    /// <summary>战斗视口中心。</summary>
    ViewportCenter,

    /// <summary>直接使用 Offset 作为绝对视口坐标。</summary>
    Absolute
}

/// <summary>
/// 同一卡牌、同一槽位已有特效时的处理方式。
/// </summary>
public enum CardFxReplayPolicy
{
    Replace,
    IgnoreWhilePlaying,
    Parallel
}
