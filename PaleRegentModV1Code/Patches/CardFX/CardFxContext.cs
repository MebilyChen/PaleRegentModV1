using Godot;
using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

/// <summary>
/// 一次卡牌交互状态变化传给 CardFX 的只读上下文。
/// </summary>
public sealed record CardFxContext(
    CardModel Card,
    CardFxState State,
    Vector2 CardPosition,
    Vector2 PointerPosition,
    Vector2 ViewportSize)
{
    /// <summary>
    /// 将参照点与偏移解析为战斗视口坐标。
    /// </summary>
    public Vector2 Resolve(
        CardFxAnchor anchor,
        Vector2 offset)
    {
        Vector2 origin = anchor switch
        {
            CardFxAnchor.Card => CardPosition,
            CardFxAnchor.Pointer => PointerPosition,
            CardFxAnchor.ViewportCenter => ViewportSize * 0.5f,
            CardFxAnchor.Absolute => Vector2.Zero,
            _ => CardPosition
        };

        return origin + offset;
    }
}
