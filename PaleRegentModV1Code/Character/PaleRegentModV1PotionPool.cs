using BaseLib.Abstracts;
using PaleRegentModV1.PaleRegentModV1Code.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace PaleRegentModV1.PaleRegentModV1Code.Character;

/// <summary>
/// 苍白之王专属药水池。
/// 自定义药水（如虚空精华）靠基类的 [Pool(typeof(PaleRegentModV1PotionPool))] 自动注入，
/// 这里只需要把原版 Regent 的药水合并进来（理由同遗物池，见 PaleRegentModV1RelicPool 注释）。
/// </summary>
public class PaleRegentModV1PotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => PaleRegentModV1.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/paleregent_energy_icon.png".ImagePath();

    protected override IEnumerable<PotionModel> GenerateAllPotions()
    {
        // 合并原版 Regent 药水池全部内容；自制药水由 [Pool] 特性自动注入，不要重复加。
        return ModelDb.PotionPool<RegentPotionPool>().AllPotions;
    }
}