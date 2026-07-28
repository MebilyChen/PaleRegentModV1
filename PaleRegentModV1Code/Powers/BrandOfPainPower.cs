using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【痛痕烙印】buff（效果表 P#24，卡牌 C#66 痛痕烙印 施加）。
/// 效果：你每次造成伤害后，对受到伤害的敌人施加 [层数] 层【苦痛之路】。
///
/// 实现说明：
/// - 挂 AfterDamageGiven：持有者造成的任意伤害（攻击/Power触发）均计一次；
/// - 只对敌方目标施加，避免对自己或队友造成伤害时反向挂 debuff；
/// - _resolving 递归保护：苦痛之路结算或本效果连带的伤害不再二次触发。
/// </summary>
public class BrandOfPainPower : PaleRegentModV1Power
{
    /// <summary>递归保护。</summary>
    private bool _resolving;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || _resolving || target == Owner || !target.IsAlive)
        {
            return;
        }

        // 只对敌方施加苦痛之路
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null || combatState.PlayerCreatures.Contains(target))
        {
            return;
        }

        _resolving = true;
        try
        {
            Flash();
            await PowerCmd.Apply<PathOfPainPower>(choiceContext, target, Amount, Owner, null);
        }
        finally
        {
            _resolving = false;
        }
    }
}
