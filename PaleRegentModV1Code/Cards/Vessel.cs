using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【容器】生成牌（机制文档：造物流，"容器计划"每回合生成 / 容器药水）。
/// 0 灵魂 技能：对自己施加 1 层【纯粹封印】（封印期间不能攻击——
/// 占位用"每回合第一次攻击无效"实现）；
/// 消耗手牌中所有【感染】：少于 3 张 → 本卡变为【失败容器】，
/// 3 张及以上 → 变为【纯粹容器】。消耗。
///
/// 占位说明：文档中容器吸收感染孕育的流程较复杂，先做成
/// "打出时立刻结算吸收数量并变形"的简化版；变形后的牌进入弃牌堆
/// （TransformTo 保持原位置，本卡打出后进消耗堆——因此变形结果
/// 直接以新卡形式加入弃牌堆，本卡照常消耗）。
/// </summary>
public class Vessel() : PaleRegentModV1Card(0,
    CardType.Skill, CardRarity.Special,
    TargetType.Self)
{
    private const int SealAmount = 1;
    private const int PureThreshold = 3;

    public override bool IsCreationCard => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 封印自己：孕育期间不能攻击（占位：1 层纯粹封印）
        await PowerCmd.Apply<PureSealPower>(choiceContext, Owner.Creature, SealAmount, Owner.Creature, this);

        // 2. 吞噬手牌中所有感染
        List<CardModel> infections = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c is Infection)
            .ToList();
        foreach (CardModel infection in infections)
        {
            await CardCmd.Exhaust(choiceContext, infection);
        }

        // 3. 按吞噬数量孕育结果，加入弃牌堆
        if (infections.Count >= PureThreshold)
        {
            await CardPileCmd.AddToCombatAndPreview<PureVessel>(Owner.Creature, PileType.Discard, 1, Owner);
        }
        else
        {
            await CardPileCmd.AddToCombatAndPreview<FailedVessel>(Owner.Creature, PileType.Discard, 1, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
