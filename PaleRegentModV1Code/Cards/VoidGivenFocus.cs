using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空化神】生成牌（机制文档：造物流终端，"驯化"消耗 9 张虚空状态生成）。
/// 0 灵魂 攻击（全体）：对所有敌人造成 35 点伤害并施加 10 层【虚空之触】；
/// 然后选择一张手牌变为【虚空】状态牌（神性的代价）。
/// 纯粹。消耗。
/// 升级后：40 伤，15 层虚空之触。
/// </summary>
public class VoidGivenFocus() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Token,
    TargetType.AllEnemies)
{
    private const int BaseDamage = 35;
    private const int UpgradeDamageBonus = 5;
    private const int BaseTouch = 10;
    private const int UpgradeTouchBonus = 5;

    public override bool IsCreationCard => true;
    public override bool IsPure => true;

    /// <summary>
    /// Folly 特质（君王之剑式）：此牌生成时，将你所有的 Folly 加入手牌（若没有则生成一张）。
    /// </summary>
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card == this)
        {
            await CurseTraitHelper.Summon<Folly>(Owner);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move), new PowerVar<VoidTouchPower>(BaseTouch)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_giant_horizontal_slash")
            .Execute(choiceContext);

        await PowerCmd.Apply<VoidTouchPower>(choiceContext, CombatState!.HittableEnemies,
            DynamicVars["VoidTouchPower"].BaseValue, Owner.Creature, this);

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
        DynamicVars["VoidTouchPower"].UpgradeValueBy(UpgradeTouchBonus);
    }
}
