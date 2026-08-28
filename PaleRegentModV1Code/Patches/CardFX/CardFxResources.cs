using Godot;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

internal static class CardFxResources
{
    private static readonly Dictionary<string, Texture2D> Textures =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, SpriteFrames> SpriteFrameSets =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, PackedScene> Scenes =
        new(StringComparer.OrdinalIgnoreCase);

    public static Texture2D? LoadTexture(string path)
    {
        return LoadCached(path, Textures, "Texture2D");
    }

    public static SpriteFrames? LoadSpriteFrames(string path)
    {
        return LoadCached(path, SpriteFrameSets, "SpriteFrames");
    }

    public static PackedScene? LoadScene(string path)
    {
        return LoadCached(path, Scenes, "PackedScene");
    }

    private static T? LoadCached<T>(
        string path,
        Dictionary<string, T> cache,
        string kind)
        where T : Resource
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            GD.PushError($"[CardFX] {kind} 路径为空。");
            return null;
        }

        if (cache.TryGetValue(path, out T? cached))
        {
            return cached;
        }

        if (!ResourceLoader.Exists(path))
        {
            GD.PushError($"[CardFX] 找不到 {kind} 资源：{path}");
            return null;
        }

        T? resource = GD.Load<T>(path);

        if (resource is null)
        {
            GD.PushError($"[CardFX] 无法加载 {kind} 资源：{path}");
            return null;
        }

        cache[path] = resource;
        return resource;
    }
}
