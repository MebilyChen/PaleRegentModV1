using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【白沃姆摇篮】能力牌（表 C#97，0727 新增）。
/// 4 灵魂：获得能力【白沃姆摇篮】——你的【蓄灵】不再于触发后消失；
/// 每回合开始获得 1 层【蓄灵】和 2 层【白根】；
/// 受到未被格挡完的攻击伤害时，移除所有【蓄灵】。
/// 升级后：每回合 3 层白根。
/// </summary>
public class WhiteWyrmCradleCard() : PaleRegentModV1Card(4,
    CardType.Power, CardRarity.Ancient,
    TargetType.Self)
{
    private const int BaseWhiteRoot = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WhiteWyrmCradlePower>(BaseWhiteRoot)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WhiteWyrmCradlePower>(choiceContext, Owner.Creature,
            DynamicVars["WhiteWyrmCradlePower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WhiteWyrmCradlePower"].UpgradeValueBy(1);
    }
}
