using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【入梦帷幕】技能牌。
/// 未升级：选择一个敌人，使其【苦痛之路】层数翻倍；为所有友方施加入梦；自身获得白根。
/// 升级后：所有敌人的【苦痛之路】层数翻倍；入梦数值按 DynamicVars 的升级值结算。
/// </summary>
public class DreamVeil : PaleRegentModV1Card
{
    private const int BaseDreamAmount = 3;
    private const int WhiteRootAmount = 1;

    public DreamVeil() : base(
        2,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AnyEnemy)
    {
    }

    // 未升级时选定单个敌人；升级后改为全体敌人，界面不会再要求选择一个无实际作用的目标。
    public override TargetType TargetType =>
        IsUpgraded ? TargetType.AllEnemies : TargetType.AnyEnemy;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DreamPower>((int?)null),
        HoverTipFactory.FromPower<WhiteRootPower>((int?)null)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<DreamPower>(BaseDreamAmount)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 苦痛之路：未升级作用于所选敌人；升级后作用于所有仍可被命中的敌人。
        if (IsUpgraded)
        {
            foreach (var enemy in Owner.Creature.CombatState.HittableEnemies.ToList())
            {
                await DoublePainPath(choiceContext, enemy);
            }
        }
        else if (cardPlay.Target is not null)
        {
            await DoublePainPath(choiceContext, cardPlay.Target);
        }

        // 为所有存活友方（包括自己）施加入梦。
        decimal dreamAmount = DynamicVars["DreamPower"].BaseValue;
        foreach (var player in CombatState.Players)
        {
            if (!player.Creature.IsAlive)
            {
                continue;
            }

            await PowerCmd.Apply<DreamPower>(
                choiceContext,
                player.Creature,
                dreamAmount,
                Owner.Creature,
                this);
        }

        // 自己获得 1 层白根。
        await PowerCmd.Apply<WhiteRootPower>(
            choiceContext,
            Owner.Creature,
            WhiteRootAmount,
            Owner.Creature,
            this);
    }

    /// <summary>
    /// 通过额外施加等同于当前层数的能力，使【苦痛之路】的总层数翻倍。
    /// 若目标没有该能力，则不施加任何层数。
    /// </summary>
    private async Task DoublePainPath(PlayerChoiceContext choiceContext, Creature target)
    {
        var painPath = target.Powers.OfType<PathOfPainPower>().FirstOrDefault();
        if (painPath is null || painPath.Amount <= 0m)
        {
            return;
        }

        await PowerCmd.Apply<PathOfPainPower>(
            choiceContext,
            target,
            painPath.Amount,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DreamPower"].UpgradeValueBy(2m);
    }
}
