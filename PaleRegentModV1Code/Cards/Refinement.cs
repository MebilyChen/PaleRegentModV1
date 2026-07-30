using MegaCrit.Sts2.Core.HoverTips;
using PaleRegentModV1.PaleRegentModV1Code.Traits;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【精益求精】
/// 抽 1 张牌。将一张国王佣卫加入手牌。获得 3 层驾驭。
/// 升级后：获得 5 层驾驭，并生成升级版国王佣卫。
/// </summary>
public class Refinement() : PaleRegentModV1Card(
    1,
    CardType.Skill,
    CardRarity.Common,
    TargetType.Self)
{
    private const int BaseHarness = 3;
    private const int DrawCount = 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        ModHoverTips.Harness,
        HoverTipFactory.FromCard<KingsRetainer>(IsUpgraded),
        ModHoverTips.Mould
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<HarnessPower>(BaseHarness)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // 1. 抽牌
        await CardPileCmd.Draw(
            choiceContext,
            DrawCount,
            cardPlay.Player);

        // 2. 创建国王佣卫
        CardModel retainer =
            CombatState.CreateCard<KingsRetainer>(Owner);

        // 升级后的精益求精生成国王佣卫+
        if (IsUpgraded)
        {
            CardCmd.Upgrade(retainer);
        }

        // 3. 将生成牌加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(
            retainer,
            PileType.Hand,
            Owner);

        // 4. 获得驾驭
        await PowerCmd.Apply<HarnessPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["HarnessPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HarnessPower"].UpgradeValueBy(2m);
    }
}
