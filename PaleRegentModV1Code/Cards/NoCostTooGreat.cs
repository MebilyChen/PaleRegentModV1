using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【不惜代价】技能牌（机制文档：造物流 "No Cost Too Great"，占位设计）。
/// 1 灵魂 技能：失去 3 点生命，获得 3 点虚空，抽 1 张牌。
/// 升级后：失去生命不变，虚空 +1。
/// </summary>
public class NoCostTooGreat() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    private const int HpLoss = 3;
    private const int BaseVoidGain = 3;
    private const int DrawCount = 1;

    /// <summary>升级后额外虚空。</summary>
    private int _voidBonus;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 代价：失去生命（不受格挡、不吃力量加成，同原版 Bloodletting 写法）
        await CreatureCmd.Damage(choiceContext, Owner.Creature, HpLoss,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);

        await VoidResource.Gain(Owner, BaseVoidGain + _voidBonus);
        await VoidResource.SyncPower(choiceContext, Owner, this);

        await CardPileCmd.Draw(choiceContext, DrawCount, cardPlay.Player);
    }

    protected override void OnUpgrade()
    {
        _voidBonus = 1;
    }
}
