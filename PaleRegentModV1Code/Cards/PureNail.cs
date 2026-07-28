using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【纯粹之钉】攻击牌（表 C#82，0727 新增）。
/// 0 灵魂：你的所有手牌获得【苍白】；每有 1 张牌因此被取消【失心】，
/// 本次攻击次数 +1。造成 5 点伤害（次数 = 1 + 取消失心数）。
/// 升级后：8 点伤害。
/// </summary>
public class PureNail() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Rare,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 1) 全部手牌附加苍白，统计其中原本带失心的张数（苍白会取消失心）
        int lostCancelled = 0;
        List<CardModel> hand = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c != this)
            .ToList();
        foreach (CardModel card in hand)
        {
            bool wasLost = CardTraits.IsLost(card);
            CardTraits.ApplyPale(card);
            if (wasLost && !CardTraits.IsLost(card))
            {
                lostCancelled++;
            }
        }

        // 2) 攻击：次数 = 1 + 取消失心数
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(1 + lostCancelled)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
