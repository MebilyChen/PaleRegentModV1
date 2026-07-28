using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空崇拜】技能牌（表 C#69，0727 新增）。
/// 2 灵魂：获得 2 点虚空。
/// 升级后：获得 3 点虚空。
/// </summary>
public class VoidWorship() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    /// <summary>获得的虚空数量（升级后 3）。</summary>
    private int _voidGain = 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VoidPower>((int?)null)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await VoidResource.Gain(cardPlay.Player, _voidGain);
        await VoidResource.SyncPower(choiceContext, cardPlay.Player, this);
    }

    protected override void OnUpgrade()
    {
        _voidGain = 3;
    }
}
