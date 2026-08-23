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
            //["vfx_common_ring_polar_b"] =
              //  "res://PaleRegentModV1/images/charui/common_ring_polar_b.png",

              //["vfx_starry_impact_small_stars"] =
              // "res://PaleRegentModV1/scenes/vfx/energy/common_glow_transparent.png",

            ["vfx_starry_impact_constellation_small_a"] =
                "res://PaleRegentModV1/images/charui/paleregent_orb_layer_5.png",

            //["vfx_starry_impact_constellation_small_b"] =
            //   "res://PaleRegentModV1/scenes/vfx/energy/common_glow_transparent.png",
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

    // 缓存“是否为明显黑底图”的检测结果，避免每次 Restart 都重新扫描像素。
    private static readonly Dictionary<string, bool> BlackBackgroundCache =
        new Dictionary<string, bool>(StringComparer.Ordinal);

    // 普通透明图：保留原图 Alpha，只把最终色调转成带明暗层次的白色。
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

    // 黑底图：把像素亮度转换成 Alpha。
    // 这样纯黑背景会变透明，越亮的内容越不透明；最终 RGB 仍然输出白色。
    private const string BlackBackgroundWhiteTintShaderCode = @"
shader_type canvas_item;
render_mode blend_mix;

uniform float white_gain : hint_range(0.1, 3.0, 0.01) = 1.60;

void fragment()
{
    vec4 texture_color = texture(TEXTURE, UV);

    // 使用最大 RGB 分量比单纯 luminance 更适合带颜色的发光素材：
    // 纯黑 -> 0；任一通道较亮 -> 保留较高 Alpha。
    float extracted_alpha = max(
        texture_color.r,
        max(texture_color.g, texture_color.b)
    );

    // 保留原图自身 Alpha，兼容本来就带半透明边缘的素材。
    extracted_alpha *= texture_color.a;

    float white_value = clamp(
        pow(extracted_alpha, 0.55) * white_gain,
        0.0,
        1.0
    );

    // 粒子的 COLOR.a 仍负责整体淡入淡出。
    COLOR = vec4(
        vec3(white_value),
        extracted_alpha * COLOR.a
    );
}
";

    private static ShaderMaterial _whiteTintMaterial;
    private static ShaderMaterial _blackBackgroundWhiteTintMaterial;

    [HarmonyPrefix]
    public static void Prefix(NParticlesContainer __instance)
    {
        if (!IsEnergyCounterFx(__instance))
            return;

        AssignTexturesOpacityAndWhiteTint(__instance);
    }

    private static bool IsEnergyCounterFx(NParticlesContainer container)
    {
        if (container.GetParent() is not NEnergyCounter)
            return false;

        string containerName = container.Name.ToString();
        return containerName == "EnergyVfxBack"
            || containerName == "EnergyVfxFront";
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

                        bool hasBlackBackground = HasBlackBackground(texture, texturePath);
                        particles.Material = hasBlackBackground
                            ? GetBlackBackgroundWhiteTintMaterial()
                            : GetWhiteTintMaterial();

                        GD.Print(
                            $"[EnergyParticleFix][APPLY] particle={particleName}, " +
                            $"texture={texture.ResourcePath}, " +
                            $"size={texture.GetWidth()}x{texture.GetHeight()}, " +
                            $"blackBg={hasBlackBackground}, " +
                            $"shader={(hasBlackBackground ? "BlackBackgroundWhiteTintShader" : "WhiteTintShader")}, " +
                            $"materialNull={particles.Material == null}"
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

    private static ShaderMaterial GetBlackBackgroundWhiteTintMaterial()
    {
        if (_blackBackgroundWhiteTintMaterial != null)
            return _blackBackgroundWhiteTintMaterial;

        Shader shader = new Shader();
        shader.Code = BlackBackgroundWhiteTintShaderCode;

        _blackBackgroundWhiteTintMaterial = new ShaderMaterial();
        _blackBackgroundWhiteTintMaterial.Shader = shader;

        return _blackBackgroundWhiteTintMaterial;
    }

    private static bool HasBlackBackground(Texture2D texture, string texturePath)
    {
        if (BlackBackgroundCache.TryGetValue(texturePath, out bool cached))
        {
            GD.Print(
                $"[EnergyParticleFix][BLACK_BG_CACHE] texture={texturePath}, result={cached}"
            );
            return cached;
        }

        Image image = texture.GetImage();
        if (image == null || image.IsEmpty())
        {
            GD.PushWarning(
                $"[EnergyParticleFix][BLACK_BG] 无法取得 Image：texture={texturePath}"
            );
            BlackBackgroundCache[texturePath] = false;
            return false;
        }

        int width = image.GetWidth();
        int height = image.GetHeight();
        if (width <= 0 || height <= 0)
        {
            GD.PushWarning(
                $"[EnergyParticleFix][BLACK_BG] 图片尺寸异常：texture={texturePath}, size={width}x{height}"
            );
            BlackBackgroundCache[texturePath] = false;
            return false;
        }

        const float blackThreshold = 0.04f;
        const float opaqueThreshold = 0.90f;
        const float requiredRatio = 0.08f;

        int stepX = Math.Max(1, width / 64);
        int stepY = Math.Max(1, height / 64);

        int sampled = 0;
        int opaqueBlack = 0;
        int transparent = 0;
        int nearOpaque = 0;

        float minR = 1f;
        float minG = 1f;
        float minB = 1f;
        float minA = 1f;
        float maxR = 0f;
        float maxG = 0f;
        float maxB = 0f;
        float maxA = 0f;

        for (int y = 0; y < height; y += stepY)
        {
            for (int x = 0; x < width; x += stepX)
            {
                Color pixel = image.GetPixel(x, y);
                sampled++;

                minR = Math.Min(minR, pixel.R);
                minG = Math.Min(minG, pixel.G);
                minB = Math.Min(minB, pixel.B);
                minA = Math.Min(minA, pixel.A);

                maxR = Math.Max(maxR, pixel.R);
                maxG = Math.Max(maxG, pixel.G);
                maxB = Math.Max(maxB, pixel.B);
                maxA = Math.Max(maxA, pixel.A);

                if (pixel.A <= 0.05f)
                    transparent++;

                if (pixel.A >= opaqueThreshold)
                    nearOpaque++;

                if (pixel.A >= opaqueThreshold
                    && pixel.R <= blackThreshold
                    && pixel.G <= blackThreshold
                    && pixel.B <= blackThreshold)
                {
                    opaqueBlack++;
                }
            }
        }

        float blackRatio = sampled > 0 ? (float)opaqueBlack / sampled : 0f;
        float transparentRatio = sampled > 0 ? (float)transparent / sampled : 0f;
        float opaqueRatio = sampled > 0 ? (float)nearOpaque / sampled : 0f;

        bool hasBlackBackground =
            sampled > 0 && blackRatio >= requiredRatio;

        BlackBackgroundCache[texturePath] = hasBlackBackground;

        Color topLeft = image.GetPixel(0, 0);
        Color topRight = image.GetPixel(width - 1, 0);
        Color bottomLeft = image.GetPixel(0, height - 1);
        Color bottomRight = image.GetPixel(width - 1, height - 1);
        Color center = image.GetPixel(width / 2, height / 2);

        GD.Print(
            $"[EnergyParticleFix][BLACK_BG] texture={texturePath}, " +
            $"size={width}x{height}, step={stepX}x{stepY}, sampled={sampled}, " +
            $"opaqueBlack={opaqueBlack}, blackRatio={blackRatio:P2}, " +
            $"transparent={transparent}, transparentRatio={transparentRatio:P2}, " +
            $"nearOpaque={nearOpaque}, opaqueRatio={opaqueRatio:P2}, " +
            $"thresholdRGB<={blackThreshold:F3}, alpha>={opaqueThreshold:F2}, requiredRatio={requiredRatio:P0}, " +
            $"result={hasBlackBackground}"
        );

        GD.Print(
            $"[EnergyParticleFix][RANGE] texture={texturePath}, " +
            $"R={minR:F3}..{maxR:F3}, G={minG:F3}..{maxG:F3}, " +
            $"B={minB:F3}..{maxB:F3}, A={minA:F3}..{maxA:F3}"
        );

        GD.Print(
            $"[EnergyParticleFix][PIXELS] texture={texturePath}, " +
            $"TL={FormatColor(topLeft)}, TR={FormatColor(topRight)}, " +
            $"BL={FormatColor(bottomLeft)}, BR={FormatColor(bottomRight)}, " +
            $"CENTER={FormatColor(center)}"
        );

        return hasBlackBackground;
    }

    private static string FormatColor(Color color)
    {
        return $"({color.R:F3},{color.G:F3},{color.B:F3},{color.A:F3})";
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
