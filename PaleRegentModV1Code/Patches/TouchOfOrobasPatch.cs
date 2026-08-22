using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using PaleRegentModV1.PaleRegentModV1Code.Relics;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 为原版“欧洛巴斯之触”补充苍白君主的初始遗物升级线：
/// PaleToken（苍白信物） -> FirstLight（第一道光）；
/// Kingsoul（国王之魂） -> VoidHeart（虚空之心）。
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), "GetUpgradedStarterRelic")]
public static class TouchOfOrobasPatch
{
    /// <summary>
    /// 在原版决定升级后的初始遗物后，替换苍白君主对应的升级结果。
    /// </summary>
    /// <param name="__0">
    /// GetUpgradedStarterRelic 的第一个参数，即当前需要升级的初始遗物。
    /// 使用 __0 可以避免依赖原方法的参数名称。
    /// </param>
    /// <param name="__result">原版决定返回的升级遗物。</param>
    [HarmonyPostfix]
    private static void Postfix(RelicModel __0, ref RelicModel __result)
    {
        // Kingsoul 继承自 PaleToken，必须优先判断，避免落入 PaleToken -> FirstLight 分支。
        if (__0 is Kingsoul)
        {
            RelicModel voidHeart = ModelDb.Relic<VoidHeart>();

            if (voidHeart == null)
            {
                Log.Error(
                    "[PaleRegentModV1] VoidHeart 尚未注册，" +
                    "无法完成欧洛巴斯之触升级。"
                );
                return;
            }

            __result = voidHeart;

            Log.Info(
                "[PaleRegentModV1] 欧洛巴斯之触：" +
                "Kingsoul -> VoidHeart"
            );
            return;
        }

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