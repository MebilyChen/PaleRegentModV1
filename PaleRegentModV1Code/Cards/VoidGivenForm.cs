using PaleRegentModV1.PaleRegentModV1Code.Traits;
using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【虚空化形】生成牌（机制文档：造物流，"虚空实验"X≥3 / "驯化"5+ 生成）。
/// 0 灵魂 攻击（全体）：对所有敌人造成 10 点伤害并施加 3 层【虚空之触】。
/// 纯粹。消耗。
/// 升级后：15 伤，5 层虚空之触。
/// </summary>
public class VoidGivenForm() : PaleRegentModV1Card(0,
    CardType.Attack, CardRarity.Token,
    TargetType.AllEnemies)
{
    private const int BaseDamage = 15;
    private const int UpgradeDamageBonus = 5;
    private const int BaseTouch = 3;
    private const int UpgradeTouchBonus = 2;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ModHoverTips.Lost, ModHoverTips.Pure,
         HoverTipFactory.FromPower<VoidTouchPower>((int?)null)];

    public override bool IsCreationCard => true;
    public override bool IsPure => true;

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
        
        // 从手牌选择牌附加【失心】
        // filter：过滤掉不能失心的牌（X 费牌）和自己
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1),
            (CardModel c) => c != this && CardTraits.CanApplyLost(c),
            this);

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyLost(card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        DynamicVars["VoidTouchPower"].UpgradeValueBy(UpgradeTouchBonus);
    }
    
    //不进入奖励池
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<TokenCardPool>();
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;
}
