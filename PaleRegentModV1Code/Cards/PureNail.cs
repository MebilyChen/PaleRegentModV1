using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【纯粹之钉】攻击牌。
/// 0 灵魂：选择手牌、抽牌堆、弃牌堆中的任意张牌附加【苍白】。带【纯粹】。
/// 造成伤害。本牌每因苍白实际取消 1 次【失心】或虚空花费，攻击次数 +1。
/// </summary>
public class PureNail() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Rare,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 5;
    private const int UpgradeDamageBonus = 3;
    /// <summary>【纯粹】特质：不受感染/变形类效果影响。</summary>
    public override bool IsPure => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [ModHoverTips.Lost, ModHoverTips.Pale
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // CanApplyPale 会排除规范实例、已有苍白的牌和虚空 X 费牌。
        List<CardModel> candidates = CardPile.GetCards(Owner, PileType.Hand)
            .Concat(CardPile.GetCards(Owner, PileType.Draw))
            .Concat(CardPile.GetCards(Owner, PileType.Discard))
            .Where(card => card != this && CardTraits.CanApplyPale(card))
            .Distinct()
            .ToList();

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, candidates.Count));

        int cancelledEffectCount = 0;
        foreach (CardModel card in selectedCards)
        {
            // 由 ApplyPale 原子返回本次真正取消的内容。
            // 虚空费用仅在“失心施加前原生存在且固定大于 0”时计 1 次；
            // 因失心将灵魂费并入而产生的虚空费不重复计数；
            // 虚空 0 费不计入，虚空 X 费仍由 CanApplyPale 排除。
            if (!CardTraits.ApplyPale(
                    card,
                    out bool cancelledLost,
                    out bool cancelledVoidCost))
            {
                continue;
            }

            if (cancelledLost)
            {
                cancelledEffectCount++;
            }

            if (cancelledVoidCost)
            {
                cancelledEffectCount++;
            }
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(1 + cancelledEffectCount)
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
