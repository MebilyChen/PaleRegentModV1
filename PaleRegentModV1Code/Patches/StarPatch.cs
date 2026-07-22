using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

// 使用别名，避免项目名、命名空间和角色类都叫 PaleRegentModV1 时产生歧义。
using PaleRegentCharacter =
    global::PaleRegentModV1.PaleRegentModV1Code.Character.PaleRegentModV1;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 保留原版 Regent 的能量与星辉计数器场景，
/// 仅在当前玩家是 Pale Regent 时替换其中的纹理。
/// </summary>
[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
internal static class PaleRegentCounterTexturePatch
{
    private static readonly FieldInfo? EnergyCounterField =
        AccessTools.Field(typeof(NCombatUi), "_energyCounter");

    private static readonly FieldInfo? StarCounterField =
        AccessTools.Field(typeof(NCombatUi), "_starCounter");

    private static readonly FieldInfo? PlayerField =
        AccessTools.Field(typeof(NEnergyCounter), "_player");

    /*
     * 注意结尾必须有斜杠。
     *
     * 对应项目目录：
     * PaleRegentModV1/images/charui/
     */
    private const string TextureRoot =
        "res://PaleRegentModV1/images/charui/";

    /*
     * 左边：
     * 原版 Regent 能量计数器或星辉计数器使用的 PNG 文件名。
     *
     * 右边：
     * 你的 Mod 中用于替换的 PNG 路径。
     *
     * 这里按你当前提供的三个文件名填写。
     * 后续发现其他需要替换的能量贴图，也继续添加到这个字典即可。
     */
    private static readonly Dictionary<string, string> TextureReplacements =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["energy_star.png"] =
                TextureRoot + "energy_star.png",

            ["energy_star_layer_2.png"] =
                TextureRoot + "energy_star_layer_2.png",

            ["energy_star_layer_3.png"] =
                TextureRoot + "energy_star_layer_3.png",
            
            ["regent_orb_layer_1.png"] =
                    TextureRoot + "paleregent_orb_layer_1.png",

            ["regent_orb_layer_2.png"] =
                TextureRoot + "paleregent_orb_layer_2.png",

            ["regent_orb_layer_3.png"] =
                TextureRoot + "paleregent_orb_layer_3.png",

            ["regent_orb_layer_5.png"] =
                TextureRoot + "paleregent_orb_layer_5.png"
        };

    /*
     * 已经加载过的纹理放进缓存。
     * 避免每次进入战斗时重复从 PCK 加载同一张图片。
     */
    private static readonly Dictionary<string, Texture2D> TextureCache =
        new(StringComparer.OrdinalIgnoreCase);

    [HarmonyPostfix]
    private static void Postfix(NCombatUi __instance)
    {
        /*
         * 先取得已经由游戏创建完成的能量计数器。
         */
        if (EnergyCounterField?.GetValue(__instance)
            is not NEnergyCounter energyCounter)
        {
            return;
        }

        /*
         * 从能量计数器中取得其所属玩家。
         */
        if (PlayerField?.GetValue(energyCounter)
            is not Player player)
        {
            return;
        }

        /*
         * 只有当前玩家是你的 Pale Regent 角色时才替换。
         * 原版 Regent 和其他角色不会受到影响。
         */
        if (player.Character is not PaleRegentCharacter)
        {
            return;
        }

        /*
         * 在原版能量计数器节点树中替换匹配的纹理。
         * 节点、AnimationPlayer、材质、Shader 和粒子全部保留。
         */
        ReplaceTexturesRecursively(energyCounter);

        /*
         * 星辉计数器可能并非所有角色都会创建，
         * 所以这里找不到时直接跳过，不让整个战斗报错。
         */
        if (StarCounterField?.GetValue(__instance)
            is NStarCounter starCounter)
        {
            ReplaceTexturesRecursively(starCounter);
        }
    }

    /// <summary>
    /// 递归检查计数器场景中的全部子节点。
    /// </summary>
    private static void ReplaceTexturesRecursively(Node node)
    {
        switch (node)
        {
            case TextureRect textureRect:
                textureRect.Texture =
                    GetReplacement(textureRect.Texture);
                break;

            case Sprite2D sprite:
                sprite.Texture =
                    GetReplacement(sprite.Texture);
                break;

            case NinePatchRect ninePatch:
                ninePatch.Texture =
                    GetReplacement(ninePatch.Texture);
                break;
        }

        foreach (Node child in node.GetChildren())
        {
            ReplaceTexturesRecursively(child);
        }
    }

    /// <summary>
    /// 根据原纹理的文件名查找白王替换纹理。
    /// 没有匹配项时保持原纹理不变。
    /// </summary>
    private static Texture2D? GetReplacement(Texture2D? original)
    {
        if (original is null)
        {
            return null;
        }

        string resourcePath = original.ResourcePath;

        /*
         * 某些运行时生成的纹理可能没有 ResourcePath。
         * 这种纹理不能按文件名替换，直接保留。
         */
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return original;
        }

        string filename = Path.GetFileName(resourcePath);

        if (!TextureReplacements.TryGetValue(
                filename,
                out string? replacementPath))
        {
            return original;
        }

        /*
         * 已经加载过则直接使用缓存。
         */
        if (TextureCache.TryGetValue(
                replacementPath,
                out Texture2D? cachedTexture))
        {
            return cachedTexture;
        }

        /*
         * 先检查资源是否真的被打进了 PCK。
         */
        if (!ResourceLoader.Exists(replacementPath))
        {
            GD.PushError(
                $"[PaleRegent] Replacement texture does not exist: " +
                replacementPath);

            return original;
        }

        Texture2D? replacement =
            GD.Load<Texture2D>(replacementPath);

        if (replacement is null)
        {
            GD.PushError(
                $"[PaleRegent] Failed to load replacement texture: " +
                replacementPath);

            return original;
        }

        TextureCache[replacementPath] = replacement;

        return replacement;
    }
}
