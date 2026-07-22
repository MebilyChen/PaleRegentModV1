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
/// 机制：虚空跨回合保留，回合开始时由 VoidPower 扣除等量灵魂（能量）。
///
/// 本次编译错误的修复点：
/// 1. SecondaryResourceDefinition / SecondaryResourceCounterStyle / SecondaryResourceIconStyle
///    都是 record 类型，属性全部是 init-only —— 只能在 new 的对象初始化器里赋值，
///    不能先 new 再逐行 style.Xxx = ... 赋值（CS8852 的原因）。
/// 2. SecondaryResourcePersistencePolicy 的枚举值只有 None / Combat / Run，
///    没有 Retain（CS0117 的原因）。"跨回合保留"不由它控制——
///    只要 TurnStartPolicy = None，资源在回合开始时就不会被清空/重置。
///    PersistencePolicy 管的是"是否写入存档"，这里用 None（战斗内资源即可）。
/// </summary>
public static class VoidResource
{
    public static SecondaryResourceDefinition Definition { get; private set; } = null!;
    public static string Id { get; private set; } = string.Empty;

    public static void Register()
    {
        ModSecondaryResourceRegistry registry = RitsuLibFramework.GetSecondaryResourceRegistry("PaleRegentModV1");

        // 构造函数参数全部有默认值，用命名参数只填需要的项。
        // TitleKey/DescriptionKey 是 init-only，必须通过构造参数传入，不能构造后再赋值。
        SecondaryResourceDefinition def = new SecondaryResourceDefinition(
            defaultAmount: 0,
            baseMaxAmount: null,
            minAmount: 0,
            hardMaxAmount: 9999,
            turnStartPolicy: SecondaryResourceTurnStartPolicy.None,   // 回合开始不清空 = 跨回合保留
            persistencePolicy: SecondaryResourcePersistencePolicy.None,
            locTable: "static_hover_tips",
            titleKey: "PALEREGENTMODV1-VOID_COUNTER.title",
            descriptionKey: "PALEREGENTMODV1-VOID_COUNTER.description",
            smallIconPath: "res://PaleRegentModV1/images/charui/energy_void.png",
            largeIconPath: "res://PaleRegentModV1/images/charui/energy_void.png"
        );

        Definition = registry.Register("void", def);
        Id = Definition.Id;

        registry.AlwaysShowInCombatUiForCharacter<PaleRegentModV1.PaleRegentModV1Code.Character.PaleRegentModV1>(Definition.LocalId, -1000);

        registry.RegisterCombatUi<NSecondaryResourceCounter>("void_combat_counter", (NCombatUi parent) =>
        {
            // record 的 init-only 属性必须在对象初始化器里一次性赋值
            SecondaryResourceCounterStyle style = new SecondaryResourceCounterStyle
            {
                FontSize = 30,
                OutlineSize = 12,
                OutlineColor = Colors.Black,
                FormatAmount = (int amount, int? max) => $"{amount}",
                // IconStyle 同为 record：用 with 表达式基于 Default 创建修改副本
                IconStyle = SecondaryResourceIconStyle.Default with
                {
                    Size = new Vector2(100f, 100f)
                }
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
