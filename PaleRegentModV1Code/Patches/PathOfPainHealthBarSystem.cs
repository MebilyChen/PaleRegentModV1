using System;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using PaleRegentModV1.PaleRegentModV1Code.Powers;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

/// <summary>
/// 自动寻找战斗中的 NHealthBar，并挂载苦痛之路警告图标。
///
/// PathOfPainPower 不再按 Amount 绘制血条段；
/// 只要能力存在，就在对应血条的正中央显示 ⚠️。
/// </summary>
public sealed partial class PathOfPainHealthBarSystem : Node
{
    private const string OverlayNodeName =
        "__PathOfPainHealthBarWarningOverlay";

    private SceneTree? _sceneTree;

    public override void _Ready()
    {
        _sceneTree = GetTree();
        _sceneTree.NodeAdded += OnNodeAdded;
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
        if (healthBar.GetChildren().Any(
            child => child.Name.ToString() == OverlayNodeName))
        {
            return;
        }

        healthBar.AddChild(new PathOfPainHealthBarOverlay
        {
            Name = OverlayNodeName
        });
    }
}

/// <summary>
/// 当 PathOfPainPower 存在时，在血条正中央显示 ⚠️。
/// 图标不根据 Amount 改变，也不占用任何血条预览段空间。
/// </summary>
public sealed partial class PathOfPainHealthBarOverlay : Control
{
    private const string WarningIcon = "⚠！";
    private const int WarningFontSize = 22;
    private const int WarningOutlineSize = 4;

    private static readonly Color WarningColor =
        new(1f, 0.78f, 0.12f, 1f);

    private NHealthBar? _healthBar;
    private Creature? _creature;
    private Control? _barArea;
    private Label? _warningLabel;
    private bool _resolved;

    public override void _Ready()
    {
        _healthBar = GetParent() as NHealthBar;
        MouseFilter = MouseFilterEnum.Ignore;

        CreateWarningLabel();
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (!_resolved)
        {
            if (!TryResolveReferences())
            {
                SetWarningVisible(false);
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

        bool shouldShow =
            _creature != null &&
            _creature.GetPowerAmount<PathOfPainPower>() > 0;

        SetWarningVisible(shouldShow);
    }

    private void CreateWarningLabel()
    {
        _warningLabel = new Label
        {
            Name = "__PathOfPainWarningIcon",
            Text = WarningIcon,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false
        };

        _warningLabel.AddThemeFontSizeOverride(
            "font_size",
            WarningFontSize
        );
        _warningLabel.AddThemeColorOverride(
            "font_color",
            WarningColor
        );
        _warningLabel.AddThemeColorOverride(
            "font_outline_color",
            Colors.Black
        );
        _warningLabel.AddThemeConstantOverride(
            "outline_size",
            WarningOutlineSize
        );

        AddChild(_warningLabel);
        _warningLabel.SetAnchorsAndOffsetsPreset(
            LayoutPreset.FullRect
        );
    }

    private bool TryResolveReferences()
    {
        if (_healthBar == null)
        {
            return false;
        }

        _creature ??= FindCreature(_healthBar);
        if (_creature == null)
        {
            return false;
        }

        // 优先使用中毒视觉的父容器，它通常就是实际血条区域。
        Control? poisonVisual = FindDescendantControl(
            _healthBar,
            control => control.Name.ToString().Contains(
                "poison",
                StringComparison.OrdinalIgnoreCase
            )
        );

        _barArea = poisonVisual?.GetParent() as Control;

        // 找不到中毒节点时，再按常见血条节点名称寻找。
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

        // 最终回退到整个 NHealthBar。
        _barArea ??= _healthBar as Control;
        if (_barArea == null)
        {
            return false;
        }

        if (GetParent() != _barArea)
        {
            Reparent(_barArea, keepGlobalTransform: false);
        }

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // 位于普通生命条、毒条及其他预览段上方。
        ZAsRelative = true;
        ZIndex = 100;

        _warningLabel?.SetAnchorsAndOffsetsPreset(
            LayoutPreset.FullRect
        );

        return true;
    }

    private void SetWarningVisible(bool visible)
    {
        if (_warningLabel != null)
        {
            _warningLabel.Visible = visible;
        }
    }

    private static Creature? FindCreature(Node start)
    {
        for (Node? current = start;
             current != null;
             current = current.GetParent())
        {
            Type type = current.GetType();

            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            {
                if (!typeof(Creature).IsAssignableFrom(
                    field.FieldType))
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
                    // 忽略无法读取的字段。
                }
            }

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            {
                if (!property.CanRead ||
                    property.GetIndexParameters().Length != 0 ||
                    !typeof(Creature).IsAssignableFrom(
                        property.PropertyType))
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
                    // 忽略要求特殊状态的 Getter。
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
