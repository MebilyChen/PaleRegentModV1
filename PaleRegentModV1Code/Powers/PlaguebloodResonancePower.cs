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
/// - 挂 AfterDamageGiven；瘟疫本身的随机段伤会回流到这里。
///   PlaguePower 在结算该段伤期间暴露 IsResolvingExtraDamage 标记；命中该
///   标记时直接返回，确保瘟疫效果触发的攻击不计入本能力，也不再叠加瘟疫。
/// - _resolving 仅作为本能力自身的同步重入保护。
/// </summary>
public class PlaguebloodResonancePower : PaleRegentModV1Power
{
    /// <summary>递归保护。</summary>
    private bool _resolving;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // 不把瘟疫段伤标为 Unpowered：它仍须享受力量与瘟疫的伤害加成。
        // 改以当前 PlaguePower 的结算标记准确识别其伤害来源。
        PlaguePower? plague = Owner.GetPower<PlaguePower>();
        if (dealer != Owner ||
            _resolving ||
            target == Owner ||
            plague?.IsResolvingExtraDamage == true)
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