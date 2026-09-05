using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 强制保护原版 Regent 的 Combat / RestSite Spine 资源。
///
/// 原理：
/// 1. 不使用 Godot 已缓存的 Regent PackedScene。
/// 2. 使用 CacheMode.IgnoreDeep 从原版路径重新加载一份完全独立的场景。
/// 3. Combat 直接把 CharacterModel.CreateVisuals 的最终结果替换掉。
/// 4. RestSite 则把所有 SpineSprite 的 skeleton_data_res
///    从一份 fresh vanilla scene 中重新绑定回来。
///
/// 不碰 PaleRegent 自己，也不碰 Merchant。
/// </summary>
public static class VanillaRegentVisualHardFix
{
    private const string RegentCombatScene =
        "res://scenes/creature_visuals/regent.tscn";

    private const string RegentRestScene =
        "res://scenes/rest_site/characters/regent_rest_site.tscn";


    // ============================================================
    //  Combat
    // ============================================================

    [HarmonyPatch(
        typeof(CharacterModel),
        nameof(CharacterModel.CreateVisuals))]
    private static class RegentCombatCreateVisualsPatch
    {
        /// <summary>
        /// Priority.Last：
        /// 尽量在其他 CreateVisuals postfix 都跑完以后，
        /// 最后重新覆盖结果。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            CharacterModel __instance,
            ref NCreatureVisuals __result)
        {
            if (__instance is not Regent)
                return;

            try
            {
                NCreatureVisuals fresh =
                    LoadFreshScene<NCreatureVisuals>(
                        RegentCombatScene
                    );

                if (fresh == null)
                {
                    GD.PushWarning(
                        "[VanillaRegentHardFix] " +
                        "Failed to load fresh Regent combat scene."
                    );
                    return;
                }

                NCreatureVisuals old = __result;

                __result = fresh;

                GD.Print(
                    "[VanillaRegentHardFix][COMBAT] " +
                    $"FORCED fresh vanilla scene. " +
                    $"scene='{fresh.SceneFilePath}'"
                );

                // CreateVisuals 此时正常情况下还没 AddChild。
                // 旧实例已经不会被使用，可以释放。
                if (old != null &&
                    old != fresh &&
                    GodotObject.IsInstanceValid(old) &&
                    !old.IsInsideTree())
                {
                    old.Free();
                }
            }
            catch (Exception e)
            {
                GD.PushError(
                    "[VanillaRegentHardFix][COMBAT] " +
                    $"Exception: {e}"
                );
            }
        }
    }


    // ============================================================
    //  Rest Site
    // ============================================================

    [HarmonyPatch(
        typeof(NRestSiteCharacter),
        nameof(NRestSiteCharacter._Ready))]
    private static class RegentRestSiteReadyPatch
    {
        /*
         * 在 NRestSiteCharacter 自己初始化之前先恢复一次。
         *
         * 注意这里不根据 Node.Name 判断。
         * 只认真正的 vanilla SceneFilePath。
         */
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            NRestSiteCharacter __instance)
        {
            if (!IsVanillaScene(
                    __instance,
                    RegentRestScene))
            {
                return;
            }

            ForceFreshSpineBindings(
                __instance,
                RegentRestScene,
                "REST/PREFIX"
            );
        }


        /*
         * _Ready 执行完再恢复一次。
         *
         * 防止游戏本身或其他 Harmony patch
         * 在 Ready 中重新碰 SkeletonDataResource。
         */
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            NRestSiteCharacter __instance)
        {
            if (!IsVanillaScene(
                    __instance,
                    RegentRestScene))
            {
                return;
            }

            ForceFreshSpineBindings(
                __instance,
                RegentRestScene,
                "REST/POSTFIX"
            );

            /*
             * 再延迟到下一帧做最后一次。
             *
             * 这是专门防某些 mod 在 _Ready 结束以后
             * CallDeferred 修改 Spine 的情况。
             */
            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(__instance))
                    return;

                ForceFreshSpineBindings(
                    __instance,
                    RegentRestScene,
                    "REST/DEFERRED"
                );
            }).CallDeferred();
        }
    }


    // ============================================================
    //  Helpers
    // ============================================================

    private static T LoadFreshScene<T>(
        string scenePath)
        where T : Node
    {
        /*
         * IgnoreDeep 是重点。
         *
         * 不只是 PackedScene 本身不走 cache，
         * 它的 ext_resource dependency 也不走 cache。
         *
         * 对 Spine 来说意味着：
         *
         *   SpineSkeletonDataResource
         *   SpineSkeletonFileResource
         *   SpineAtlasResource
         *   texture dependency
         *
         * 尽可能拿到全新的一套。
         */
        Resource resource =
            ResourceLoader.Load(
                scenePath,
                "PackedScene",
                ResourceLoader.CacheMode.IgnoreDeep
            );

        if (resource is not PackedScene packed)
        {
            GD.PushError(
                "[VanillaRegentHardFix] " +
                $"Not a PackedScene: '{scenePath}'"
            );

            return null;
        }

        Node node = packed.Instantiate();

        if (node is T typed)
            return typed;

        GD.PushError(
            "[VanillaRegentHardFix] " +
            $"Unexpected root type for '{scenePath}'. " +
            $"Expected={typeof(T).FullName}, " +
            $"Actual={node.GetType().FullName}"
        );

        node.Free();

        return null;
    }


    /// <summary>
    /// 从完全 fresh 的 vanilla scene 中，
    /// 按相同 NodePath 找到所有 SpineSprite，
    /// 把 skeleton_data_res 复制给当前实际场景。
    ///
    /// Combat 中我们直接替换整个 root，所以主要给 Rest 用。
    /// </summary>
    private static void ForceFreshSpineBindings(
        Node targetRoot,
        string vanillaScenePath,
        string tag)
    {
        Node freshRoot = null;

        try
        {
            freshRoot =
                LoadFreshScene<Node>(
                    vanillaScenePath
                );

            if (freshRoot == null)
                return;

            int changed =
                CopySpineBindingsRecursive(
                    freshRoot,
                    freshRoot,
                    targetRoot
                );

            GD.Print(
                $"[VanillaRegentHardFix][{tag}] " +
                $"fresh spine bindings applied: {changed}"
            );
        }
        catch (Exception e)
        {
            GD.PushError(
                $"[VanillaRegentHardFix][{tag}] " +
                $"Exception: {e}"
            );
        }
        finally
        {
            if (freshRoot != null &&
                GodotObject.IsInstanceValid(freshRoot))
            {
                /*
                 * skeleton_data_res 是 Resource，
                 * target 已经持有引用。
                 * fresh Node tree 可以安全释放。
                 */
                freshRoot.Free();
            }
        }
    }


    private static int CopySpineBindingsRecursive(
        Node freshRoot,
        Node freshNode,
        Node targetRoot)
    {
        int changed = 0;

        if (freshNode.GetClass() == "SpineSprite")
        {
            NodePath relativePath =
                freshRoot.GetPathTo(freshNode);

            Node targetNode =
                targetRoot.GetNodeOrNull<Node>(
                    relativePath
                );

            if (targetNode != null &&
                targetNode.GetClass() == "SpineSprite")
            {
                /*
                 * 不需要引用 Spine Godot 的 C# 类型。
                 * 直接通过 Godot property 设置，
                 * 避免版本 / assembly binding 问题。
                 */
                Variant freshSkeletonData =
                    freshNode.Get(
                        "skeleton_data_res"
                    );

                targetNode.Set(
                    "skeleton_data_res",
                    freshSkeletonData
                );

                changed++;

                GD.Print(
                    "[VanillaRegentHardFix] " +
                    $"Spine rebound: '{relativePath}'"
                );
            }
        }

        foreach (Node child in freshNode.GetChildren())
        {
            changed +=
                CopySpineBindingsRecursive(
                    freshRoot,
                    child,
                    targetRoot
                );
        }

        return changed;
    }


    private static bool IsVanillaScene(
        Node node,
        string expectedPath)
    {
        if (node == null)
            return false;

        string actual =
            node.SceneFilePath?
                .Replace('\\', '/');

        return string.Equals(
            actual,
            expectedPath,
            StringComparison.OrdinalIgnoreCase
        );
    }
}
