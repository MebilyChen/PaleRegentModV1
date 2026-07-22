# PaleRegentModV1 能量系统分析与解决方案

## 1. 为什么目前的实现会影响原版 Regent？

目前 PaleRegentModV1 使用了 `StarPatch.cs` 来拦截并替换战斗中能量计数器的贴图：
```csharp
[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
internal static class PaleRegentCounterTexturePatch
```

**问题出在两个地方：**

1. **资源冲突与类型转换异常 (InvalidCastException)**：
   在 `godot.log` 中我们看到了这个错误：
   ```
   System.InvalidCastException: Unable to cast object of type 'Godot.Control' to type 'MegaCrit.Sts2.Core.Nodes.Combat.NStarCounter'.
   ```
   以及：
   ```
   [BaseLib] Registered scene 'res://scenes/combat/energy_counters/regent_energy_counter.tscn' for auto-conversion to NEnergyCounter
   ```
   这是因为你在 `PaleRegentModV1/scenes/combat/paleregent_energy_counter.tscn` 或者某个地方，意外覆盖或错误引用了游戏原版的资源路径，或者你尝试把一个普通的 `Control` 节点强转成了 `NStarCounter`。
   在 `void_counter.tscn` 中，你把根节点的脚本设为了 `res://src/Core/Nodes/Combat/NStarCounter.cs`。但 `NStarCounter` 是游戏内置类，通过这种方式直接挂载 C# 脚本在 STS2 的 modding 环境中可能会导致类型映射失败，从而抛出 `InvalidCastException`。

2. **Patch 逻辑在所有角色上触发**：
   虽然你的 `StarPatch` 里写了：
   ```csharp
   if (player.Character is not PaleRegentCharacter) return;
   ```
   但因为前面的 `InvalidCastException` 导致 `NCombatUi._Ready` 或 `Activate` 方法抛出异常，这破坏了原版 Regent 甚至整个战斗的初始化流程。所以当你选原版 Regent 进战斗时，UI 初始化崩溃，导致游戏无法正常运行。

## 2. 灵魂-虚空 双能量系统设计方案

你的需求：
- **灵魂（能量）**：上限4，每回合恢复 `[灵魂上限 - 当前虚空]`。
- **虚空（星辉）**：无上限，跨回合保留，但在回合开始时会占用灵魂的恢复额度。
- **视觉**：复用 Regent 的能量 tscn，但只替换贴图。

### 方案：使用自定义 Secondary Resource

参考 `modstudy` 中大黄蜂（Hornet）的丝线（Silk）机制，STS2 官方和 RitsuLib 提供了更好的方式来添加副资源，而不是去强行 Patch 原版的 `NStarCounter`。

由于 `NStarCounter` 是硬编码给 Regent 用的，我们应该：
1. **取消使用 `NStarCounter`**。
2. **创建一个自定义的 Power 或 SecondaryResource** 来管理“虚空”。
3. **创建一个自定义的 UI 控件**（不需要挂载原版的 `NStarCounter.cs`，而是挂载自定义的 C# 脚本，或者直接由 C# 代码动态生成 UI）。
4. **修改能量恢复逻辑**：在回合开始时（`OnPlayerTurnStart` 或通过 Power 的 `AtStartOfTurn`），计算当前虚空层数，并修改玩家获得的能量。

### 具体实施步骤

1. **移除有问题的 `void_counter.tscn` 和 `StarPatch.cs`**。
   不再尝试替换原版 `NStarCounter`。

2. **启用 `CustomEnergyCounterPath`**。
   在 `PaleRegentModV1.cs` 中取消注释：
   ```csharp
   public override string CustomEnergyCounterPath => ModRoot + "/scenes/combat/paleregent_energy_counter.tscn";
   ```
   这样你的角色就会使用自己专属的能量计数器场景，**绝对不会影响原版 Regent**。

3. **创建“虚空”机制（Void Power）**。
   创建一个不可见的 Power（或者可见的 Buff），用来存储玩家当前的“虚空”层数。
   在回合开始时，这个 Power 会计算 `4 - 当前虚空层数`，并设置本回合的能量恢复量。

4. **实现能量扣除逻辑**。
   STS2 中，能量恢复通常由角色的 `EnergyPerTurn` 决定。我们可以通过 Patch `Player.RechargeEnergy` 或者在回合开始的事件中，扣除相应的能量。

---

5. **制作专属的 `paleregent_energy_counter.tscn`**。
   把原版 Regent 的能量计数器复制过来，**直接在 Godot 编辑器里把贴图换成你画的苍白之王贴图**。这样就不需要写复杂的 C# Patch 去动态替换贴图了，性能更好且不会出错。

我将把这些分析整理并汇报给你，并提供具体的代码实现。
