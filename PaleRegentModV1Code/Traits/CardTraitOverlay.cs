using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using PaleRegentModV1.PaleRegentModV1Code.Extensions;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 在卡牌原有画面上叠加 Pure / Pale / Lost 的透明卡框装饰。
///
/// ============ 本版（v3）相对上一版的两处关键修正 ============
///
/// 【1】不再新建自定义 Node 类型。
/// 上一版建了一个 CardTraitOverlaySync : Node 来做每帧轮询，结果日志里刷满：
///     System.ArgumentException: Value does not fall within the expected range.
///        at MonoMod...JitHookDelegateHolder.CompileMethodHook
///        at CardTraitOverlaySync.InvokeGodotClassMethod
/// 原因：在运行时加载的 mod DLL 里 new 一个【新的 Godot Node 子类】，
/// 会和 Harmony / MonoMod 的 JIT 钩子冲突，每次引擎回调 _Process 都抛一次异常。
/// 新版改为订阅 SceneTree.ProcessFrame 事件（静态方法，不注册任何新类型），
/// 效果一样，异常消失。
///
/// 【2】换了挂载位置，让卡框不再遮住文字和费用。
/// 旧版把容器挂成 NCard 的【最后一个子节点】，于是它盖住 NCard 下的一切——
/// 卡名、描述、灵魂费，以及 RitsuLib 以 NodeAttachment 形式挂在 NCard 上的虚空费 UI。
/// 靠 z_index 是解决不了的：z 调高会穿到隔壁卡上面，z 调低会被卡面底图盖住。
/// 新版把容器插到【卡面美术节点的下一个兄弟】位置，绘制顺序变成
///     卡面底图 → 卡框 → 文字 / 费用 / 关键词 / 图标
/// 卡框正好夹在中间。开关见 InsertBelowUi。
///
/// ============ 坐标系背景（v2 已确认，仍然适用） ============
/// STS2 的 NCard 是 Control，但它的 Size 恒为 (0,0)，不使用 Control 的矩形布局，
/// 而是像 Node2D 一样靠子节点局部坐标 + 卡面 Sprite2D（Centered=true）定位。
/// 所以绝对不能用锚点铺满，必须自己反推卡面矩形再手动摆位；
/// 反推出的 Position 通常是负值（例如 (-138, -199)），这是正常的。
/// </summary>
public static class CardTraitOverlay
{
    // =====================================================================
    //  你可能需要调的东西都在这一段
    // =====================================================================

    /// <summary>
    /// true（默认）= 把卡框插到卡面美术的后面一位，
    ///               于是文字、灵魂费、虚空费、关键词图标都会压在卡框之上；
    /// false        = 退回旧行为，卡框挂在 NCard 最后，盖住卡内所有元素。
    /// 如果发现卡框反而被卡面底图整个盖住了（完全看不见），把它改成 false 再看。
    /// </summary>
    public static bool InsertBelowUi = true;

    /// <summary>
    /// 卡框整体放大系数。1.0 = 与卡面等大；1.10 = 四周各外扩 5%。
    /// 【卡框偏小就调大，偏大就调小，只动这一个数字。】
    /// 扩张以卡面矩形中心为锚点，放大不会破坏已经对齐好的位置。
    /// </summary>
    public static float OverlayScale = 0.4f;

    /// <summary>在 OverlayScale 之后再叠加的像素级外扩（X = 左右各扩，Y = 上下各扩）。</summary>
    public static Vector2 OverlayPadding = Vector2.Zero;

    /// <summary>
    /// true  = 保持贴图宽高比并居中（比例不一致时会留边，看起来偏小）；
    /// false = 无条件拉伸填满容器（默认）。
    /// 你的 overlay png 是 652×878（比 0.743），卡面 300×422（比 0.711），差约 4%。
    /// </summary>
    public static bool KeepAspect = false;

    /// <summary>
    /// 调试开关：画半透明品红色块（容器范围）+ 青色小方块（容器原点）。
    ///   看得到品红块但看不到卡框 → 贴图问题（路径 / 预载 / 透明度）；
    ///   连品红块都看不到        → 布局或层级问题（尺寸 0 / 被遮挡 / 被裁剪）。
    /// </summary>
    public static bool DebugFrame = false;

    /// <summary>详细日志。默认关闭，打开后也只在数值变化时打印，不会刷屏。</summary>
    public static bool VerboseLog = false;

    /// <summary>后台同步间隔（秒）。</summary>
    public static float SyncInterval = 0.15f;

    /// <summary>每张贴图的独立微调：(相对容器的缩放, 像素偏移)。</summary>
    private static readonly Dictionary<string, (float Scale, Vector2 Offset)> PerOverlayTweak = new()
    {
        [PureOverlayName] = (1.00f, Vector2.Zero),
        [PaleOverlayName] = (1.00f, Vector2.Zero),
        [LostOverlayName] = (1.00f, Vector2.Zero),
    };

    /// <summary>兜底卡牌尺寸（实机量到的真实卡面尺寸，正常用不上）。</summary>
    private static readonly Vector2 FallbackCardSize = new(300f, 422f);

    // =====================================================================
    //  常量
    // =====================================================================

    public const string ContainerName = "PaleRegentTraitOverlay";
    public const string MaskMetaKey   = "pale_regent_trait_mask";
    private const string RectMetaKey  = "pale_regent_trait_rect";

    // ImagePath() 已经会自动添加 images/，这里不要再写 images/。
    private const string PureOverlayPath = "pure_overlay.png";
    private const string PaleOverlayPath = "pale_overlay.png";
    private const string LostOverlayPath = "lost_overlay.png";

    private const string PureOverlayName = "PureOverlay";
    private const string PaleOverlayName = "PaleOverlay";
    private const string LostOverlayName = "LostOverlay";
    private const string DebugFrameName  = "TraitDebugFrame";
    private const string DebugOriginName = "TraitDebugOrigin";

    public const int MaskPure = 1 << 0;
    public const int MaskPale = 1 << 1;
    public const int MaskLost = 1 << 2;

    private static bool _candidatesLogged;
    private static bool _syncHooked;
    private static ulong _lastSyncMs;

    // ---------------- 绑定表 ----------------

    private sealed class Binding
    {
        public Control Container = null!;
        public WeakReference<CardModel> Card = null!;

        /// <summary>卡面美术节点，用来决定容器在树里的插入位置和矩形。</summary>
        public Node? Anchor;
    }

    private static readonly List<Binding> Bindings = new();

    // =====================================================================
    //  对外入口
    // =====================================================================

    /// <summary>BaseLib 创建临时 UI 节点时调用（保留旧签名，调用方不用改）。</summary>
    public static void Create(Control root, CardModel card)
    {
        if (root == null || card == null) return;

        InstallSync();

        root.MouseFilter  = Control.MouseFilterEnum.Ignore;
        root.ClipContents = false;

        // 旧版无条件订阅 Ready；如果 root 已经在树里，Ready 早发过了，
        // Godot 的信号不补发 → lambda 永不执行 → 什么都不会建。
        if (root.IsInsideTree())
            DeferAttachFrom(root, card);
        else
            root.Ready += () => DeferAttachFrom(root, card);
    }

    /// <summary>在卡牌节点（通常是 NCard）下创建 / 复用装饰容器。幂等。</summary>
    public static Control? Attach(Node cardNode, CardModel card)
    {
        if (cardNode == null || card == null || !GodotObject.IsInstanceValid(cardNode))
            return null;

        InstallSync();

        // 容器现在可能挂在 NCard 的子孙层，所以要整棵子树找。
        // FindChild 的第三个参数 owned 必须传 false：运行时 AddChild 的节点没有 owner。
        if (cardNode.FindChild(ContainerName, true, false) is Control existing &&
            GodotObject.IsInstanceValid(existing))
        {
            Bind(existing, card, null);
            return existing;
        }

        // ★ 找卡面美术节点作为插入锚点，容器插在它后面一位。
        //   这样后面绘制的文字 / 费用 / 图标都会压在卡框之上。
        Node? face = InsertBelowUi ? FindCardFaceNode(cardNode) : null;
        Node  host = face?.GetParent() ?? cardNode;

        Control container = new()
        {
            Name         = ContainerName,
            MouseFilter  = Control.MouseFilterEnum.Ignore,
            ClipContents = false,

            // z 必须是 0。Godot 没有 CSS 那种层叠上下文，z_index 在整个 CanvasLayer
            // 范围内比较，任何大于"相邻卡 z 间距"的值都会让卡框浮到隔壁牌上面。
            // 卡内的上下关系一律靠【树序】控制。
            ZIndex      = 0,
            ZAsRelative = true,

            Visible = true,
        };

        host.AddChild(container);

        int desired = face != null
            ? face.GetIndex() + 1
            : host.GetChildCount() - 1;
        desired = Mathf.Clamp(desired, 0, host.GetChildCount() - 1);
        host.MoveChild(container, desired);

        if (VerboseLog)
        {
            GD.Print(
                $"[CardTraitOverlay] Attach: host={host.Name}[{host.GetType().Name}], " +
                $"anchor={face?.Name.ToString() ?? "(无)"}, index={container.GetIndex()}/" +
                $"{host.GetChildCount() - 1}");
        }

        Bind(container, card, face);
        return container;
    }

    /// <summary>刷新某张牌当前所有已绑定的装饰容器。</summary>
    public static void RefreshAll(CardModel card)
    {
        if (card == null) return;

        Prune();
        foreach (Binding b in Bindings.ToArray())
        {
            if (!b.Card.TryGetTarget(out CardModel? bound)) continue;
            if (!ReferenceEquals(bound, card)) continue;
            if (!GodotObject.IsInstanceValid(b.Container)) continue;

            Refresh(b.Container, card);
        }
    }

    /// <summary>刷新指定容器里的装饰。</summary>
    public static void Refresh(Control container, CardModel card)
    {
        if (container == null || card == null || !GodotObject.IsInstanceValid(container))
            return;

        int mask = BuildMask(card);
        container.SetMeta(MaskMetaKey, mask);

        RemoveChildByName(container, PureOverlayName);
        RemoveChildByName(container, PaleOverlayName);
        RemoveChildByName(container, LostOverlayName);
        RemoveChildByName(container, DebugFrameName);
        RemoveChildByName(container, DebugOriginName);

        // 先摆好容器，子节点才能拿到正确尺寸。
        ApplyLayout(container);

        if (VerboseLog)
        {
            GD.Print(
                $"[CardTraitOverlay] Refresh: card={card.Id}, mask={mask}, " +
                $"containerRect=({container.Position}, {container.Size})");
        }

        if (DebugFrame)
            AddDebugFrame(container);

        // 三张图之间的上下关系靠 AddChild 先后顺序：Pure 在底，Pale / Lost 在上。
        if ((mask & MaskPure) != 0) AddOverlay(container, PureOverlayPath, PureOverlayName);
        if ((mask & MaskPale) != 0) AddOverlay(container, PaleOverlayPath, PaleOverlayName);
        if ((mask & MaskLost) != 0) AddOverlay(container, LostOverlayPath, LostOverlayName);

        SyncChildren(container);
    }

    /// <summary>把三个特质压成一个 int，便于比较"要不要重建装饰"。</summary>
    public static int BuildMask(CardModel card)
    {
        if (card == null) return 0;

        int mask = 0;
        if (CardTraits.IsPure(card)) mask |= MaskPure;
        if (CardTraits.IsPale(card)) mask |= MaskPale;
        if (CardTraits.IsLost(card)) mask |= MaskLost;
        return mask;
    }

    // =====================================================================
    //  后台同步（不新建任何 Node 类型）
    // =====================================================================

    /// <summary>
    /// 挂上每帧回调（幂等）。Create / Attach / CardTraitUi.Refresh 都会调，
    /// 正常情况下你不需要手动调用。
    ///
    /// 这里【故意不新建 Node 子类】：mod DLL 里 new 一个新的 Godot Node 类型，
    /// 会触发 MonoMod JIT 钩子异常（上一版日志里刷屏的那个 ArgumentException）。
    /// 订阅 SceneTree.ProcessFrame 只是注册一个静态委托，不注册新类型，因此安全。
    /// </summary>
    public static void InstallSync()
    {
        if (_syncHooked) return;
        if (Engine.GetMainLoop() is not SceneTree tree) return;

        _syncHooked = true;
        tree.ProcessFrame += OnProcessFrame;

        GD.Print("[CardTraitOverlay] 后台同步已挂到 SceneTree.ProcessFrame。");
    }

    private static void OnProcessFrame()
    {
        ulong now      = Time.GetTicksMsec();
        ulong interval = (ulong)Mathf.Max(16f, SyncInterval * 1000f);

        if (now - _lastSyncMs < interval) return;
        _lastSyncMs = now;

        try
        {
            SyncScene();
        }
        catch (Exception e)
        {
            // 绝不能把异常抛回引擎主循环，否则会连锁刷屏。
            GD.PushWarning($"[CardTraitOverlay] 同步异常：{e}");
        }
    }

    /// <summary>扫描场景树，给每张 NCard 补容器、重申树序、重算布局。</summary>
    private static void SyncScene()
    {
        if (Engine.GetMainLoop() is not SceneTree tree) return;

        Prune();
        SyncRecursive(tree.Root);
    }

    private static void SyncRecursive(Node node)
    {
        if (!GodotObject.IsInstanceValid(node)) return;

        if (node is NCard nCard)
        {
            SyncCardNode(nCard);
            return; // NCard 内部不会再嵌套 NCard
        }

        foreach (Node child in node.GetChildren())
            SyncRecursive(child);
    }

    private static void SyncCardNode(NCard nCard)
    {
        CardModel card = nCard.Model;
        if (card == null) return;

        Control? container = nCard.FindChild(ContainerName, true, false) as Control;

        if (container == null || !GodotObject.IsInstanceValid(container))
        {
            container = Attach(nCard, card);
            if (container == null) return;
        }

        // 1) 重申树序：卡牌重建、动态加图标都可能把容器挤走，
        //    表现为"卡框有时候盖住文字有时候不盖"，极难复现。
        ReassertOrder(container);

        // 2) 重算布局：入场动画、悬停放大期间尺寸一直在变，
        //    而 NCard 的 Size 恒为 0、Resized 信号没有参考价值，只能轮询。
        ApplyLayout(container);

        // 3) 特质变了才重建贴图。
        int mask = BuildMask(card);
        int old  = container.HasMeta(MaskMetaKey) ? container.GetMeta(MaskMetaKey).AsInt32() : -1;

        if (old != mask)
            Refresh(container, card);
        else
            SyncChildren(container);
    }

    /// <summary>把容器摆回"卡面美术的下一位"，保证它压住底图但被 UI 压住。</summary>
    private static void ReassertOrder(Control container)
    {
        Node? host = container.GetParent();
        if (host == null) return;

        int last = host.GetChildCount() - 1;
        if (last < 0) return;

        Binding? b = FindBinding(container);
        Node? anchor = b?.Anchor;

        if (InsertBelowUi &&
            anchor != null &&
            GodotObject.IsInstanceValid(anchor) &&
            ReferenceEquals(anchor.GetParent(), host))
        {
            int ai = anchor.GetIndex();
            int ci = container.GetIndex();

            // MoveChild 的语义是"先摘掉再插入"，所以目标下标要分情况：
            // 容器原本在锚点【前面】时目标是 ai，在后面时目标是 ai + 1。
            int target = ci < ai ? ai : ai + 1;
            target = Mathf.Clamp(target, 0, last);

            if (ci != target) host.MoveChild(container, target);
            return;
        }

        // 没有锚点（或关掉了 InsertBelowUi）：退回"放最后"。
        if (container.GetIndex() != last)
            host.MoveChild(container, last);
    }

    // =====================================================================
    //  内部：绑定
    // =====================================================================

    private static void DeferAttachFrom(Control root, CardModel card)
    {
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(root)) return;

            Node host = FindCardHost(root) ?? root;
            Attach(host, card);
        }).CallDeferred();
    }

    private static Node? FindCardHost(Node node)
    {
        Node? cur = node;
        while (cur != null)
        {
            if (cur is NCard) return cur;
            cur = cur.GetParent();
        }
        return null;
    }

    private static Binding? FindBinding(Control container)
    {
        foreach (Binding b in Bindings)
        {
            if (ReferenceEquals(b.Container, container)) return b;
        }
        return null;
    }

    private static void Bind(Control container, CardModel card, Node? anchor)
    {
        Prune();

        Binding? existing = FindBinding(container);
        if (existing != null)
        {
            existing.Card = new WeakReference<CardModel>(card);
            if (anchor != null) existing.Anchor = anchor;
            DeferRefresh(container, card);
            return;
        }

        Bindings.Add(new Binding
        {
            Container = container,
            Card      = new WeakReference<CardModel>(card),
            Anchor    = anchor,
        });

        DeferRefresh(container, card);
    }

    private static void DeferRefresh(Control container, CardModel card)
    {
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(container)) return;
            Refresh(container, card);
        }).CallDeferred();
    }

    private static void Prune()
    {
        for (int i = Bindings.Count - 1; i >= 0; i--)
        {
            Binding b = Bindings[i];
            if (!GodotObject.IsInstanceValid(b.Container) || !b.Card.TryGetTarget(out _))
                Bindings.RemoveAt(i);
        }
    }

    // =====================================================================
    //  内部：布局
    // =====================================================================

    private static void ApplyLayout(Control container)
    {
        if (!GodotObject.IsInstanceValid(container)) return;

        Rect2 card = ResolveCardRect(container);

        // 以卡面矩形【中心】为锚点向四周扩张。
        // 只加 Size 不改 Position 的话，图会朝右下单向长出去，对齐会被破坏。
        Vector2 center  = card.Position + card.Size * 0.5f;
        Vector2 size    = card.Size * Mathf.Max(0.01f, OverlayScale) + OverlayPadding * 2f;
        Vector2 topLeft = center - size * 0.5f;

        // 锚点全部归 0：位置与尺寸完全由我们控制。
        // 【不能用 SetAnchorsAndOffsetsPreset(FullRect)】——NCard 的 Size 恒为 (0,0)，
        // 铺满 0×0 的结果就是容器尺寸 (0,0)，一个像素都画不出来。
        container.AnchorLeft   = 0f;
        container.AnchorTop    = 0f;
        container.AnchorRight  = 0f;
        container.AnchorBottom = 0f;

        container.Position    = topLeft;
        container.Size        = size;
        container.PivotOffset = size * 0.5f;

        if (VerboseLog)
        {
            Rect2 applied = new(topLeft, size);
            Rect2 last = container.HasMeta(RectMetaKey)
                ? container.GetMeta(RectMetaKey).AsRect2()
                : new Rect2();

            if (!applied.IsEqualApprox(last))
            {
                container.SetMeta(RectMetaKey, applied);
                GD.Print(
                    $"[CardTraitOverlay] Layout: cardRect={card}, scale={OverlayScale}, " +
                    $"finalPos={container.Position}, finalSize={container.Size}, " +
                    $"global={container.GlobalPosition}");
            }
        }

        SyncChildren(container);
    }

    private static void SyncChildren(Control container)
    {
        if (!GodotObject.IsInstanceValid(container)) return;

        Vector2 size = container.Size;

        foreach (Node child in container.GetChildren())
        {
            if (child is not Control c) continue;
            if (c.Name.ToString() == DebugOriginName) continue; // 原点标记不参与缩放

            (float s, Vector2 off) =
                PerOverlayTweak.TryGetValue(c.Name.ToString(), out var t)
                    ? t
                    : (1f, Vector2.Zero);

            Vector2 childSize = size * s;

            c.AnchorLeft = 0f; c.AnchorTop = 0f;
            c.AnchorRight = 0f; c.AnchorBottom = 0f;

            c.Size = childSize;
            // 中心对齐：子节点比容器大或小时都居中，而不是贴左上角。
            c.Position    = (size - childSize) * 0.5f + off;
            c.PivotOffset = childSize * 0.5f;
        }
    }

    /// <summary>
    /// 求出卡面在【容器父节点的局部坐标系】里的矩形。
    /// 优先用绑定时记录的卡面锚点直接算（最确定），其次才做候选搜索。
    /// </summary>
    private static Rect2 ResolveCardRect(Control container)
    {
        Node? parentNode = container.GetParent();
        if (parentNode == null)
            return new Rect2(-FallbackCardSize * 0.5f, FallbackCardSize);

        // A：用锚点节点。容器就插在它旁边，两者同一个父级，坐标直接可用。
        Binding? b = FindBinding(container);
        if (b?.Anchor is { } anchor &&
            GodotObject.IsInstanceValid(anchor) &&
            ReferenceEquals(anchor.GetParent(), parentNode) &&
            TryGetLocalRect(anchor, out Rect2 anchorRect) &&
            LooksLikeCard(anchorRect.Size))
        {
            return anchorRect;
        }

        // B：父节点是 Control 且尺寸有效（STS2 的 NCard 走不到这里，它的 Size 是 0）。
        if (parentNode is Control pc && pc.Size.X > 1f && pc.Size.Y > 1f)
            return new Rect2(Vector2.Zero, pc.Size);

        // C：从兄弟节点里挑"最像整张卡"的最大矩形。
        List<Rect2>? log = (VerboseLog && !_candidatesLogged) ? new List<Rect2>() : null;

        Rect2 best = new();
        float bestArea = 0f;
        CollectCandidates(parentNode, container, 0, Vector2.Zero, ref best, ref bestArea, log);

        if (log is { Count: > 0 })
        {
            _candidatesLogged = true;
            GD.Print("[CardTraitOverlay] 卡面矩形候选（只打印一次）：");
            foreach (Rect2 r in log) GD.Print($"    {r}");
            GD.Print($"    → 选中 {best}");
        }

        if (bestArea > 0f) return best;

        // D：全都推不出来 —— 假定以父原点为中心。
        return new Rect2(-FallbackCardSize * 0.5f, FallbackCardSize);
    }

    /// <summary>在 NCard 子树里找"卡面美术"：面积最大且长得像整张卡的可绘制节点。</summary>
    private static Node? FindCardFaceNode(Node root)
    {
        Node? best = null;
        float bestArea = 0f;
        FindFaceRecursive(root, 0, ref best, ref bestArea);
        return best;
    }

    private static void FindFaceRecursive(Node node, int depth, ref Node? best, ref float bestArea)
    {
        if (depth > 4) return;

        foreach (Node child in node.GetChildren())
        {
            if (child.Name.ToString() == ContainerName) continue; // 别把自己人算进去

            if (TryGetLocalRect(child, out Rect2 r) && LooksLikeCard(r.Size))
            {
                float area = r.Size.X * r.Size.Y;
                if (area > bestArea)
                {
                    bestArea = area;
                    best     = child;
                }
            }

            FindFaceRecursive(child, depth + 1, ref best, ref bestArea);
        }
    }

    /// <summary>
    /// 取一个节点在其父级局部坐标系里的矩形。
    /// Sprite2D.GetRect() 已经处理了 Centered / Offset / AtlasTexture 区域，
    /// 它返回的负 Position 正是"Control 左上角"与"Sprite 中心原点"之间的换算量。
    /// </summary>
    private static bool TryGetLocalRect(Node node, out Rect2 rect)
    {
        switch (node)
        {
            case Sprite2D s when s.Texture != null:
            {
                Rect2 local = s.GetRect();
                rect = new Rect2(s.Position + local.Position * s.Scale, local.Size * s.Scale);
                return true;
            }

            case TextureRect tr when tr.Texture != null && tr.Size.X > 1f:
                rect = new Rect2(tr.Position, tr.Size);
                return true;

            case Control c when c.Size.X > 1f && c.Size.Y > 1f:
                rect = new Rect2(c.Position, c.Size);
                return true;
        }

        rect = default;
        return false;
    }

    private static void CollectCandidates(
        Node node, Control self, int depth, Vector2 offset,
        ref Rect2 best, ref float bestArea, List<Rect2>? log)
    {
        foreach (Node child in node.GetChildren())
        {
            if (ReferenceEquals(child, self)) continue;
            if (child.Name.ToString() == ContainerName) continue;

            if (TryGetLocalRect(child, out Rect2 local))
            {
                Rect2 world = new(local.Position + offset, local.Size);
                if (LooksLikeCard(world.Size))
                {
                    log?.Add(world);

                    float area = world.Size.X * world.Size.Y;
                    if (area > bestArea) { bestArea = area; best = world; }
                }
            }

            Vector2 childOffset = child switch
            {
                Node2D n2 => offset + n2.Position,
                Control c => offset + c.Position,
                _         => offset,
            };

            if (depth < 2)
                CollectCandidates(child, self, depth + 1, childOffset, ref best, ref bestArea, log);
        }
    }

    /// <summary>
    /// 合理性过滤：排除全屏 Control、小图标、细长条。
    /// 一张卡的宽高比约 0.71（300×422），这里放宽到 0.45 ~ 1.15。
    /// </summary>
    private static bool LooksLikeCard(Vector2 size)
    {
        if (size.X < 40f || size.Y < 40f) return false;
        if (size.X > 2000f || size.Y > 2000f) return false;

        float aspect = size.X / size.Y;
        return aspect > 0.45f && aspect < 1.15f;
    }

    // =====================================================================
    //  内部：装饰节点
    // =====================================================================

    private static void RemoveChildByName(Control container, string nodeName)
    {
        Node? old = container.GetNodeOrNull<Node>(nodeName);
        if (old == null) return;

        container.RemoveChild(old);
        old.QueueFree();
    }

    private static void AddOverlay(Control container, string imagePath, string nodeName)
    {
        Texture2D? texture = LoadTexture(imagePath);
        if (texture == null)
        {
            GD.PushError(
                $"[CardTraitOverlay] 贴图加载失败，跳过 {nodeName}：{imagePath}。" +
                $"请确认文件已随模组打包，并被资源预载流程收录。");
            return;
        }

        TextureRect overlay = new()
        {
            Name        = nodeName,
            Texture     = texture,
            ExpandMode  = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = KeepAspect
                ? TextureRect.StretchModeEnum.KeepAspectCentered
                : TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,

            // z 归零，理由同容器：z_index 是全局比较的，会穿透到隔壁卡上面。
            ZIndex      = 0,
            ZAsRelative = true,

            Visible  = true,
            Modulate = Colors.White,
        };

        container.AddChild(overlay);

        overlay.AnchorLeft = 0f; overlay.AnchorTop = 0f;
        overlay.AnchorRight = 0f; overlay.AnchorBottom = 0f;
        overlay.Position = Vector2.Zero;
        overlay.Size     = container.Size;

        if (VerboseLog)
        {
            GD.Print(
                $"[CardTraitOverlay] Added {nodeName}: textureSize={texture.GetSize()}, " +
                $"overlaySize={overlay.Size}, visibleInTree={overlay.IsVisibleInTree()}");
        }
    }

    private static void AddDebugFrame(Control container)
    {
        ColorRect rect = new()
        {
            Name        = DebugFrameName,
            Color       = new Color(1f, 0f, 1f, 0.25f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex      = 0,
            ZAsRelative = true,
        };
        container.AddChild(rect);
        rect.Position = Vector2.Zero;
        rect.Size     = container.Size;

        // 青色小方块标出容器原点：在卡框左上角 = 坐标系正确；在卡牌正中 = 原点没换算。
        ColorRect origin = new()
        {
            Name        = DebugOriginName,
            Color       = new Color(0f, 1f, 1f, 0.9f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex      = 0,
            ZAsRelative = true,
            Position    = new Vector2(-4f, -4f),
            Size        = new Vector2(8f, 8f),
        };
        container.AddChild(origin);
    }

    private static Texture2D? LoadTexture(string imagePath)
    {
        string fullPath = imagePath.ImagePath();

        try
        {
            Texture2D? cached = PreloadManager.Cache.GetTexture2D(fullPath);
            if (cached != null) return cached;
        }
        catch (Exception e)
        {
            GD.PushWarning($"[CardTraitOverlay] PreloadManager 取图异常 {fullPath}: {e.Message}");
        }

        // 兜底：直接走 ResourceLoader。
        // 日志里的 "Asset not cached: res://...pure_overlay.png" 就是走了这条路，
        // 能正常加载，只是绕过了预载缓存，不影响显示。
        try
        {
            if (ResourceLoader.Exists(fullPath))
                return ResourceLoader.Load<Texture2D>(fullPath);
        }
        catch (Exception e)
        {
            GD.PushWarning($"[CardTraitOverlay] ResourceLoader 兜底失败 {fullPath}: {e.Message}");
        }

        return null;
    }

    // =====================================================================
    //  诊断工具（出问题时手动调）
    // =====================================================================

    /// <summary>算出 CanvasItem 的有效 z（把 ZAsRelative 的层层累加算清楚）。</summary>
    public static int GetEffectiveZ(CanvasItem item)
    {
        int z = 0;
        CanvasItem? cur = item;

        while (cur != null)
        {
            z += cur.ZIndex;
            if (!cur.ZAsRelative) break;
            cur = cur.GetParent() as CanvasItem;
        }

        return z;
    }

    /// <summary>打印所有 NCard 的 z 值、树序、TopLevel 以及子树结构。</summary>
    public static void DumpCardZ()
    {
        if (Engine.GetMainLoop() is not SceneTree tree) return;

        GD.Print("=============== Card Z dump ===============");
        DumpCardZRecursive(tree.Root);
        GD.Print("===========================================");
    }

    private static void DumpCardZRecursive(Node node)
    {
        if (node is NCard nc)
        {
            GD.Print(
                $"NCard model={nc.Model?.Id} z={nc.ZIndex} rel={nc.ZAsRelative} " +
                $"effZ={GetEffectiveZ(nc)} treeIdx={nc.GetIndex()} topLevel={nc.TopLevel}");

            DumpTree(nc, 1, 3);
            return;
        }

        foreach (Node c in node.GetChildren())
            DumpCardZRecursive(c);
    }

    /// <summary>把一棵子树连同变换信息打印出来，用于定位坐标系 / 层级问题。</summary>
    public static void DumpTree(Node node, int depth = 0, int maxDepth = 6)
    {
        if (node == null || depth > maxDepth) return;

        string pad = new(' ', depth * 2);
        string info = node switch
        {
            Sprite2D s =>
                $"Sprite2D centered={s.Centered} rect={s.GetRect()} pos={s.Position} " +
                $"scale={s.Scale} z={s.ZIndex}/rel={s.ZAsRelative} vis={s.IsVisibleInTree()}",

            Control c =>
                $"Control size={c.Size} pos={c.Position} clip={c.ClipContents} " +
                $"z={c.ZIndex}/rel={c.ZAsRelative} vis={c.IsVisibleInTree()}",

            Node2D n =>
                $"Node2D pos={n.Position} scale={n.Scale} z={n.ZIndex}/rel={n.ZAsRelative}",

            _ => "(非 CanvasItem)"
        };

        GD.Print($"{pad}└─ [{node.GetIndex()}] {node.Name} [{node.GetType().Name}] {info}");

        foreach (Node child in node.GetChildren())
            DumpTree(child, depth + 1, maxDepth);
    }
}
