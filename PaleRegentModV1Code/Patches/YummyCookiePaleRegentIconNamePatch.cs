using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using PaleRegentModV1.PaleRegentModV1Code.Extensions;

// 用别名避免与项目、命名空间同名的角色类产生歧义。
using PaleRegentCharacter =
    global::PaleRegentModV1.PaleRegentModV1Code.Character.PaleRegentModV1;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 仅当原版美味饼干由 PaleRegent 持有时，改用本模组的 PNG 图标。
/// 不创建新遗物，也不影响原版 Regent 或其他角色。
/// </summary>
[HarmonyPatch]
internal static class YummyCookiePaleRegentIconPatch
{
    private const string IconName = "yummy_cookie_paleregent.png";
    private const string OutlineName = "yummy_cookie_paleregent_outline.png";

    /// <summary>
    /// 原版小图、描边和大图都定义在 RelicModel，
    /// 所以在这里一次性补丁三个属性 getter。
    /// </summary>
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.PropertyGetter(
            typeof(RelicModel),
            nameof(RelicModel.PackedIconPath));

        // 这是 protected 属性，不能直接 nameof，按属性名取得 getter。
        yield return AccessTools.PropertyGetter(
            typeof(RelicModel),
            "PackedIconOutlinePath");

        yield return AccessTools.PropertyGetter(
            typeof(RelicModel),
            "BigIconPath");
    }

    [HarmonyPostfix]
    private static void Postfix(
        MethodBase __originalMethod,
        RelicModel __instance,
        ref string __result)
    {
        // 只处理原版 YummyCookie 的可变实例；不要读取 canonical 实例的 Owner。
        if (__instance is not YummyCookie || __instance.IsCanonical)
        {
            return;
        }

        // 只有 PaleRegent 持有时才替换图片。
        if (__instance.Owner?.Character is not PaleRegentCharacter)
        {
            return;
        }

        __result = __originalMethod.Name switch
        {
            "get_PackedIconPath" => IconName.RelicImagePath(),
            "get_PackedIconOutlinePath" => OutlineName.RelicImagePath(),
            "get_BigIconPath" => IconName.BigRelicImagePath(),
            _ => __result
        };
    }
}
