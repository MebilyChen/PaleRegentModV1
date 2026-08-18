using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using PaleRegentModV1.PaleRegentModV1Code.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
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

        // 仅在 PaleRegent 角色的战斗中常显；其他角色保持隐藏。
        registry.AlwaysShowInCombatUiForCharacter<
            PaleRegentModV1.PaleRegentModV1Code.Character.PaleRegentModV1>(
            Definition.LocalId,
            -1000);

        registry.RegisterCombatUi<NSecondaryResourceCounter>("void_combat_counter", (NCombatUi parent) =>
        {
            // record 的 init-only 属性必须在对象初始化器里一次性赋值
            SecondaryResourceCounterStyle style = new SecondaryResourceCounterStyle
            {
                // 根控件、图标和数字都使用同一尺寸，避免大图标从 48 像素根节点溢出。
                CounterSize = new Vector2(100f, 100f),
                IconSize = new Vector2(100f, 100f),
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

            // 固定锚定在战斗 UI 的左下角：不读取、不跟随任何星辉/能量节点。
            // 位置以左下角为基准；Y 为负值时向上移动。
            counter.AnchorLeft = 0f;
            counter.AnchorTop = 1f;
            counter.AnchorRight = 0f;
            counter.AnchorBottom = 1f;
            counter.OffsetLeft = 180f;
            counter.OffsetTop = -200f;
            counter.OffsetRight = 280f;
            counter.OffsetBottom = -100f;

            return counter;
        }, (ctx) =>
        {
            // Bind 会按 AlwaysShowInCombatUiForCharacter 的角色条件决定可见性。
            // 不要在这里把 Visible 强制设为 true，否则其他角色也会显示。
            ctx.Node.Bind(ctx.Player, true);
        });

        // ---- 卡牌费用显示 UI（修复：虚空费不显示在卡牌上）----
        // 之前只注册了战斗计数器（RegisterCombatUi），没有注册卡牌费用 UI，
        // 所以带虚空费的卡牌上看不到费用图标/数字。
        // 参考 .Register() 的 _cost 注册：
        // NSecondaryResourceCardCostUi 会自动读取卡牌的 SecondaryCosts 并渲染费用，
        // 没有虚空费的卡不会显示任何内容。
        registry.RegisterCardUi<NSecondaryResourceCardCostUi>("void_card_cost", (NCard parent) =>
        {
            // record 的 init-only 属性必须在对象初始化器里一次性赋值
            SecondaryResourceCardCostUiStyle style = new SecondaryResourceCardCostUiStyle
            {
                IconSize = new Vector2(48f, 48f),
                FontSize = 28,
                OutlineSize = 12,
                AffordableOutlineColor = Colors.Black,
                UnaffordableOutlineColor = Colors.Black
            };

            NSecondaryResourceCardCostUi node = NSecondaryResourceCardCostUi.Create(Id, style);

            // 把费用图标放在能量费用图标正下方
            TextureRect energyIcon = parent.GetNode<TextureRect>("%EnergyIcon");
            node.Position = energyIcon.Position + new Vector2(0f, 48f);

            return node;
        }, (ctx) =>
        {
            // 卡牌状态变化（抽到手牌/资源变化/费用修改）时刷新显示
            ctx.Node.Refresh<NCard>(ctx);
        });
    }

    public static int Get(Player player) => SecondaryResourceCmd.Get(player, Id);
    public static async Task Gain(Player player, int amount) => await SecondaryResourceCmd.Gain(player, Id, amount, null);
    public static async Task Spend(Player player, int amount) => await SecondaryResourceCmd.Spend(player, Id, amount, null, null);
    public static async Task Lose(Player player, int amount) => await SecondaryResourceCmd.Lose(player, Id, amount, null);

    /// <summary>
    /// 把 VoidPower（虚空 Buff 图标）的层数同步为当前虚空副资源数值。
    /// 任何“获得/消耗虚空”之后都应调用一次本方法，保证图标层数与真实资源一致。
    ///
    /// 设计说明：VoidPower 的层数只是“展示 + 回合开始扣灵魂的挂点”，
    /// 真正的结算数值一律以副资源（SecondaryResourceCmd）为准。
    /// 同步逻辑（三种情况）：
    ///   1. 资源为 0 且身上还有 Power → 移除 Power；
    ///   2. 资源 > 0 且身上没有 Power → 按当前资源量施加 Power；
    ///   3. 两者都有但数值不同 → 用 ModifyAmount 补差值。
    /// </summary>
    /// <param name="choiceContext">当前玩家选择上下文（命令系统需要）</param>
    /// <param name="player">目标玩家</param>
    /// <param name="source">来源卡牌（可空，用于日志/悬浮提示追踪）</param>
    public static async Task SyncPower(PlayerChoiceContext choiceContext, Player player, CardModel? source)
    {
        int resource = Get(player);
        VoidPower? power = player.Creature.GetPower<VoidPower>();

        if (resource <= 0 && power != null)
        {
            // 资源清零：移除图标
            await PowerCmd.Remove(power);
        }
        else if (power == null && resource > 0)
        {
            // 第一次获得虚空：挂上图标
            await PowerCmd.Apply<VoidPower>(choiceContext, player.Creature, resource, player.Creature, source);
        }
        else if (power != null && (int)power.Amount != resource)
        {
            // 数值不一致：补差值（可正可负）
            await PowerCmd.ModifyAmount(choiceContext, power, resource - (int)power.Amount, player.Creature, source);
        }
    }
}
