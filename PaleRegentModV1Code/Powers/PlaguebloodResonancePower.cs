using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【疫血共鸣】buff（效果表 P#25，卡牌 C#67 疫血共鸣 施加）。
/// 效果：你每次造成伤害后，对自身施加 [层数] 层【瘟疫】。
/// （瘟疫已按 20260727 效果表改为正面效果：+力量并追加随机段伤，
/// 回合结束消失，所以本能力等于"打人就滚雪球"。）
///
/// 实现说明：
/// - 挂 AfterDamageGiven；瘟疫本身的随机段伤是 PlaguePower 触发的伤害，
///   同样会回流到这里，用 _resolving 防止无限递归（段伤只叠一次瘟疫的话
///   会指数爆炸，这里统一：瘟疫段伤不再叠加瘟疫）。
/// </summary>
public class PlaguebloodResonancePower : PaleRegentModV1Power
{
    /// <summary>递归保护。</summary>
    private bool _resolving;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || _resolving || target == Owner)
        {
            return;
        }

        _resolving = true;
        try
        {
            Flash();
            await PowerCmd.Apply<PlaguePower>(choiceContext, Owner, Amount, Owner, null);
        }
        finally
        {
            _resolving = false;
        }
    }
}
