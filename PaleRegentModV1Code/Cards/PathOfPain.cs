using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【试炼】稀有技能牌（效果"苦痛之路"的载体；旧名：苦痛之路）。
/// 3 灵魂 技能：对所有敌人施加 5 层【苦痛之路】。
/// 升级后：施加 10 层。
/// </summary>
public class PathOfPain() : PaleRegentModV1Card(3,
    CardType.Skill, CardRarity.Rare,
    TargetType.AllEnemies)
{
    private const int BaseAmount = 5;

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PathOfPainPower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PathOfPainPower>(BaseAmount)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对所有可打敌人施加苦痛之路（HittableEnemies 写法参考 modstudy WeaveMemoryNeedle）
        List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
        foreach (Creature enemy in enemies)
        {
            await PowerCmd.Apply<PathOfPainPower>(choiceContext, enemy,
                DynamicVars["PathOfPainPower"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：5 层 → 10 层
        DynamicVars["PathOfPainPower"].UpgradeValueBy(5m);
    }
}
