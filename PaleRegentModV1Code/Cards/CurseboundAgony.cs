using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【咒痛宣告】技能牌（表 C#84，0727 新增）。
/// X 灵魂：对所有敌人施加 X 层【苦痛之路】，你的牌组每有 1 张诅咒牌，
/// 额外 +1 层。升级后：X+1 层。
/// 备注：诅咒数统计战斗内所有牌堆（手牌/抽牌/弃牌/消耗）中的诅咒牌。
/// </summary>
public class CurseboundAgony() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Rare,
    TargetType.AllEnemies)
{
    /// <summary>声明为主能量（灵魂）X 费。</summary>
    protected override bool HasEnergyCostX => true;

    /// <summary>升级后的额外层数（X+1）。</summary>
    private int _upgradeBonus;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PathOfPainPower>((int?)null)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        int curses = CardPile.GetCards(Owner,
                PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust)
            .Count(c => c.Type == CardType.Curse);

        int amount = x + curses + _upgradeBonus;
        if (amount <= 0)
        {
            return;
        }

        foreach (Creature enemy in CombatState!.GetOpponentsOf(Owner.Creature)
                     .Where(c => c.IsAlive).ToList())
        {
            await PowerCmd.Apply<PathOfPainPower>(choiceContext, enemy,
                amount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        _upgradeBonus = 1;
    }
}
