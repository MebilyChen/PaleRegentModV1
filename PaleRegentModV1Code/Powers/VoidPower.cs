using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using HarmonyLib;
using Godot;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

public class VoidPower : PaleRegentModV1Power
{
    public const string PowerId = "Void";
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public VoidPower(int amount)
    {
        Amount = amount;
    }

    public override void AtStartOfTurn()
    {
        // 虚空机制：回合开始时，扣除相应灵魂。
        // 例如，灵魂上限为4，上一回合剩余2虚空，下一回合开始灵魂只能恢复到2。
        // 因为玩家在回合开始时已经恢复了基础能量，我们需要在这里扣除。
        if (Owner is Player player)
        {
            int voidAmount = Amount;
            if (voidAmount > 0)
            {
                // 扣除能量
                player.Energy -= voidAmount;
                if (player.Energy < 0)
                {
                    player.Energy = 0;
                }
                
                // 可选：播放一些特效或提示
                // CombatManager.Instance.AddAction(new FloatTextAction(player, $"失去 {voidAmount} 灵魂", Color.Color8(100, 100, 255)));
            }
        }
    }
}
