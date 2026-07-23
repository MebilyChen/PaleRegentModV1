using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Resources;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空实验】技能牌（机制文档：造物流）。
/// 1 灵魂 + X 虚空：X ≥ 3 → 将 1 张【虚空化形】加入手牌；
/// 否则 → 将 1 张【失败实验】加入手牌。消耗。
/// </summary>
public class VoidExperiment : PaleRegentModV1Card
{
    private const int SuccessThreshold = 3;

    public VoidExperiment() : base(1,
        CardType.Skill, CardRarity.Uncommon,
        TargetType.Self)
    {
        CardTraits.SetVoidCostX(this);
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = 0;
        if (cardPlay.TryGetSecondaryResources(out SecondaryResourcePlayLedger ledger))
        {
            x = ledger.Spent(VoidResource.Id);
        }
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);

        if (x >= SuccessThreshold)
        {
            await CardPileCmd.AddToCombatAndPreview<VoidGivenForm>(Owner.Creature, PileType.Hand, 1, Owner);
        }
        else
        {
            await CardPileCmd.AddToCombatAndPreview<FailedExperiment>(Owner.Creature, PileType.Hand, 1, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
