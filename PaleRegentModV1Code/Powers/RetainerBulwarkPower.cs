using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Powers;

/// <summary>
/// 【卫群壁垒】buff（效果表 P#28，卡牌 C#88 卫群壁垒 施加）。
/// 效果：每生成 1 张"俑卫"牌，获得 [层数] 点格挡。
///
/// 实现说明：
/// - 挂 AfterCardGeneratedForCombat，判定生成的牌是否为俑卫牌
///   （目前为 KingsRetainer 国王俑卫 / WingedRetainerCard 有翼俑卫，
///   集中在 IsRetainerCard 判定，后续新俑卫在此处登记即可）；
/// - 格挡为 Power 触发格挡，不吃敏捷加成（Unpowered 口径，与荆棘类一致）。
/// </summary>
public class RetainerBulwarkPower : PaleRegentModV1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>俑卫牌判定（新增俑卫在这里登记）。</summary>
    internal static bool IsRetainerCard(CardModel card) =>
        card is KingsRetainer or WingedRetainerCard;

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (!IsRetainerCard(card))
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount,
            MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, cardPlay: null);
    }
}
