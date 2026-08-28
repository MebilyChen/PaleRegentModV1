using Godot;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

/// <summary>
/// GIF 卡牌特效。
///
/// Godot 4.5 不原生导入 GIF；请先用 tools/gif_to_spriteframes.py
/// 将 GIF 转为 PNG 帧和 SpriteFrames .tres，再把 .tres 路径传入本类。
/// </summary>
public sealed class GifCardFx : CardFxDefinition
{
    private static readonly NodePath SpritePath = "AnimatedSprite";

    public GifCardFx(
        string spriteFramesPath,
        float durationSeconds,
        CardFxPlacement? placement = null,
        string animationName = "default",
        bool loop = false)
        : base(durationSeconds, placement)
    {
        if (string.IsNullOrWhiteSpace(spriteFramesPath))
        {
            throw new ArgumentException(
                "GIF CardFX 的 SpriteFrames 路径不能为空。",
                nameof(spriteFramesPath));
        }

        SpriteFramesPath = spriteFramesPath;
        AnimationName = animationName;
        Loop = loop;
    }

    public string SpriteFramesPath { get; }

    public StringName AnimationName { get; }

    public bool Loop { get; }

    internal override Node2D? CreateNode(CardFxContext context)
    {
        SpriteFrames? cachedFrames =
            CardFxResources.LoadSpriteFrames(SpriteFramesPath);

        if (cachedFrames is null)
        {
            return null;
        }

        SpriteFrames frames =
            (SpriteFrames)cachedFrames.Duplicate(true);

        if (!frames.HasAnimation(AnimationName))
        {
            GD.PushError(
                $"[CardFX] SpriteFrames {SpriteFramesPath} 中不存在动画 " +
                $"{AnimationName}。");
            return null;
        }

        frames.SetAnimationLoop(
            AnimationName,
            Loop || Persistent);

        int frameCount = frames.GetFrameCount(AnimationName);
        double fps = frames.GetAnimationSpeed(AnimationName);
        double sourceDuration = 0.0;

        for (int i = 0; i < frameCount; i++)
        {
            sourceDuration += frames.GetFrameDuration(AnimationName, i);
        }

        sourceDuration = fps > 0.0
            ? sourceDuration / fps
            : DurationSeconds;

        Node2D root = new()
        {
            Name = "GifCardFx"
        };

        AnimatedSprite2D sprite = new()
        {
            Name = "AnimatedSprite",
            SpriteFrames = frames,
            Animation = AnimationName,
            Centered = true,
            SpeedScale = (float)(sourceDuration / DurationSeconds)
        };

        if (Placement.Size != Vector2.Zero && frameCount > 0)
        {
            Texture2D? firstTexture =
                frames.GetFrameTexture(AnimationName, 0);

            if (firstTexture is not null)
            {
                Vector2 sourceSize = firstTexture.GetSize();

                if (sourceSize.X > 0.0f && sourceSize.Y > 0.0f)
                {
                    sprite.Scale = Placement.Size / sourceSize;
                }
            }
        }

        root.AddChild(sprite);
        return root;
    }

    internal override void Start(Node2D instance)
    {
        instance.GetNodeOrNull<AnimatedSprite2D>(SpritePath)?.Play(AnimationName);
    }
}
