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
/// 3 灵魂：整场战斗启用【连锁共鸣】，初始伤害 BaseDamage，
/// 每次触发后伤害 +DamageAdd。
/// 升级后：初始伤害 +UpgradeDamageBonus。
/// 备注：与 C#62【连连看】共用 ChainResonancePower。
///
/// 修复记录：
/// 原来的 OnPlay 只调用了 PowerCmd.Apply，从未调用
/// ChainResonancePower.ActivateForCombat。而 Power 内部用
/// _activeForCombat 是否为 true 来判断“当前是否应该监听连续同名牌”
/// （见 IsActive 属性）。没有 ActivateForCombat，_activeForCombat
/// 永远是 false，AfterCardPlayed 里 `if (!IsActive || !isChain) return;`
/// 会直接拦下所有触发，于是表现为“图标和数字都显示正常，
/// 但连续打同名牌永远不造成伤害”。
/// </summary>
public class ChainReactionCard() : PaleRegentModV1Card(3,
    CardType.Power, CardRarity.Uncommon,
    TargetType.Self)
{
    private const string DamageKey = "ChainResonancePower";
    private const string DamageAddKey = "ChainResonancePowerAdd";

    private const int BaseDamage = 5;
    private const int BaseDamageAdd = 2;

    private const int UpgradeDamageBonus = 2;

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
            // 第一次使用：创建 Power，并把 Amount 初始化为基础伤害。
            await PowerCmd.Apply<ChainResonancePower>(
                choiceContext,
                Owner.Creature,
                baseDamage,
                Owner.Creature,
                this);

            power = Owner.Creature.GetPower<ChainResonancePower>();
        }
        else if (power.Amount < baseDamage)
        {
            // 已经积累的伤害不能被重置。
            // 但如果这次使用的版本初始伤害更高，则提高到该最低值。
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                baseDamage - power.Amount,
                Owner.Creature,
                this);
        }

        // 必须调用 ActivateForCombat，否则 Power 虽然挂载、Amount 也正确，
        // 但 _activeForCombat 一直是 false，连锁效果永远不会真正触发。
        power?.ActivateForCombat(baseDamage, 0);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DamageKey]
            .UpgradeValueBy(UpgradeDamageBonus);
    }
}

