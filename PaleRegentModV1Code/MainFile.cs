using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace PaleRegentModV1.PaleRegentModV1Code;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "PaleRegentModV1"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        CheckAnimationResources();
        
        PaleRegentModV1.PaleRegentModV1Code.Resources.VoidResource.Register();
        Harmony harmony = new(ModId);

        harmony.PatchAll();
    }
    
    private static void CheckAnimationResources()
    {
        string[] paths =
        {
            "res://PaleRegentModV1/scenes/creature_visuals/paleregent.tscn",
            "res://PaleRegentModV1/animations/characters/paleregent/paleregent.png",
            "res://PaleRegentModV1/animations/characters/paleregent/paleregent.atlas",
            "res://PaleRegentModV1/animations/characters/paleregent/paleregent.skel",
            "res://PaleRegentModV1/animations/characters/paleregent/paleregent_skel_data.tres"
        };

        GD.PrintErr("========== PaleRegent resource check started ==========");

        foreach (string path in paths)
        {
            // FileExists 更适合检查 atlas、skel 这类原始文件；
            // ResourceLoader.Exists 用来判断 Godot 能否把它当资源加载。
            bool fileExists = Godot.FileAccess.FileExists(path);
            bool resourceExists = ResourceLoader.Exists(path);

            GD.PrintErr(
                $"[PaleRegent Resource Check] " +
                $"file={fileExists}, resource={resourceExists}, path={path}"
            );
        }

        GD.PrintErr("========== PaleRegent resource check finished ==========");
    }
}
