using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【守梦者】攻击牌（表 C#73，0727 新增）。
/// 1 灵魂：造成 3 点伤害，获得 3 点格挡，获得 3 层入梦。带【纯粹】。
/// 升级后：造成 6 点伤害，获得 6 点格挡，获得 6 层入梦。
/// </summary>
public class Dreamers() : PaleRegentModV1Card(1,
    CardType.Attack, CardRarity.Common,
    TargetType.AnyEnemy)
{
    private const int BaseDamage = 3;
    private const int UpgradeDamageBonus = 3;
    private const int BaseBlock = 3;
    private const int UpgradeBlockBonus = 3;
    private const int BaseDreamStacks = 3;
    private const int UpgradeDreamStacks = 3;

    private int _dreamStacks = BaseDreamStacks;

    // 带 Defend 标签：与“对防御牌生效”的效果联动（原版惯例）
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    /// <summary>【纯粹】特质：不受感染/变形类效果影响。</summary>
    public override bool IsPure => true;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DreamPower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move),
         new BlockVar(BaseBlock, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        await PowerCmd.Apply<DreamPower>(choiceContext, Owner.Creature, _dreamStacks, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
        _dreamStacks += UpgradeDreamStacks;
    }
}
