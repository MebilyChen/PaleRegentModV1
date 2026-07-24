using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【完美格挡】罕见技能牌（重型防御）。
/// 4 灵魂：获得 10 点格挡；接下来 2 回合，每回合开始时获得 3 点格挡。
/// 升级后：获得 15 点格挡，每回合开始时获得 5 点格挡。
///
/// 机制要点：
/// - "接下来 2 回合各 +3 格挡"用自定义的 EchoWardPower 实现
///   （层数 = 剩余回合数，每回合开始 +3 格挡并减 1 层）。
///   不用原版 BlockNextTurnPower 是因为它只触发一次就消失。
///
/// 修改指南：
/// - 即时格挡：BaseBlock / UpgradeBlockBonus 常量。
/// - 持续回合数：EchoTurns 常量。
/// - 每回合格挡量：_echoBlockPerTurn 字段（施加时写入 EchoWardPower.BlockPerTurn）。
/// </summary>
public class PerfectBlock() : PaleRegentModV1Card(4,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    /// <summary>打出时立即获得的格挡。</summary>
    private const int BaseBlock = 10;
    /// <summary>升级后即时格挡增加量（10→15）。</summary>
    private const int UpgradeBlockBonus = 5;
    /// <summary>回响守护持续的回合数（= EchoWardPower 初始层数）。</summary>
    private const int EchoTurns = 2;
    /// <summary>回响守护每回合格挡量（升级后 5）。</summary>
    private int _echoBlockPerTurn = 3;

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 立即获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 施加【回响守护】：接下来 EchoTurns 回合每回合开始获得格挡（基础3，升级5）
        //    先把每回合格挡量写入 Power（静态配置，后施加者会覆盖前者）
        EchoWardPower.BlockPerTurn = _echoBlockPerTurn;
        await PowerCmd.Apply<EchoWardPower>(choiceContext, cardPlay.Player.Creature, EchoTurns, cardPlay.Player.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
        _echoBlockPerTurn = 5;
    }
}
