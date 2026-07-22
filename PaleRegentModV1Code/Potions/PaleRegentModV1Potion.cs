using BaseLib.Abstracts;
using BaseLib.Utils;
using PaleRegentModV1.PaleRegentModV1Code.Character;

namespace PaleRegentModV1.PaleRegentModV1Code.Potions;

[Pool(typeof(PaleRegentModV1PotionPool))]
public abstract class PaleRegentModV1Potion : CustomPotionModel;