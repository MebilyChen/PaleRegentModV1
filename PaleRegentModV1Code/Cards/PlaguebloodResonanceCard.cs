using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【瘟血共鸣】能力牌（表 C#67，0727 新增）。
/// 3 灵魂 + 1 虚空：每当你造成伤害，对自身施加 1 层【瘟疫】。
/// 升级后：2 灵魂 + 1 虚空。
/// 备注：瘟疫已按 0727 要求改为正面效果（Buff）。
/// </summary>
public class PlaguebloodResonanceCard : PaleRegentModV1Card
{
    private const int VoidCost = 1;
    private const int PlaguePerHit = 1;

    public PlaguebloodResonanceCard() : base(3,
        CardType.Power, CardRarity.Rare,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PlaguePower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PlaguebloodResonancePower>(PlaguePerHit)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PlaguebloodResonancePower>(choiceContext, Owner.Creature,
            DynamicVars["PlaguebloodResonancePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
