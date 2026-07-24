using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【不惜代价】技能牌（机制文档：造物流 "No Cost Too Great"）。
/// 0 灵魂 Rare 技能：
/// 生成 1 张【容器】加入手牌；将 1 张【羞愧】加入你的手牌；
/// 本场战斗每打出一次，此牌费用增加 1；结束你的回合。
/// 升级后：生成【容器+】。
/// </summary>
public class NoCostTooGreat() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Rare,
    TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 生成 1 张容器（升级后为容器+）加入手牌
        CardModel vessel = Owner.Creature.CombatState.CreateCard<Vessel>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(vessel, (CardPreviewStyle)1);
        }
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(vessel, PileType.Hand, Owner, (CardPilePosition)1),
            2.2f, (CardPreviewStyle)1);

        // 将 1 张羞愧加入手牌（特质召回：已有则全部移回手牌，没有才生成一张）
        await CurseTraitHelper.Summon<Shame>(Owner);

        // 本场战斗每打出一次，费用 +1
        EnergyCost.AddThisCombat(1, false);

        // 结束你的回合
        PlayerCmd.EndTurn(Owner, false, (Func<Task>)null);
    }

    protected override void OnUpgrade()
    {
        // 升级：生成【容器+】（见 OnPlay 的 IsUpgraded 分支）
    }
}
