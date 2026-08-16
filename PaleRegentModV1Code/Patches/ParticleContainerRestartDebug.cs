using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

[HarmonyPatch(typeof(NParticlesContainer), nameof(NParticlesContainer.Restart))]
public static class ParticleContainerRestartDebug
{
    [HarmonyPrefix]
    public static void Prefix(NParticlesContainer __instance)
    {
        GD.Print(
            $"[ParticleRestart] CALL name={__instance.Name}, " +
            $"children={__instance.GetChildCount()}, " +
            $"globalPos={__instance.GlobalPosition}, " +
            $"visible={__instance.Visible}"
        );
    }

    [HarmonyPostfix]
    public static void Postfix(NParticlesContainer __instance)
    {
        PrintParticles(__instance);
    }

    private static void PrintParticles(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            // 就是你原本的这一段
            if (child is GpuParticles2D particles)
            {
                particles.SelfModulate = Colors.White;
                particles.Modulate = Colors.White;
                particles.ZIndex = 999;
                
                string texturePath =
                    particles.Texture == null
                        ? "NULL"
                        : particles.Texture.ResourcePath;

                string processMaterial =
                    particles.ProcessMaterial == null
                        ? "NULL"
                        : particles.ProcessMaterial.GetType().FullName;

                GD.Print(
                    $"[ParticleRestart] particle={particles.Name}, " +
                    $"emitting={particles.Emitting}, " +
                    $"oneShot={particles.OneShot}, " +
                    $"amount={particles.Amount}, " +
                    $"lifetime={particles.Lifetime}, " +
                    $"visible={particles.Visible}, " +
                    $"globalPos={particles.GlobalPosition}, " +
                    $"texture={texturePath}, " +
                    $"processMaterial={processMaterial}, " +
                    $"modulate={particles.Modulate}, " +
                    $"selfModulate={particles.SelfModulate}, " +
                    $"zIndex={particles.ZIndex}, " +
                    $"visibilityRect={particles.VisibilityRect}"
                );
            }

            PrintParticles(child);
        }
    }
}
