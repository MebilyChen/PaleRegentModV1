using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【王家集结】技能牌（表 C#96，0727 新增）。
/// 2 灵魂：获得 13 点格挡，在手牌中生成 1 张【国王俑卫】，获得 10 点驾驭。
/// 升级后：生成的国王俑卫已升级。
/// </summary>
public class RoyalMuster() : PaleRegentModV1Card(2,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseBlock = 13;
    private const int HarnessGain = 10;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];
    // 声明"这张牌提供格挡"，游戏会据此显示格挡预览等 UI
    public override bool GainsBlock => true;

    // 带 Defend 标签：与"对防御牌生效"的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<KingsRetainer>(IsUpgraded), ModHoverTips.Harness];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 手牌生成 1 张国王俑卫（升级后生成已升级版本，写法参考原版 Charge）
        CardModel retainer = Owner.Creature.CombatState.CreateCard<KingsRetainer>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(retainer, CardPreviewStyle.None);
        }
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(retainer, PileType.Hand, Owner),
            2.2f, CardPreviewStyle.HorizontalLayout);

        // 3. 获得驾驭（HarnessPower，写法同 Wingforging）
        await PowerCmd.Apply<HarnessPower>(choiceContext, Owner.Creature,
            HarnessGain, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级效果：生成的国王俑卫已升级（见 OnPlay 中 IsUpgraded 传参）
    }
}
