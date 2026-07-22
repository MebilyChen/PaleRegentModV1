# PaleRegentModV1 双能量系统实现方案 (基于 RitsuLib)

通过分析 modstudy，我发现它的“丝线(Silk)”双能量系统并不是通过原版 Regent 的 `NStarCounter` 实现的，而是**完全依赖于 `STS2-RitsuLib` 提供的 `SecondaryResource` 框架**。

## 1. 为什么你目前的做法会报错？

你目前在 `void_counter.tscn` 中挂载了 `res://src/Core/Nodes/Combat/NStarCounter.cs`。
但在 Godot 和 STS2 的 Mod 环境下，原版的 C# 类（特别是带有 UI 和依赖注入的类）不能直接被外部 tscn 随意挂载，否则会引发类型转换异常（`InvalidCastException`）和空引用异常（`NullReferenceException`）。
这就导致了你的角色和原版 Regent 在进入战斗时，因为 UI 初始化失败而崩溃。

## 2. modstudy 是怎么做的？

modstudy 引入了 `STS2-RitsuLib`，这是社区维护的一个扩展库，专门提供了“副资源（Secondary Resource）”的 API。

1. **注册资源**：在 `SilkResource.cs` 中，使用 `RitsuLibFramework.GetSecondaryResourceRegistry` 注册了一个名为 "silk" 的副资源，定义了它的上限、保留策略（跨回合保留）、以及 UI 样式。
2. **UI 生成**：RitsuLib 会自动根据 `SecondaryResourceDefinition` 生成计数器 UI，或者允许你自定义 UI 控件并将其挂载到战斗界面上。
3. **能量控制**：通过监听回合开始或结束的事件（如 `SilkFreeListener`），动态调整卡牌的费用或能量的恢复。

## 3. 我们的解决方案

为了实现“灵魂-虚空”双能量机制（虚空跨回合保留，但在回合开始时占用灵魂的恢复额度），我们需要：

### 步骤一：添加 RitsuLib 依赖
你的项目需要引用 `STS2-RitsuLib`。你需要在 `PaleRegentModV1.csproj` 中添加：
```xml
<PackageReference Include="Ritsukage.Sts2.RitsuLib" Version="*" />
```
*(注：包名可能需要根据实际的 NuGet 源调整，通常是 `STS2-RitsuLib`)*

### 步骤二：创建 `VoidResource.cs`
我们需要创建一个类来向 RitsuLib 注册“虚空(Void)”副资源。

```csharp
using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;
using PaleRegentModV1.PaleRegentModV1Code.Character;

namespace PaleRegentModV1.PaleRegentModV1Code.Resources;

public static class VoidResource
{
    public static SecondaryResourceDefinition Definition { get; private set; } = null!;
    public static string Id { get; private set; } = string.Empty;

    public static void Register()
    {
        ModSecondaryResourceRegistry registry = RitsuLibFramework.GetSecondaryResourceRegistry("PaleRegentModV1");
        
        // 定义虚空资源：初始0，上限999，跨回合保留(PersistencePolicy.Retain)
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
        
        // 只在苍白之王角色上显示
        registry.AlwaysShowInCombatUiForCharacter<PaleRegentModV1.PaleRegentModV1Code.Character.PaleRegentModV1>(Definition.LocalId, -1000);
        
        // 注册战斗 UI
        registry.RegisterCombatUi<NSecondaryResourceCounter>("void_combat_counter", (NCombatUi parent) =>
        {
            SecondaryResourceCounterStyle style = new SecondaryResourceCounterStyle();
            style.FontSize = 30;
            style.OutlineSize = 12;
            style.OutlineColor = Colors.Black;
            style.FormatAmount = (int amount, int? max) => $"{amount}"; // 虚空无上限，只显示当前值
            
            SecondaryResourceIconStyle iconStyle = SecondaryResourceIconStyle.Default.Clone();
            iconStyle.Size = new Vector2(100f, 100f); // 根据你的贴图调整
            style.IconStyle = iconStyle;
            
            NSecondaryResourceCounter counter = NSecondaryResourceCounter.Create(Definition, style);
            
            // 定位到原版能量计数器旁边
            Control energyNode = parent.GetNode<Control>("%EnergyCounterContainer");
            counter.Position = energyNode.Position + new Vector2(150f, -50f); 
            
            return counter;
        }, (ctx) =>
        {
            ctx.Node.Bind(ctx.Player, true);
        });
    }

    // 辅助方法
    public static int Get(Player player) => SecondaryResourceCmd.Get(player, Id);
    public static async Task Gain(Player player, int amount) => await SecondaryResourceCmd.Gain(player, Id, amount, null);
    public static async Task Spend(Player player, int amount) => await SecondaryResourceCmd.Spend(player, Id, amount, null, null);
}
```

### 步骤三：实现回合开始扣除灵魂的逻辑

因为虚空会在回合开始时扣除灵魂（基础能量），我们可以通过监听 RitsuLib 或 Harmony Patch `Player.RechargeEnergy`，或者更简单地，注册一个不可见的 `VoidEnergyManagerPower`（类似 `SilkFreeListener`）。

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.RechargeEnergy))]
internal static class VoidEnergyPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance.Character is Character.PaleRegentModV1)
        {
            int currentVoid = Resources.VoidResource.Get(__instance);
            if (currentVoid > 0)
            {
                // 扣除相应灵魂(能量)
                __instance.Energy -= currentVoid;
                if (__instance.Energy < 0)
                {
                    __instance.Energy = 0;
                }
            }
        }
    }
}
```

### 步骤四：清理和隔离原版 Regent

1. **删除 `void_counter.tscn`** 和 `StarPatch.cs`。它们不仅引发报错，而且在使用 RitsuLib 后已经不需要了。
2. **启用 `CustomEnergyCounterPath`**：
   在 `PaleRegentModV1.cs` 中取消注释 `CustomEnergyCounterPath`，并指向 `paleregent_energy_counter.tscn`。
3. **在 `paleregent_energy_counter.tscn` 中直接修改贴图**，不再依赖代码动态替换。
4. **在 `MainFile.cs` 中调用 `VoidResource.Register()`**。

这样，你就拥有了一个完全独立的“虚空”副资源系统，且完美隔离了原版 Regent！
