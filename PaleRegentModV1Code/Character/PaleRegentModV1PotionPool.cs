using BaseLib.Abstracts;
using PaleRegentModV1.PaleRegentModV1Code.Extensions;
using Godot;

namespace PaleRegentModV1.PaleRegentModV1Code.Character;

public class PaleRegentModV1PotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => PaleRegentModV1.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/paleregent_energy_icon.png".ImagePath();
}