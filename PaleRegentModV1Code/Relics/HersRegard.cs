using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 王后信物（Hers Regard）—— 商店遗物。
/// 效果：
/// 1. 每场战斗开始时，获得 3 层白根（WhiteRootPower）。
///    参考原版 BeltBuckle：BeforeCombatStart + PowerCmd.Apply&lt;T&gt;(new ThrowingPlayerChoiceContext(), ...)。
/// 2. 拾起时，如果你拥有苍白信物，则将它替换为国王之魂；没有苍白信物则不生效。
///    参考原版 TouchOfOrobas.AfterObtained：RelicCmd.Replace(旧遗物, ModelDb.Relic&lt;新遗物&gt;().ToMutable())。
/// </summary>
public class HersRegard : PaleRegentModV1Relic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    private const int WhiteRootStacks = 3;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<WhiteRootPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, WhiteRootStacks, null, null);
    }

    public override async Task AfterObtained()
    {
        // 只在持有"苍白信物"本体时才替换；已经是国王之魂（子类）的不再处理
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
