using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【入梦帷幕】技能牌（机制文档：入梦、白根）。
/// 2 灵魂 技能：对场上所有友方施加 5 层【入梦】，自己获得 1 层【白根】。
/// 升级后：改为 10 层入梦。
/// </summary>
public class DreamVeil : PaleRegentModV1Card
{
    private const int BaseDreamAmount = 5;
    private const int WhiteRootAmount = 1;

    public DreamVeil() : base(2,
        CardType.Skill, CardRarity.Rare,
        TargetType.Self)
    {
    }

    /// <summary>手牌聚焦悬停词条（机制表：关键词/生成牌 Hover Card Preview）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DreamPower>((int?)null),
         HoverTipFactory.FromPower<WhiteRootPower>((int?)null)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DreamPower>(BaseDreamAmount)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal amount = DynamicVars["DreamPower"].BaseValue;

        // 自己
        // await PowerCmd.Apply<DreamPower>(choiceContext, Owner.Creature,
        // amount, Owner.Creature, this);
        
        // 所有友方 包括自己
        foreach (var player in CombatState.Players)
        {
            // 通常不应给已死亡的队友添加能力
            if (!player.Creature.IsAlive)
                continue;

            await PowerCmd.Apply<DreamPower>(
                choiceContext,
                player.Creature, // 每位玩家是能力目标
                amount,
                Owner.Creature,  // 你是能力施加者
                this);
        }
        
        // 所有敌人
        //foreach (var enemy in Owner.Creature.CombatState.HittableEnemies.ToList())
        //{
        //await PowerCmd.Apply<DreamPower>(choiceContext, enemy,
        //amount, Owner.Creature, this);
        //}

        // 自己获得 1 层白根
        await PowerCmd.Apply<WhiteRootPower>(choiceContext, Owner.Creature,
            WhiteRootAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DreamPower"].UpgradeValueBy(5m);
    }
}
