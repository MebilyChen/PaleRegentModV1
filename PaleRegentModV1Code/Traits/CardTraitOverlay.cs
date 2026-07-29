using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Extensions;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 在卡牌原有画面上叠加 Pure、Pale、Lost 的透明装饰。
/// </summary>
public static class CardTraitOverlay
{
    // ImagePath() 已经会自动添加 images/，这里不要再写 images/。
    private const string PureOverlayPath = "pure_overlay.png";
    private const string PaleOverlayPath = "pale_overlay.png";
    private const string LostOverlayPath = "lost_overlay.png";

    private const string PureOverlayName = "PureOverlay";
    private const string PaleOverlayName = "PaleOverlay";
    private const string LostOverlayName = "LostOverlay";

    /// <summary>
    /// BaseLib 创建临时 UI 节点时调用。
    /// 此时 root 还没有被添加到 NCard，因此等待 Ready 后再建立布局。
    /// </summary>
    public static void Create(Control root, CardModel card)
    {
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.ClipContents = false;
        root.ZIndex = 1000;

        root.Ready += () =>
        {
            // 再等一帧，确保 NCard 的尺寸和布局已经计算完成。
            SceneTree tree = root.GetTree();

            void OnNextFrame()
            {
                tree.ProcessFrame -= OnNextFrame;

                if (!GodotObject.IsInstanceValid(root))
                    return;

                ConfigureRoot(root);
                Refresh(root, card);

                string parentInfo =
                    root.GetParent() is Control parent
                        ? $"{parent.GetType().Name}, size={parent.Size}"
                        : root.GetParent()?.GetType().Name ?? "null";

                GD.Print(
                    $"[CardTraitOverlay] Created: " +
                    $"card={card.Id}, rootSize={root.Size}, parent={parentInfo}"
                );
            }

            tree.ProcessFrame += OnNextFrame;
        };
    }

    /// <summary>
    /// 卡牌特质改变后，更新现有装饰。
    /// </summary>
    public static void Refresh(Control root, CardModel card)
    {
        ConfigureRoot(root);

        RemoveOverlay(root, PureOverlayName);
        RemoveOverlay(root, PaleOverlayName);
        RemoveOverlay(root, LostOverlayName);

        bool isPure = CardTraits.IsPure(card);
        bool isPale = CardTraits.IsPale(card);
        bool isLost = CardTraits.IsLost(card);

        GD.Print(
            $"[CardTraitOverlay] Refresh: " +
            $"card={card.Id}, Pure={isPure}, Pale={isPale}, Lost={isLost}, " +
            $"rootSize={root.Size}"
        );

        if (isPure)
        {
            AddOverlay(
                root,
                PureOverlayPath,
                PureOverlayName,
                10
            );
        }

        if (isPale)
        {
            AddOverlay(
                root,
                PaleOverlayPath,
                PaleOverlayName,
                20
            );
        }

        if (isLost)
        {
            AddOverlay(
                root,
                LostOverlayPath,
                LostOverlayName,
                20
            );
        }
    }

    /// <summary>
    /// 让 BaseLib 提供的临时节点覆盖整个 NCard。
    /// </summary>
    private static void ConfigureRoot(Control root)
    {
        root.AnchorLeft = 0f;
        root.AnchorTop = 0f;
        root.AnchorRight = 1f;
        root.AnchorBottom = 1f;

        root.OffsetLeft = 0f;
        root.OffsetTop = 0f;
        root.OffsetRight = 0f;
        root.OffsetBottom = 0f;

        root.Position = Vector2.Zero;
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.ClipContents = false;
        root.ZIndex = 1000;
        root.Visible = true;

        // 额外显式同步父节点尺寸，防止锚点布局没有立即刷新。
        if (root.GetParent() is Control parent)
        {
            root.Size = parent.Size;
        }
    }

    private static void RemoveOverlay(
        Control root,
        string nodeName)
    {
        Node? oldOverlay =
            root.GetNodeOrNull<Node>(nodeName);

        if (oldOverlay == null)
            return;

        root.RemoveChild(oldOverlay);
        oldOverlay.QueueFree();
    }

    private static void AddOverlay(
        Control parent,
        string imagePath,
        string nodeName,
        int zIndex)
    {
        string fullPath = imagePath.ImagePath();

        Texture2D texture =
            PreloadManager.Cache.GetTexture2D(fullPath);

        TextureRect overlay = new()
        {
            Name = nodeName,
            Texture = texture,

            ExpandMode =
                TextureRect.ExpandModeEnum.IgnoreSize,

            StretchMode =
                TextureRect.StretchModeEnum.Scale,

            MouseFilter =
                Control.MouseFilterEnum.Ignore,

            ZIndex = zIndex,
            Visible = true,
            Modulate = Colors.White
        };

        overlay.AnchorLeft = 0f;
        overlay.AnchorTop = 0f;
        overlay.AnchorRight = 1f;
        overlay.AnchorBottom = 1f;

        overlay.OffsetLeft = 0f;
        overlay.OffsetTop = 0f;
        overlay.OffsetRight = 0f;
        overlay.OffsetBottom = 0f;

        parent.AddChild(overlay);

        GD.Print(
            $"[CardTraitOverlay] Added {nodeName}: " +
            $"path={fullPath}, textureSize={texture.GetSize()}, " +
            $"overlaySize={overlay.Size}"
        );
    }
}
