using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【连锁反应】能力牌（表 C#71，0727 新增）。
/// 3 灵魂：整场战斗启用【连锁共鸣】。
/// 每连续打出 1 张同名牌，对所有敌人造成 BaseDamage 点伤害，
/// 并获得 BaseDamage 点格挡。
/// 升级后：初始伤害与格挡 +UpgradeDamageBonus。
/// 备注：与 C#62【连连看】共用 ChainResonancePower。
/// </summary>
public class ChainReactionCard() : PaleRegentModV1Card(
    3,
    CardType.Power,
    CardRarity.Uncommon,
    TargetType.Self)
{
    private const string DamageKey = "ChainResonancePower";
    private const string DamageAddKey = "ChainResonancePowerAdd";

    private const int BaseDamage = 5;
    private const int BaseDamageAdd = 0; // 连锁反应不提供“每次触发 +N”。
    private const int UpgradeDamageBonus = 2;

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ChainResonancePower>(BaseDamage),
        new DynamicVar(DamageAddKey, BaseDamageAdd)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        decimal baseDamage = DynamicVars[DamageKey].BaseValue;
        decimal damageAdd = DynamicVars[DamageAddKey].BaseValue;

        ChainResonancePower? power =
            Owner.Creature.GetPower<ChainResonancePower>();

        if (power == null)
        {
            // 第一次打出：创建 Power，Amount 初始化为 baseDamage。
            await PowerCmd.Apply<ChainResonancePower>(
                choiceContext,
                Owner.Creature,
                baseDamage,
                Owner.Creature,
                this);

            power = Owner.Creature.GetPower<ChainResonancePower>();
        }
        else
        {
            // 再次打出：在已有 Amount 上增加 baseDamage。
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                baseDamage,
                Owner.Creature,
                this);
        }

        // 必须启用整场战斗监听，否则连续同名牌不会触发效果。
        power?.ActivateForCombat(baseDamage, damageAdd);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DamageKey].UpgradeValueBy(UpgradeDamageBonus);
    }
}
