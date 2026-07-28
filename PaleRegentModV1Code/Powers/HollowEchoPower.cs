using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【空魂回响】buff（效果表 P#26，卡牌 C#75 空魂回响 施加）。
/// 效果：回合开始时若灵魂（能量）为 0，本回合打出的第一张牌
/// 会额外打出一张它的【失心】复制品。
///
/// 实现说明：
/// - AfterEnergyReset 之后各遗物/Power 还会增减能量（苍白信物按虚空扣灵魂），
///   因此判定挂在 AfterEnergyResetLate（若无 Late 钩子则 AfterEnergyReset 末尾判定，
///   本实现按 AfterEnergyReset，苍白信物扣灵魂同为 AfterEnergyReset，顺序取决于
///   注册顺序——如实测判定过早，可改为 AfterSideTurnStart 里读能量；已在此备注）；
/// - "第一张牌"：用 _armedThisTurn 标记，本回合首次 AfterCardPlayed 时消耗标记；
/// - 复制品：CreateCard 克隆同 ModelId 的新卡（保留升级状态），施加失心
///   （CardTraits.ApplyLost，失心自带重放1+虚空费转换），再 AutoPlay 打出；
/// - 复制品是打出型 Token，打出后按失心/消耗规则处理；
/// - _resolving 防止复制品自己再触发复制。
/// </summary>
public class HollowEchoPower : PaleRegentModV1Power
{
    /// <summary>本回合是否已"武装"（回合开始灵魂为 0）。</summary>
    private bool _armedThisTurn;

    /// <summary>递归保护：复制品打出时不再触发。</summary>
    private bool _resolving;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        // 回合开始灵魂为 0 才武装（备注：能量重置一般会回满，
        // 本判定针对"上限被扣光/虚空吃满导致 0 灵魂开局"的构筑场景）
        _armedThisTurn = player.PlayerCombatState != null
            && player.PlayerCombatState.Energy <= 0;
        if (_armedThisTurn)
        {
            Flash();
        }
        await Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_armedThisTurn || _resolving || cardPlay.Player != Owner.Player)
        {
            return;
        }

        _armedThisTurn = false;
        _resolving = true;
        try
        {
            Flash();
            ICombatState? combatState = Owner.CombatState;
            if (combatState == null)
            {
                return;
            }

            // 克隆一张同名复制品（CreateCard(canonicalCard, owner) 会保留升级状态）
            CardModel copy = combatState.CreateCard(cardPlay.Card, Owner.Player!);
            // 施加失心（不可施加时按原样复制，已在条目后备注）
            if (CardTraits.CanApplyLost(copy))
            {
                CardTraits.ApplyLost(copy);
            }
            // 加入战斗并自动打出（沿用队列中原目标不可知，自动选目标）
            await CardPileCmd.AddGeneratedCardToCombat(copy, MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand,
                Owner.Player, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top);
            await CardCmd.AutoPlay(choiceContext, copy, cardPlay.Target,
                MegaCrit.Sts2.Core.Entities.Cards.AutoPlayType.Default);
        }
        finally
        {
            _resolving = false;
        }
    }
}
