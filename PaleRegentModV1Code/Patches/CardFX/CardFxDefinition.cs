using Godot;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

/// <summary>
/// 所有卡牌特效的基类。
///
/// 子类只负责创建并启动自己的 Godot 节点；位置、移动、淡入淡出、
/// 槽位替换和自动回收由 CardFxPlayer 统一处理。
/// </summary>
public abstract class CardFxDefinition
{
    protected CardFxDefinition(
        float durationSeconds,
        CardFxPlacement? placement = null)
    {
        if (durationSeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationSeconds),
                "CardFX 播放时间必须大于 0 秒。");
        }

        DurationSeconds = durationSeconds;
        Placement = placement ?? new CardFxPlacement();
    }

    /// <summary>特效总播放时间，单位为秒。</summary>
    public float DurationSeconds { get; }

    /// <summary>位置、尺寸、缩放、层级与移动参数。</summary>
    public CardFxPlacement Placement { get; }

    /// <summary>
    /// 为 true 时不按 DurationSeconds 自动回收，直到状态绑定主动停止或同槽位被替换。
    /// 适合 Hover 循环光效。
    /// </summary>
    public bool Persistent { get; init; }

    public float FadeInSeconds { get; init; }

    public float FadeOutSeconds { get; init; } = 0.15f;

    public Color Modulate { get; init; } = Colors.White;

    /// <summary>便于日志识别的可读名称。</summary>
    public virtual string DebugName => GetType().Name;

    /// <summary>创建这次播放专属的根节点。禁止复用已经挂树的节点。</summary>
    internal abstract Node2D? CreateNode(CardFxContext context);

    /// <summary>节点挂入 CardFX 图层后开始播放。</summary>
    internal abstract void Start(Node2D instance);

    /// <summary>停止前的子类钩子，例如关闭持续粒子发射。</summary>
    internal virtual void BeforeStop(Node2D instance)
    {
    }
}
