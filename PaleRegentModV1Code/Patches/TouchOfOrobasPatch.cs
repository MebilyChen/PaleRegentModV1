using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using PaleRegentModV1.PaleRegentModV1Code.Relics;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 为原版“欧洛巴斯之触”添加：
/// 苍白信物 PaleToken -> 第一道光 FirstLight
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), "GetUpgradedStarterRelic")]
public static class TouchOfOrobasPatch
{
    /// <param name="__0">
    /// GetUpgradedStarterRelic 的第一个参数，即当前要升级的初始遗物。
    /// 使用 __0 可以避免依赖原方法的参数名称。
    /// </param>
    /// <param name="__result">
    /// 原版决定返回的升级遗物。
    /// </param>
    [HarmonyPostfix]
    private static void Postfix(
        RelicModel __0,
        ref RelicModel __result)
    {
        if (__0 is not PaleToken)
        {
            return;
        }

        RelicModel firstLight = ModelDb.Relic<FirstLight>();

        if (firstLight == null)
        {
            Log.Error(
                "[PaleRegentModV1] FirstLight 尚未注册，" +
                "无法完成欧洛巴斯之触升级。"
            );
            return;
        }

        __result = firstLight;

        Log.Info(
            "[PaleRegentModV1] 欧洛巴斯之触：" +
            "PaleToken -> FirstLight"
        );
    }
}