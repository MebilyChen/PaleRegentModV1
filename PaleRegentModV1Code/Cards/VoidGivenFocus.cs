using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空化神】生成牌（机制文档：造物流终端，"驯化"消耗 9 张虚空状态生成）。
/// 0 灵魂 攻击（全体）：对所有敌人造成 35 点伤害并施加 10 层【虚空之触】；
/// 然后选择一张手牌变为【虚空】状态牌（神性的代价）。
/// 纯粹。消耗。
/// </summary>
public class VoidGivenFocus() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Token,
    TargetType.AllEnemies)
{
    private const int BaseDamage = 35;
    private const int UpgradeDamageBonus = 10;
    private const int TouchAmount = 10;

    public override bool IsCreationCard => true;
    public override bool IsPure => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_giant_horizontal_slash")
            .Execute(choiceContext);

        await PowerCmd.Apply<VoidTouchPower>(choiceContext, CombatState!.HittableEnemies, TouchAmount, Owner.Creature, this);

        // 神性的代价：选择一张手牌变为【虚空】状态牌
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            (CardModel c) => c != this,
            this);
        foreach (CardModel card in selected.ToList())
        {
            await CardCmd.TransformTo<TheVoidStatus>(card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
