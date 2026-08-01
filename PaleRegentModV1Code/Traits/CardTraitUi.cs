using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 刷新场景中指定卡牌的 Pure / Pale / Lost 装饰。
/// 不调用私有的 NCard.Reload()，只更新我们自己的装饰节点。
///
/// ============ 与旧版的区别 ============
/// 旧版去 NCard 的直接子节点里找 BaseLib 建的 "{类型名}_TEMP" 临时节点，
/// 找不到就静默 return。这有三个问题：
///   1. 原版牌（非模组牌）根本没有这个节点 → 灵魂护佑给原版牌加纯粹时永远不显示；
///   2. 该节点在 NCard 里的层级不确定，只查一层子节点经常找不到；
///   3. 找不到时那句"卡牌下次正常加载时 BaseLib 会调用 CreateCustomUi"的注释
///      对原版牌是不成立的，等于永远不会显示。
///
/// 新版直接调用 CardTraitOverlay.Attach()，由它在 NCard 下建一个
/// 固定名字的容器（PaleRegentTraitOverlay），所有牌一视同仁。
/// 同时顺手安装后台同步节点，卡牌重建后卡框会自动回来。
/// </summary>
public static class CardTraitUi
{
    /// <summary>
    /// 特质变化后调用（ApplyLost / ApplyPale / ApplyPure 里已经在调）。
    /// 立即刷新当下场景里所有对应这张牌的 NCard。
    /// </summary>
    public static void Refresh(CardModel card)
    {
        if (card == null) return;

        // 保证后台同步节点已安装：即便这次没找到 NCard
        // （例如卡牌还在牌库里、或者刚好正在重建），
        // 它进手牌时也会被同步节点自动补上卡框。
        CardTraitOverlay.InstallSync();

        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        RefreshRecursive(tree.Root, card);
    }

    private static void RefreshRecursive(Node node, CardModel card)
    {
        if (!GodotObject.IsInstanceValid(node)) return;

        if (node is NCard nCard)
        {
            if (ReferenceEquals(nCard.Model, card))
                RefreshCardNode(nCard, card);

            // NCard 内部不会再嵌套另一张 NCard。
            return;
        }

        foreach (Node child in node.GetChildren())
            RefreshRecursive(child, card);
    }

    private static void RefreshCardNode(NCard nCard, CardModel card)
    {
        Control? container = CardTraitOverlay.Attach(nCard, card);
        if (container == null) return;

        CardTraitOverlay.Refresh(container, card);
    }
}