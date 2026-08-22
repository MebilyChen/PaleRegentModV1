using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 王后信物（Hers Regard）—— 商店遗物。
/// 效果：
/// 1. 自己的第一回合开始后，获得 3 层白根（WhiteRootPower）。
/// 2. 拾起时，如果你拥有苍白信物，则将它替换为国王之魂；没有苍白信物则不生效。
/// </summary>
public class HersRegard : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    private const int WhiteRootStacks = 3;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        // 多人模式下：仅在该遗物拥有者自己的回合触发。
        // RoundNumber 从 1 开始，因此仅限本场战斗的第一回合。
        if (player != Owner || Owner.Creature.CombatState.RoundNumber != 1)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<WhiteRootPower>(
            choiceContext,
            Owner.Creature,
            WhiteRootStacks,
            null,
            null);
    }

    public override async Task AfterObtained()
    {
        // 只在持有“苍白信物”本体时才替换；已经是国王之魂（子类）的不再处理。
        RelicModel? paleToken = null;
        foreach (RelicModel relic in Owner.Relics)
        {
            if (relic is PaleToken && relic is not Kingsoul)
            {
                paleToken = relic;
                break;
            }
        }

        if (paleToken == null)
        {
            return;
        }

        RelicModel kingsoul = ModelDb.Relic<Kingsoul>().ToMutable();
        await RelicCmd.Replace(paleToken, kingsoul);
    }
}