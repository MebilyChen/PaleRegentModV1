using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【苍白增援】技能牌（表 C#101，0727 新增，多人协作牌）。
/// 0 灵魂：一名其他玩家的手牌中生成 1 张已添加【苍白】的随机俑卫牌。
/// 升级后：生成的俑卫牌已升级。
/// 备注：随机俑卫 = 国王俑卫 / 飞翼俑卫 二选一；单人游戏时兜底为自己获得（表格未说明单人行为，见条目备注）。
/// </summary>
public class PaleReinforcements() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<KingsRetainer>(IsUpgraded),
         HoverTipFactory.FromCard<WingedRetainerCard>(IsUpgraded),
         ModHoverTips.Pale];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 找到另一名玩家；单人时兜底为自己
        List<Player> others = CombatState!.PlayerCreatures
            .Select(c => c.Player)
            .OfType<Player>()
            .Where(p => p != Owner)
            .ToList();
        Player targetPlayer = others.Count > 0
            ? Owner.RunState.Rng.CombatTargets.NextItem(others)
            : Owner;

        // 随机生成一种俑卫牌
        bool kings = Owner.RunState.Rng.CombatTargets.NextBool();
        CardModel retainer = kings
            ? Owner.Creature.CombatState.CreateCard<KingsRetainer>(targetPlayer)
            : Owner.Creature.CombatState.CreateCard<WingedRetainerCard>(targetPlayer);

        if (IsUpgraded)
        {
            CardCmd.Upgrade(retainer, CardPreviewStyle.None);
        }

        // 添加苍白
        CardTraits.ApplyPale(retainer);

        // 加入目标玩家手牌
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(retainer, PileType.Hand, targetPlayer),
            2.2f, CardPreviewStyle.HorizontalLayout);
    }

    protected override void OnUpgrade()
    {
        // 升级效果：生成的俑卫牌已升级（见 OnPlay 中 IsUpgraded 判断）
    }
}
