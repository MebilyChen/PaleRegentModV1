using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【痛苦汇流】攻击牌（表 C#99，0727 新增）。
/// 1 灵魂：造成 12 点伤害，施加 X 层【苦痛之路】，
/// X = 本场战斗所有玩家打出过的攻击牌张数（含本张）。
/// 升级后：17 点伤害。
/// </summary>
public class ConfluenceOfPain() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Rare,
    TargetType.AnyEnemy)
{
    public override CardMultiplayerConstraint MultiplayerConstraint
        => CardMultiplayerConstraint.MultiplayerOnly;
    
    private const int BaseDamage = 12;
    private const int UpgradeDamageBonus = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PathOfPainPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 统计本场战斗所有玩家打出过的攻击牌张数（含本张，写法参考原版 BansheesCry）
        int attackCount = CombatManager.Instance.History.CardPlaysStarted
            .Count(e => e.CardPlay.Card.Type == CardType.Attack);

        if (attackCount > 0 && cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<PathOfPainPower>(choiceContext, cardPlay.Target,
                attackCount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
    }
}
