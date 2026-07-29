using BaseLib.Abstracts;
using PaleRegentModV1.PaleRegentModV1Code.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace PaleRegentModV1.PaleRegentModV1Code.Character;

/// <summary>
/// 苍白之王专属遗物池。
///
/// 【为什么要重写 GenerateAllRelics？】
/// 我们的自定义遗物（苍白信物/国王之魂/王后信物）通过基类上的
/// [Pool(typeof(PaleRegentModV1RelicPool))] 特性注册进这个池——这部分由 BaseLib
/// 在 AllRelics 取值时自动合并（ModHelper.ConcatModelsFromMods），不需要在这里写。
///
/// 但 BaseLib 默认的 GenerateAllRelics() 返回空数组：如果不重写，这个池里就
/// "只有"我们的3个遗物，跑一局几乎捞不到遗物。所以这里把原版君主（Regent）
/// 遗物池的全部内容合并进来，保证掉落丰富度。
///
/// 【想增删原版遗物怎么办？】
/// 把下面的返回值换成手动列表即可，例如：
///   return [ModelDb.Relic&lt;某遗物&gt;(), ModelDb.Relic&lt;另一个&gt;(), ...];
/// 或在返回前用 LINQ 过滤：.Where(r => r is not 某遗物)
/// </summary>
public class PaleRegentModV1RelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => PaleRegentModV1.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/paleregent_energy_icon.png".ImagePath();

    //protected override IEnumerable<RelicModel> GenerateAllRelics()
    //{
        // 借用原版 Regent 遗物池的完整内容（AllRelics 有缓存，取一次很便宜）。
        // 注意：不要在这里手动加我们自己的遗物，[Pool] 特性已自动注入，重复加会双倍出现。
        //return ModelDb.RelicPool<RegentRelicPool>().AllRelics;
    //}
}