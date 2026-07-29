using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 刷新场景中指定卡牌的 Pure、Pale、Lost 装饰。
/// 不调用私有的 NCard.Reload()，只更新自定义装饰节点。
/// </summary>
public static class CardTraitUi
{
    public static void Refresh(CardModel card)
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        RefreshRecursive(tree.Root, card);
    }

    private static void RefreshRecursive(Node node, CardModel card)
    {
        if (node is NCard nCard &&
            ReferenceEquals(nCard.Model, card))
        {
            RefreshCardNode(nCard, card);

            // NCard 内部通常不再包含另一张 NCard。
            return;
        }

        foreach (Node child in node.GetChildren())
        {
            RefreshRecursive(child, card);
        }
    }

    private static void RefreshCardNode(
        NCard nCard,
        CardModel card)
    {
        // BaseLib 创建的临时 UI 节点名称：
        // 卡牌实际类型名 + "_TEMP"
        string expectedRootName =
            $"{card.GetType().Name}_TEMP";

        foreach (Node child in nCard.GetChildren())
        {
            if (child is not Control root)
                continue;

            if (root.Name.ToString() != expectedRootName)
                continue;

            CardTraitOverlay.Refresh(root, card);
            return;
        }

        // 找不到临时节点时不需要处理。
        // 卡牌下次正常加载时，BaseLib 会调用 CreateCustomUi。
    }
}