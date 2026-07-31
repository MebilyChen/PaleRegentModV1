using System;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 自动寻找战斗中的 NHealthBar，并为其挂载苦痛之路血条覆盖层。
///
/// 这个节点只需要在 Mod 初始化时添加一次。
/// </summary>
public sealed partial class PathOfPainHealthBarSystem : Node
{
    private const string OverlayNodeName = "__PathOfPainHealthBarOverlay";

    private SceneTree? _sceneTree;

    public override void _Ready()
    {
        _sceneTree = GetTree();

        // 处理之后新生成的血条。
        _sceneTree.NodeAdded += OnNodeAdded;

        // 处理当前场景中已经存在的血条。
        ScanExistingNodes(_sceneTree.Root);
    }

    public override void _ExitTree()
    {
        if (_sceneTree != null)
        {
            _sceneTree.NodeAdded -= OnNodeAdded;
        }

        _sceneTree = null;
    }

    private void OnNodeAdded(Node node)
    {
        if (node is NHealthBar healthBar)
        {
            AttachOverlay(healthBar);
        }
    }

    private void ScanExistingNodes(Node node)
    {
        if (node is NHealthBar healthBar)
        {
            AttachOverlay(healthBar);
        }

        foreach (Node child in node.GetChildren())
        {
            ScanExistingNodes(child);
        }
    }

    private static void AttachOverlay(NHealthBar healthBar)
    {
        /*
         * 防止重复挂载。
         *
         * 每个血条只允许存在一个苦痛覆盖层。
         */
        if (healthBar.GetChildren()
            .Any(child => child.Name.ToString() == OverlayNodeName))
        {
            return;
        }

        var overlay = new PathOfPainHealthBarOverlay
        {
            Name = OverlayNodeName
        };

        healthBar.AddChild(overlay);
    }
}

/// <summary>
/// 真正负责绘制黑色苦痛血条的节点。
///
/// 绘制顺序：
///
/// 生命条
/// 苦痛黑条
/// 原作中毒条
/// 血条边框和文字
///
/// 苦痛条会被放到中毒条之前，因此中毒依然可以正常覆盖在最右侧。
/// </summary>
public sealed partial class PathOfPainHealthBarOverlay : Control
{
    private static readonly Color PainColor =
        new(0.015f, 0.015f, 0.02f, 0.96f);

    /*
     * 黑条边缘用一条很细的暗紫色线。
     *
     * 这是为了避免纯黑区域看起来像“已经损失的生命”。
     * 不需要边缘的话，可以删掉 DrawLine。
     */
    private static readonly Color PainEdgeColor =
        new(0.42f, 0.27f, 0.53f, 0.95f);

    /*
     * 如果实际画面中血条左右有少量边框偏差，
     * 可以调整这两个值。
     *
     * 正常情况下保持 0 即可。
     */
    private const float LeftInset = 0f;
    private const float RightInset = 0f;

    private NHealthBar? _healthBar;
    private Creature? _creature;

    /// <summary>
    /// 中毒覆盖条节点。
    ///
    /// 我们会尽量从节点名称中自动寻找包含 poison 的 Control。
    /// </summary>
    private Control? _poisonVisual;

    /// <summary>
    /// 中毒覆盖条所在的容器。
    ///
    /// 苦痛黑条会被重新挂载到这个容器中，
    /// 从而使用和原作中毒条相同的裁切范围。
    /// </summary>
    private Control? _barArea;

    private bool _resolved;
    private bool _shouldDraw;

    private Rect2 _painRect;
    private float _painPoisonBoundaryX;

    public override void _Ready()
    {
        _healthBar = GetParent() as NHealthBar;

        MouseFilter = MouseFilterEnum.Ignore;

        /*
         * 由于 Overlay 可能在血条自己的子节点创建完成之前被加入，
         * 所以不在这里强制解析，而是在 _Process 中持续尝试。
         */
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (!_resolved)
        {
            if (!TryResolveReferences())
            {
                return;
            }

            _resolved = true;
        }

        if (_healthBar == null ||
            !GodotObject.IsInstanceValid(_healthBar) ||
            _barArea == null ||
            !GodotObject.IsInstanceValid(_barArea))
        {
            QueueFree();
            return;
        }

        if (_creature == null)
        {
            _shouldDraw = false;
            QueueRedraw();
            return;
        }

        UpdatePainRect();
    }

    private bool TryResolveReferences()
    {
        if (_healthBar == null)
        {
            return false;
        }

        /*
         * NHealthBar 关联的 Creature 在不同游戏版本中可能使用
         * 不同的私有字段或属性名称。
         *
         * 这里不硬编码诸如 "_creature"、"Creature" 等字段名，
         * 而是按字段/属性类型寻找 Creature。
         */
        _creature ??= FindCreature(_healthBar);

        if (_creature == null)
        {
            return false;
        }

        /*
         * 优先寻找原作中毒条。
         *
         * 节点名称一般不会受到语言设置影响，
         * 因此按 "poison" 搜索比按显示文本搜索稳定。
         */
        _poisonVisual ??= FindDescendantControl(
            _healthBar,
            control => control.Name
                .ToString()
                .Contains("poison", StringComparison.OrdinalIgnoreCase)
        );

        /*
         * 最理想的情况是把黑条加到中毒条的父节点中。
         *
         * 这样：
         * - 黑条和毒条使用同一坐标系；
         * - 黑条会受到相同的血条裁切；
         * - 不容易覆盖到生命数字、格挡数字等 UI。
         */
        _barArea = _poisonVisual?.GetParent() as Control;

        /*
         * 如果当前版本无法通过名字找到中毒节点，
         * 尝试寻找名称中包含 health/hp/bar 的 Control。
         */
        _barArea ??= FindDescendantControl(
            _healthBar,
            control =>
            {
                string name = control.Name.ToString();

                return name.Contains(
                           "health",
                           StringComparison.OrdinalIgnoreCase
                       ) ||
                       name.Contains(
                           "hp",
                           StringComparison.OrdinalIgnoreCase
                       ) ||
                       name.Contains(
                           "bar",
                           StringComparison.OrdinalIgnoreCase
                       );
            }
        );

        if (_barArea == null)
        {
            return false;
        }

        /*
         * 将自己移动到血条绘制区域中。
         */
        if (GetParent() != _barArea)
        {
            Reparent(_barArea, keepGlobalTransform: false);

            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        }

        /*
         * 保证黑条在原作中毒条之前绘制：
         *
         * 生命条 -> 黑条 -> 毒条
         *
         * 同一 ZIndex 下，后面的子节点会覆盖前面的子节点。
         */
        if (_poisonVisual != null &&
            _poisonVisual.GetParent() == _barArea)
        {
            ZIndex = _poisonVisual.ZIndex;
            ZAsRelative = _poisonVisual.ZAsRelative;

            int poisonIndex = _poisonVisual.GetIndex();

            if (GetIndex() != poisonIndex)
            {
                _barArea.MoveChild(this, poisonIndex);
            }
        }

        return true;
    }

    private void UpdatePainRect()
    {
        if (_creature == null || _barArea == null)
        {
            HidePain();
            return;
        }

        int painAmount =
            _creature.GetPowerAmount<PathOfPainPower>();

        if (painAmount <= 0)
        {
            HidePain();
            return;
        }

        int currentHp = Math.Max(0, _creature.CurrentHp);
        int maxHp = Math.Max(1, _creature.MaxHp);

        if (currentHp <= 0)
        {
            HidePain();
            return;
        }

        /*
         * 血条可绘制区域。
         *
         * Y 和高度尽可能沿用原作毒条，
         * X 和宽度使用毒条父容器的完整宽度。
         */
        float trackX = LeftInset;
        float trackWidth = Math.Max(
            0f,
            _barArea.Size.X - LeftInset - RightInset
        );

        float trackY = 0f;
        float trackHeight = _barArea.Size.Y;

        if (_poisonVisual != null)
        {
            /*
             * 中毒条一般和生命条拥有完全相同的高度。
             */
            if (_poisonVisual.Size.Y > 0.5f)
            {
                trackY = _poisonVisual.Position.Y;
                trackHeight = _poisonVisual.Size.Y;
            }
        }

        if (trackWidth <= 0f || trackHeight <= 0f)
        {
            HidePain();
            return;
        }

        /*
         * 每一点生命在血条上占用的像素宽度。
         *
         * 必须除以 MaxHp，而不是 CurrentHp。
         *
         * 否则损失生命之后，剩余生命的单位宽度会被错误放大。
         */
        float pixelsPerHp = trackWidth / maxHp;

        int clampedCurrentHp = Math.Min(currentHp, maxHp);

        float currentHpWidth = clampedCurrentHp * pixelsPerHp;
        float currentHpRight = trackX + currentHpWidth;

        int poisonAmount = Math.Max(
            0,
            _creature.GetPowerAmount<PoisonPower>()
        );

        /*
         * 优先读取原作中毒节点当前实际显示的像素宽度。
         *
         * 如果原作使用普通 Control 改变 Size.X，
         * 这里可以自动兼容中毒伤害修正和动画。
         *
         * 如果原作使用 Shader 或全尺寸 TextureProgressBar，
         * 无法直接从 Size.X 得知宽度，则回退到 PoisonPower 层数。
         */
        float poisonWidth = ResolvePoisonWidth(
            poisonAmount,
            currentHpWidth,
            pixelsPerHp,
            trackWidth
        );

        poisonWidth = Math.Clamp(
            poisonWidth,
            0f,
            currentHpWidth
        );

        /*
         * 中毒占据当前生命右端。
         *
         * 所以苦痛可使用的区域是：
         *
         * 当前生命宽度 - 中毒宽度
         */
        float availablePainWidth = Math.Max(
            0f,
            currentHpWidth - poisonWidth
        );

        float requestedPainWidth =
            Math.Max(0, painAmount) * pixelsPerHp;

        float painWidth = Math.Min(
            requestedPainWidth,
            availablePainWidth
        );

        if (painWidth <= 0.25f)
        {
            HidePain();
            return;
        }

        /*
         * 苦痛右边界紧贴中毒左边界。
         */
        float painRight = currentHpRight - poisonWidth;
        float painLeft = painRight - painWidth;

        _painRect = new Rect2(
            new Vector2(painLeft, trackY),
            new Vector2(painWidth, trackHeight)
        );

        _painPoisonBoundaryX = painRight;
        _shouldDraw = true;

        QueueRedraw();
    }

    private float ResolvePoisonWidth(
        int poisonAmount,
        float currentHpWidth,
        float pixelsPerHp,
        float fullTrackWidth)
    {
        if (poisonAmount <= 0)
        {
            return 0f;
        }

        if (_poisonVisual != null &&
            _poisonVisual.Visible)
        {
            float renderedWidth = _poisonVisual.Size.X;

            /*
             * 如果毒条节点本身的宽度明显小于完整血条，
             * 通常说明原作正在直接调整该节点的 Size.X。
             *
             * 这种情况下直接采用实际像素宽度。
             */
            bool looksLikeSegment =
                renderedWidth > 0.5f &&
                renderedWidth < fullTrackWidth - 0.5f;

            if (looksLikeSegment)
            {
                return Math.Min(
                    renderedWidth,
                    currentHpWidth
                );
            }
        }

        /*
         * 回退方案：
         *
         * 使用基础 PoisonPower 层数计算毒条宽度。
         */
        return Math.Min(
            poisonAmount * pixelsPerHp,
            currentHpWidth
        );
    }

    private void HidePain()
    {
        if (!_shouldDraw)
        {
            return;
        }

        _shouldDraw = false;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_shouldDraw)
        {
            return;
        }

        /*
         * 黑色主体。
         */
        DrawRect(
            _painRect,
            PainColor,
            filled: true
        );

        /*
         * 在苦痛和中毒的交界处画一条细线。
         *
         * 线向苦痛区域偏移 1 像素，
         * 避免被后绘制的中毒条完全盖住。
         */
        float separatorX = _painPoisonBoundaryX - 1f;

        DrawLine(
            new Vector2(separatorX, _painRect.Position.Y),
            new Vector2(separatorX, _painRect.End.Y),
            PainEdgeColor,
            width: 2f,
            antialiased: false
        );

        /*
         * 左边也增加一条很暗的轮廓，
         * 让黑条与正常红色生命更容易区分。
         */
        DrawLine(
            new Vector2(_painRect.Position.X, _painRect.Position.Y),
            new Vector2(_painRect.Position.X, _painRect.End.Y),
            PainEdgeColor,
            width: 1f,
            antialiased: false
        );
    }

    /// <summary>
    /// 从 NHealthBar 或其父节点的字段/属性中寻找 Creature。
    ///
    /// 只会在初始化阶段执行，不会每帧反射。
    /// </summary>
    private static Creature? FindCreature(Node start)
    {
        for (Node? current = start;
             current != null;
             current = current.GetParent())
        {
            Type type = current.GetType();

            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            foreach (FieldInfo field in fields)
            {
                if (!typeof(Creature)
                    .IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                try
                {
                    if (field.GetValue(current) is Creature creature)
                    {
                        return creature;
                    }
                }
                catch
                {
                    // 忽略无法读取的字段，继续寻找。
                }
            }

            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanRead ||
                    property.GetIndexParameters().Length != 0 ||
                    !typeof(Creature)
                        .IsAssignableFrom(property.PropertyType))
                {
                    continue;
                }

                try
                {
                    if (property.GetValue(current) is Creature creature)
                    {
                        return creature;
                    }
                }
                catch
                {
                    // 某些属性 Getter 可能要求特定状态，忽略即可。
                }
            }
        }

        return null;
    }

    private static Control? FindDescendantControl(
        Node root,
        Func<Control, bool> predicate)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is Control control &&
                predicate(control))
            {
                return control;
            }

            Control? nested =
                FindDescendantControl(child, predicate);

            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
