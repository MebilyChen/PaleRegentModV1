using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Resources;

namespace PaleRegentModV1.PaleRegentModV1Code.Cards;

/// <summary>
/// 【疫收】技能牌（机制文档：瘟疫流）。
/// 1 灵魂 技能：消耗手牌中所有【感染】，每张获得 1 点虚空并抽 1 张牌。
/// 升级后：每张获得 2 点虚空并抽 2 张牌。
/// </summary>
public class PlagueHarvest() : PaleRegentModV1Card(1,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    private const int BaseVoidPerInfection = 1;
    private const int BaseDrawPerInfection = 1;

    /// <summary>升级后每张感染额外虚空/抽牌。</summary>
    private int _voidBonus;
    private int _drawBonus;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> infections = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c is Infection)
            .ToList();
        if (infections.Count == 0)
        {
            return;
        }

        foreach (CardModel infection in infections)
        {
            await CardCmd.Exhaust(choiceContext, infection);
        }

        await VoidResource.Gain(Owner, infections.Count * (BaseVoidPerInfection + _voidBonus));
        await VoidResource.SyncPower(choiceContext, Owner, this);

        await CardPileCmd.Draw(choiceContext,
            infections.Count * (BaseDrawPerInfection + _drawBonus), cardPlay.Player);
    }

    protected override void OnUpgrade()
    {
        _voidBonus = 1;
        _drawBonus = 1;
    }
}
