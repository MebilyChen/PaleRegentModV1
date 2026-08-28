using Godot;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

/// <summary>
/// 由一张或多张 PNG 构成的逐帧卡牌特效。
/// </summary>
public sealed class PngSequenceCardFx : CardFxDefinition
{
    private static readonly StringName AnimationName = "card_fx";
    private static readonly NodePath SpritePath = "AnimatedSprite";

    private readonly string[] _framePaths;

    public PngSequenceCardFx(
        IEnumerable<string> framePaths,
        float durationSeconds,
        CardFxPlacement? placement = null,
        bool loop = false)
        : base(durationSeconds, placement)
    {
        ArgumentNullException.ThrowIfNull(framePaths);

        _framePaths = framePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();

        if (_framePaths.Length == 0)
        {
            throw new ArgumentException(
                "PNG CardFX 至少需要一张图片。",
                nameof(framePaths));
        }

        Loop = loop;
    }

    public bool Loop { get; }

    internal override Node2D? CreateNode(CardFxContext context)
    {
        SpriteFrames frames = new();
        frames.ClearAll();
        frames.AddAnimation(AnimationName);
        frames.SetAnimationSpeed(
            AnimationName,
            _framePaths.Length / DurationSeconds);
        frames.SetAnimationLoop(AnimationName, Loop || Persistent);

        Texture2D? firstTexture = null;

        foreach (string path in _framePaths)
        {
            Texture2D? texture = CardFxResources.LoadTexture(path);

            if (texture is null)
            {
                return null;
            }

            firstTexture ??= texture;
            frames.AddFrame(AnimationName, texture);
        }

        Node2D root = new()
        {
            Name = "PngSequenceCardFx"
        };

        AnimatedSprite2D sprite = new()
        {
            Name = "AnimatedSprite",
            SpriteFrames = frames,
            Animation = AnimationName,
            Centered = true
        };

        if (Placement.Size != Vector2.Zero && firstTexture is not null)
        {
            Vector2 sourceSize = firstTexture.GetSize();

            if (sourceSize.X > 0.0f && sourceSize.Y > 0.0f)
            {
                sprite.Scale = Placement.Size / sourceSize;
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
