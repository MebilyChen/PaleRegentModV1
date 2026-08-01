using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【召回精锐】技能牌（表 C#55，0727 新增）。
/// 1 灵魂 + 1 虚空：将消耗牌堆中 3 张驾驭点数最高的"俑卫"牌放回手牌
/// （点数相同则随机取）。
/// 备注（不改表格原文）：表格升级效果一栏写"升级后放回手牌"，与基础效果
/// 相同，疑似笔误；暂按"升级后改为放回 4 张"实现占位，请在表格里确认后
/// 告知我修正。
/// 俑卫判定：KingsRetainer / WingedRetainerCard。
/// 备注：当前实现中驾驭是角色身上的 HarnessPower 层数，不随单张俑卫牌
/// 记录，因此“驾驭点数最高的俑卫”按牌面基础数值（伤害/格挡）最高近似，
/// 同分随机；如需真正的“每张俑卫各自记录驾驭点数”需要改驾驭机制，请确认。
/// </summary>
public class EliteRecall : PaleRegentModV1Card
{
    private const int VoidCost = 1;

    /// <summary>放回张数（升级后 4，占位口径，见类注释）。</summary>
    private int _recallCount = 2;

    public EliteRecall() : base(1,
        CardType.Skill, CardRarity.Common,
        TargetType.Self)
    {
        CardTraits.SetVoidCost(this, VoidCost);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<KingsRetainer>(IsUpgraded)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 消耗堆中的俑卫牌，按驾驭点数降序；同点数用战斗随机流打乱保证公平
        List<CardModel> retainers = CardPile.GetCards(Owner, PileType.Exhaust)
            .Where(c => c is KingsRetainer or WingedRetainerCard)
            .ToList();
        if (retainers.Count == 0)
        {
            return;
        }

        // 先随机洗一遍（同分随机），再按驾驭值稳定降序排序
        List<CardModel> shuffled = new();
        List<CardModel> pool = new(retainers);
        while (pool.Count > 0)
        {
            CardModel pick = Owner.RunState.Rng.CombatTargets.NextItem(pool);
            pool.Remove(pick);
            shuffled.Add(pick);
        }

        foreach (CardModel card in shuffled
                     .OrderByDescending(GetRetainerValue)
                     .Take(_recallCount))
        {
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, null, false);
        }
    }

    /// <summary>俑卫牌的“驾驭点数”近似值：攻击俑卫取伤害基础值，格挡俑卫取格挡基础值。</summary>
    private static decimal GetRetainerValue(CardModel card) => card switch
    {
        KingsRetainer k => k.DynamicVars.Damage.BaseValue,
        WingedRetainerCard w => w.DynamicVars.Block.BaseValue,
        _ => 0m,
    };

    protected override void OnUpgrade()
    {
        _recallCount = 3;
    }
}
