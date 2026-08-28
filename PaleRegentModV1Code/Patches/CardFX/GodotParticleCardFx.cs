using Godot;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

/// <summary>
/// 由独立 Godot .tscn 场景组成的 CardFX。
///
/// 场景内可自由组合 GPUParticles2D、CPUParticles2D、Sprite2D、Line2D、
/// ShaderMaterial 和 AnimationPlayer。运行时负责实例化、定位、移动、启动与回收。
/// </summary>
public sealed class GodotParticleCardFx : CardFxDefinition
{
    public GodotParticleCardFx(
        string scenePath,
        float durationSeconds,
        CardFxPlacement? placement = null)
        : base(durationSeconds, placement)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            throw new ArgumentException(
                "Godot 粒子 CardFX 的场景路径不能为空。",
                nameof(scenePath));
        }

        ScenePath = scenePath;
    }

    public string ScenePath { get; }

    /// <summary>需要自动播放的 AnimationPlayer 动画名；为空则不自动播放。</summary>
    public string AnimationName { get; init; } = string.Empty;

    /// <summary>是否自动启动场景中的全部 2D 粒子节点。</summary>
    public bool AutoStartParticles { get; init; } = true;

    /// <summary>
    /// 是否强制粒子使用 OneShot。悬停持续效果应设为 false，并配合 Persistent=true。
    /// </summary>
    public bool OneShot { get; init; } = true;

    /// <summary>
    /// 粒子模拟速度倍率。大于 1 加速，小于 1 减速。
    /// </summary>
    public float ParticleSpeedScale { get; init; } = 1.0f;

    internal override Node2D? CreateNode(CardFxContext context)
    {
        PackedScene? scene = CardFxResources.LoadScene(ScenePath);

        if (scene is null)
        {
            return null;
        }

        Node instance = scene.Instantiate();

        if (instance is Node2D node2D)
        {
            node2D.Name = "GodotParticleCardFx";
            return node2D;
        }

        Node2D wrapper = new()
        {
            Name = "GodotParticleCardFx"
        };

        wrapper.AddChild(instance);
        return wrapper;
    }

    internal override void Start(Node2D instance)
    {
        foreach (Node node in EnumerateTree(instance))
        {
            switch (node)
            {
                case GpuParticles2D gpu when AutoStartParticles:
                    gpu.OneShot = OneShot && !Persistent;
                    gpu.SpeedScale *= ParticleSpeedScale;
                    gpu.Emitting = true;
                    gpu.Restart();
                    break;

                case CpuParticles2D cpu when AutoStartParticles:
                    cpu.OneShot = OneShot && !Persistent;
                    cpu.SpeedScale *= ParticleSpeedScale;
                    cpu.Emitting = true;
                    cpu.Restart();
                    break;

                case AnimationPlayer player
                    when !string.IsNullOrEmpty(AnimationName) &&
                         player.HasAnimation(AnimationName):
                    player.Play(AnimationName);
                    break;
            }
        }
    }

    internal override void BeforeStop(Node2D instance)
    {
        foreach (Node node in EnumerateTree(instance))
        {
            switch (node)
            {
                case GpuParticles2D gpu:
                    gpu.Emitting = false;
                    break;

                case CpuParticles2D cpu:
                    cpu.Emitting = false;
                    break;

                case AnimationPlayer player:
                    player.Stop();
                    break;
            }
        }
    }

    private static IEnumerable<Node> EnumerateTree(Node root)
    {
        yield return root;

        foreach (Node child in root.GetChildren())
        {
            foreach (Node descendant in EnumerateTree(child))
            {
                yield return descendant;
            }
        }
    }
}
