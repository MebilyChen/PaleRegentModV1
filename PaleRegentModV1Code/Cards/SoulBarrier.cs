using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂屏障】技能牌（表 C#58，0727 新增）。
/// 1 灵魂：获得 7 点格挡，将 1 张相同的牌（灵魂屏障）放入弃牌堆。
/// 升级后：10 点格挡（生成的同名牌为升级版）。
/// </summary>
public class SoulBarrier() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Common,
    TargetType.Self)
{
    private const int BaseBlock = 7;
    private const int UpgradeBlockBonus = 3;

    // 声明"这张牌提供格挡"，游戏会据此显示格挡预览等 UI
    public override bool GainsBlock => true;

    // 带 Defend 标签：与"对防御牌生效"的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 生成 1 张同名牌放入弃牌堆（升级状态跟随本卡）
        CardModel copy = Owner.Creature.CombatState.CreateCard<SoulBarrier>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(copy, CardPreviewStyle.None);
        }
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, Owner),
            0f, CardPreviewStyle.HorizontalLayout);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
    }
}
