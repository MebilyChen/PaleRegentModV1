using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【灵魂轰击】攻击牌（表 C#83，0727 新增）。
/// 4 灵魂：造成 30 点伤害。打出后，你所有的【灵魂轰击】本场战斗伤害 +5。
/// 升级后：35 点伤害，每次 +7。
/// 备注（不改表格原文）：表格未写明加伤范围，参考原版 Claw 按"本场战斗、
/// 全部同名牌（含各牌堆）"实现；如需跨战斗永久加伤请告知。
/// </summary>
public class SoulBlast : PaleRegentModV1Card
{
    private const string IncreaseKey = "Increase";
    private const int BaseDamage = 30;
    private const int UpgradeDamageBonus = 5;

    public SoulBlast() : base(4,
        CardType.Attack, CardRarity.Rare,
        TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move),
         new DynamicVar(IncreaseKey, 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 参考原版 Claw：战斗内全部同名牌伤害成长
        decimal increase = DynamicVars[IncreaseKey].BaseValue;
        foreach (SoulBlast blast in Owner.PlayerCombatState!.AllCards.OfType<SoulBlast>())
        {
            blast.DynamicVars.Damage.BaseValue += increase;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        DynamicVars[IncreaseKey].UpgradeValueBy(2m);
    }
}
