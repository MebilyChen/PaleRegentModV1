using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

// 能量 VFX：贴图原有 Alpha 保持不变；最终 RGB 强制为有明暗层次的白色。
[HarmonyPatch(typeof(NParticlesContainer), nameof(NParticlesContainer.Restart))]
public static class ParticleContainerRestartDebug
{
    // ====== 图片配置区 ======
    private static readonly Dictionary<string, string> TexturePathByParticle =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vfx_common_glow"] =
                "res://PaleRegentModV1/scenes/vfx/energy/common_glow_transparent.png",

            ["vfx_common_ray"] =
                "res://PaleRegentModV1/scenes/vfx/energy/common_ray.png",

            ["vfx_common_ring_polar_a"] =
                "res://PaleRegentModV1/images/charui/paleregent_orb_layer_1.png",

            ["vfx_starry_impact_small_stars"] =
                "res://PaleRegentModV1/scenes/vfx/energy/common_glow_transparent.png",

            ["vfx_starry_impact_constellation_small_a"] =
                "res://PaleRegentModV1/images/charui/paleregent_orb_layer_5.png",

            ["vfx_starry_impact_constellation_small_b"] =
                "res://PaleRegentModV1/scenes/vfx/energy/common_glow_transparent.png",
        };
    // =========================

    // 只调 paleregent_orb_layer_1 的不透明度，不改变它的图案。
    // 0.00 = 完全透明；1.00 = 不透明。
    private static readonly Dictionary<string, float> OpacityByTexturePath =
        new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["res://PaleRegentModV1/images/charui/paleregent_orb_layer_1.png"] = 0.35f,
        };

    private static readonly Dictionary<string, Texture2D> TextureCache =
        new Dictionary<string, Texture2D>(StringComparer.Ordinal);

    // 运行时 PCK 已包含这些原版粒子场景。清除 .godot 后，外层实例有时会生成空类型占位节点；
    // 此处只在该异常情况下重新实例化原场景，不重建或替换任何 PNG、材质、粒子参数。
    private static readonly Dictionary<string, string> OriginalScenePathByParticle =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vfx_common_glow"] =
                "res://PaleRegentModV1/scenes/vfx/energy/vfx_common_glow.tscn",
            ["vfx_common_ray"] =
                "res://PaleRegentModV1/scenes/vfx/energy/vfx_common_ray.tscn",
            ["vfx_common_ring_polar_a"] =
                "res://PaleRegentModV1/scenes/vfx/energy/vfx_common_ring_polar_a.tscn",
            ["vfx_starry_impact_small_stars"] =
                "res://PaleRegentModV1/scenes/vfx/energy/vfx_starry_impact_small_stars.tscn",
            ["vfx_starry_impact_constellation_small_a"] =
                "res://PaleRegentModV1/scenes/vfx/energy/vfx_starry_impact_constellation_small.tscn",
            ["vfx_starry_impact_constellation_small_b"] =
                "res://PaleRegentModV1/scenes/vfx/energy/vfx_starry_impact_constellation_small.tscn",
        };

    private static readonly Dictionary<string, string[]> OriginalParticleNamesByContainer =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["EnergyVfxBack"] =
            [
                "vfx_common_glow",
                "vfx_common_ray",
            ],
            ["EnergyVfxFront"] =
            [
                "vfx_common_ring_polar_a",
                "vfx_starry_impact_small_stars",
                "vfx_starry_impact_constellation_small_a",
                "vfx_starry_impact_constellation_small_b",
            ],
        };

    // 注意：此 Shader 不处理黑底透明。
    // 它只将最终色调转为白色，并通过亮度保留渐变、星点和图案细节。
    private const string WhiteTintShaderCode = @"
shader_type canvas_item;
render_mode blend_mix;

// 越高越白亮。若仍偏灰，将 1.60 改成 2.00；太亮则改成 1.20。
uniform float white_gain : hint_range(0.1, 3.0, 0.01) = 1.60;

void fragment()
{
    vec4 texture_color = texture(TEXTURE, UV);

    // 从原图取亮度，因此仍能看见它的图案、亮暗和渐变。
    float brightness = dot(
        texture_color.rgb,
        vec3(0.299, 0.587, 0.114)
    );

    // 让中间亮度也更接近白色，同时不把所有像素涂成同一个纯白。
    float white_value = clamp(
        pow(brightness, 0.55) * white_gain,
        0.0,
        1.0
    );

    // 忽略粒子原生的 RGB 染色（橙色来源），只沿用其 Alpha 淡入淡出。
    COLOR = vec4(
        vec3(white_value),
        texture_color.a * COLOR.a
    );
}
";

    private static ShaderMaterial _whiteTintMaterial;

    [HarmonyPrefix]
    public static bool Prefix(NParticlesContainer __instance)
    {
        if (!IsEnergyCounterFx(__instance))
        {
            return true;
        }

        bool restoredAny = RestoreOriginalParticleScenes(__instance);
        AssignTexturesOpacityAndWhiteTint(__instance);

        if (!restoredAny)
        {
            return true;
        }

        // 发生替换时，容器内部 _particles 仍缓存旧占位节点；直接播放刚恢复的原版粒子。
        RestartRestoredOriginalParticles(__instance);
        return false;
    }

    private static bool IsEnergyCounterFx(NParticlesContainer container)
    {
        if (container.GetParent() is not NEnergyCounter)
            return false;

        string containerName = container.Name.ToString();
        return containerName == "EnergyVfxBack"
            || containerName == "EnergyVfxFront";
    }

    private static bool RestoreOriginalParticleScenes(NParticlesContainer container)
    {
        bool restoredAny = false;
        string containerName = container.Name.ToString();
        if (!OriginalParticleNamesByContainer.TryGetValue(containerName, out string[] particleNames))
        {
            return false;
        }

        foreach (string particleName in particleNames)
        {
            Node existing = container.GetNodeOrNull<Node>(particleName);
            if (existing is GpuParticles2D)
            {
                continue;
            }

            // 空占位节点没有正确类名，需先移除，再以 PCK 中已经导出的原版场景替换。
            if (existing != null)
            {
                container.RemoveChild(existing);
                existing.QueueFree();
            }

            PackedScene originalScene = GD.Load<PackedScene>(OriginalScenePathByParticle[particleName]);
            if (originalScene == null)
            {
                GD.PushError($"[EnergyParticleFix] 原版粒子场景加载失败：{OriginalScenePathByParticle[particleName]}");
                continue;
            }

            Node restored = originalScene.Instantiate();
            if (restored is not GpuParticles2D)
            {
                GD.PushError($"[EnergyParticleFix] 原版粒子场景类型异常：{OriginalScenePathByParticle[particleName]}");
                restored.QueueFree();
                continue;
            }

            restored.Name = particleName;
            container.AddChild(restored);
            restoredAny = true;
            GD.Print($"[EnergyParticleFix] restored original particle scene: {particleName}");
        }

        return restoredAny;
    }

    private static void RestartRestoredOriginalParticles(NParticlesContainer container)
    {
        foreach (Node child in container.GetChildren())
        {
            if (child is GpuParticles2D particles)
            {
                particles.Restart();
                GD.Print($"[EnergyParticleFix] restarted original particle: {container.Name}/{particles.Name}");
            }
        }
    }

    private static void AssignTexturesOpacityAndWhiteTint(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is GpuParticles2D particles)
            {
                string particleName = particles.Name.ToString();

                if (TexturePathByParticle.TryGetValue(particleName, out string texturePath))
                {
                    Texture2D texture = GetTexture(texturePath);

                    if (texture != null)
                    {
                        particles.Texture = texture;
                        ApplyConfiguredOpacity(particles, texturePath);

                        // 只用于最终白色着色；没有任何黑底转透明的代码。
                        particles.Material = GetWhiteTintMaterial();

                        GD.Print(
                            $"[EnergyParticleFix] white tint: {particleName} <- {texture.ResourcePath}"
                        );
                    }
                }
                else
                {
                    GD.PushWarning(
                        $"[EnergyParticleFix] 未配置图片：{particleName}"
                    );
                }
            }

            AssignTexturesOpacityAndWhiteTint(child);
        }
    }

    private static void ApplyConfiguredOpacity(
        GpuParticles2D particles,
        string texturePath)
    {
        if (!OpacityByTexturePath.TryGetValue(texturePath, out float opacity))
            return;

        Color currentColor = particles.Modulate;
        particles.Modulate = new Color(
            currentColor.R,
            currentColor.G,
            currentColor.B,
            opacity
        );
    }

    private static ShaderMaterial GetWhiteTintMaterial()
    {
        if (_whiteTintMaterial != null)
            return _whiteTintMaterial;

        Shader shader = new Shader();
        shader.Code = WhiteTintShaderCode;

        _whiteTintMaterial = new ShaderMaterial();
        _whiteTintMaterial.Shader = shader;

        return _whiteTintMaterial;
    }

    private static Texture2D GetTexture(string texturePath)
    {
        if (TextureCache.TryGetValue(texturePath, out Texture2D cachedTexture))
            return cachedTexture;

        Texture2D texture = GD.Load<Texture2D>(texturePath);

        if (texture == null)
        {
            GD.PushError(
                $"[EnergyParticleFix] 图片加载失败，请检查路径和 PCK：{texturePath}"
            );
            return null;
        }

        TextureCache.Add(texturePath, texture);
        return texture;
    }
}
