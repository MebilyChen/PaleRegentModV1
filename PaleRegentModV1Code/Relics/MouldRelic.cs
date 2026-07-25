using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using PaleRegentModV1.PaleRegentModV1Code.Traits;

namespace PaleRegentModV1.PaleRegentModV1Code.Relics;

/// <summary>
/// 【模具·［卡牌名］】遗物基类（表格遗物 R#2 / 名词 N#9，20260725 新增）。
/// 效果：你的每回合开始时，生成并打出［卡牌名］；1 场战斗后失效
/// （已拥有时再次判定成功 → 剩余场数 +1，见 MouldHelper）。
///
/// 实现说明：
/// - "生成并打出"：CreateCard + CardCmd.AutoPlay（与 modstudy Havoc 式一致），
///   自动打出不经过手牌，天然"去除 Harness 临时效果"按普通结算处理；
///   注：Harness 加成挂在 HarnessPower 上，AutoPlay 的造物牌仍会吃到，
///   表格要求"去除 Harness 临时效果"——因此打出前临时压制：
///   由于造物牌读取 HarnessPower 的路径分散（伤害走 Power 钩子、格挡主动读），
///   这里用 MouldAutoPlayFlag 静态开关，HarnessPower 与 WingedRetainerCard
///   检测到开关时跳过加成。
/// - 场数管理：CombatsLeft 初始 1，每次战斗结束 -1，到 0 移除（碎裂）。
/// </summary>
public abstract class MouldRelic : PaleRegentModV1Relic
{
    /// <summary>正在由模具遗物自动打出（HarnessPower 检测此开关跳过加成）。</summary>
    public static bool MouldAutoPlayFlag { get; private set; }

    /// <summary>剩余战斗场数（1 场后碎裂；重复获得 +1）。</summary>
    public int CombatsLeft = MouldHelper.RelicCombats;

    // 备注：表格未指定稀有度；用 Event（项目已验证枚举）避免进入常规商店/奖励池。
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <summary>对应的模具牌类型（供 MouldHelper 匹配去重）。</summary>
    public abstract Type MouldCardType { get; }

    /// <summary>生成对应的模具牌实例。</summary>
    protected abstract CardModel CreateMouldCard();

    /// <summary>延长剩余战斗场数（重复获得时调用）。</summary>
    public void ExtendCombats(int combats)
    {
        CombatsLeft += combats;
        Flash();
    }

    /// <summary>你的每回合开始：生成并打出对应的牌（钩子签名同 modstudy HornetMaterialVault）。</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }
        Flash();
        CardModel card = CreateMouldCard();
        // 先加入战斗（生成到手牌），随后自动打出
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner, (CardPilePosition)1);
        MouldAutoPlayFlag = true;
        try
        {
            await CardCmd.AutoPlay(choiceContext, card, (Creature?)null, (AutoPlayType)1, false, false);
        }
        finally
        {
            MouldAutoPlayFlag = false;
        }
    }

    /// <summary>战斗结束：剩余场数 -1，到 0 碎裂。</summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        CombatsLeft--;
        if (CombatsLeft <= 0)
        {
            await RelicCmd.Remove(this);
        }
    }
}
