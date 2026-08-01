using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【铸翼】技能牌（表 C#76，0727 新增）。
/// 1 灵魂：将 1 张【飞翼俑卫】加入手牌，获得 3 点驾驭；
/// 选择 1 张手牌，使其获得【苍白】。
/// 升级后：生成【飞翼俑卫+】，获得 6 点驾驭。
/// </summary>
public class Wingforging() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseHarness = 3;
    private const int UpgradeHarnessBonus = 3;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<WingedRetainerCard>(IsUpgraded),
         ModHoverTips.Pale];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<HarnessPower>(BaseHarness)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1) 生成飞翼俑卫加入手牌（升级后生成升级版）
        CardModel retainer = Owner.Creature.CombatState.CreateCard<WingedRetainerCard>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(retainer, CardPreviewStyle.None);
        }
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(retainer, PileType.Hand, Owner),
            2.2f, CardPreviewStyle.HorizontalLayout);

        // 2) 获得驾驭
        await PowerCmd.Apply<HarnessPower>(choiceContext, Owner.Creature,
            DynamicVars["HarnessPower"].BaseValue, Owner.Creature, this);

        // 3) 选择 1 张手牌获得【苍白】（排除本卡；写法参考 SoulSpell）
        // 20260801：虚空X 费的牌不能附加苍白，选牌阶段就过滤掉，
        // 避免玩家选完之后静默失败。判定唯一来源：CardTraits.CanApplyPale。
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            (CardModel c) => c != this && CardTraits.CanApplyPale(c),
            this);

        foreach (CardModel card in selected)
        {
            CardTraits.ApplyPale(card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HarnessPower"].UpgradeValueBy(UpgradeHarnessBonus);
    }
}
