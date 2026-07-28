using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【祸中取福】技能牌（表 C#59，0727 新增）。
/// 2 灵魂：获得 7 点格挡，战斗中你的牌堆里每有 1 张诅咒牌，恢复 1 点生命。
/// 升级后：10 点格挡，每张诅咒恢复 2 点生命。
/// 备注：诅咒牌统计范围 = 本场战斗的抽牌堆 + 手牌 + 弃牌堆 + 消耗堆（全牌堆）。
/// </summary>
public class BlessingInBlight() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseBlock = 7;
    private const int UpgradeBlockBonus = 3;

    /// <summary>每张诅咒牌恢复的生命（升级后 2）。</summary>
    private int _healPerCurse = 1;

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 统计全战斗牌堆中的诅咒牌数量
        int curseCount = CardPile
            .GetCards(Owner, PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust)
            .Count(c => c.Type == CardType.Curse);

        if (curseCount > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, curseCount * _healPerCurse);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
        _healPerCurse = 2;
    }
}
