using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;

namespace PaleRegentModV1.PaleRegentModV1Code.Resources;

/// <summary>
/// 虚空（Void）副资源。基于 RitsuLib 的 SecondaryResource 框架注册。
/// 机制：虚空跨回合保留（Retain），回合开始时由 VoidPower 扣除等量灵魂（能量）。
/// </summary>
public static class VoidResource
{
    public static SecondaryResourceDefinition Definition { get; private set; } = null!;
    public static string Id { get; private set; } = string.Empty;

    public static void Register()
    {
        ModSecondaryResourceRegistry registry = RitsuLibFramework.GetSecondaryResourceRegistry("PaleRegentModV1");

        // 构造参数: defaultAmount, baseMaxAmount, minAmount, hardMaxAmount,
        //           turnStartPolicy, persistencePolicy, locTable, titleKey, descriptionKey,
        //           smallIconPath, largeIconPath
        SecondaryResourceDefinition def = new SecondaryResourceDefinition(
            0, null, 0, 9999,
            SecondaryResourceTurnStartPolicy.None,
            SecondaryResourcePersistencePolicy.Retain,
            null, null, null,
            "res://PaleRegentModV1/images/charui/energy_void.png",
            "res://PaleRegentModV1/images/charui/energy_void.png"
        );

        def.TitleKey = "PALEREGENTMODV1-VOID_COUNTER.title";
        def.DescriptionKey = "PALEREGENTMODV1-VOID_COUNTER.description";

        Definition = registry.Register("void", def);
        Id = Definition.Id;

        registry.AlwaysShowInCombatUiForCharacter<PaleRegentModV1.PaleRegentModV1Code.Character.PaleRegentModV1>(Definition.LocalId, -1000);

        registry.RegisterCombatUi<NSecondaryResourceCounter>("void_combat_counter", (NCombatUi parent) =>
        {
            SecondaryResourceCounterStyle style = new SecondaryResourceCounterStyle();
            style.FontSize = 30;
            style.OutlineSize = 12;
            style.OutlineColor = Colors.Black;
            style.FormatAmount = (int amount, int? max) => $"{amount}";

            // SecondaryResourceIconStyle 是 record 类型，没有公开的 Clone() 方法，
            // 也不能直接对 Default 的属性赋值（会改到全局默认值）。
            // 正确写法是用 C# 的 with 表达式创建一个修改过的副本。
            style.IconStyle = SecondaryResourceIconStyle.Default with
            {
                Size = new Vector2(100f, 100f)
            };

            NSecondaryResourceCounter counter = NSecondaryResourceCounter.Create(Definition, style);

            Control energyNode = parent.GetNode<Control>("%EnergyCounterContainer");
            counter.Position = energyNode.Position + new Vector2(150f, -50f);

            return counter;
        }, (ctx) =>
        {
            ctx.Node.Bind(ctx.Player, true);
        });
    }

    public static int Get(Player player) => SecondaryResourceCmd.Get(player, Id);
    public static async Task Gain(Player player, int amount) => await SecondaryResourceCmd.Gain(player, Id, amount, null);
    public static async Task Spend(Player player, int amount) => await SecondaryResourceCmd.Spend(player, Id, amount, null, null);
    public static async Task Lose(Player player, int amount) => await SecondaryResourceCmd.Lose(player, Id, amount, null);
}
