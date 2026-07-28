using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using PaleRegentModV1.PaleRegentModV1Code.Patches;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空回声】技能牌（表 C#64，0727 新增）。
/// 3 灵魂：生成等同于本场战斗中已生成过的虚空总量的虚空。消耗。
/// 升级后：2 灵魂（表格升级列未明示，按惯例降费处理，已在此备注，如需调整告知）。
/// </summary>
public class VoidEcho() : PaleRegentModV1Card(3,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VoidPower>((int?)null)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 注意：先读计数再 Gain，否则本次获得会把总量翻倍
        int amount = CombatCounters.VoidGainedThisCombat;
        if (amount <= 0)
        {
            return;
        }

        await VoidResource.Gain(cardPlay.Player, amount);
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
