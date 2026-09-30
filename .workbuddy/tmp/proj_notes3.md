
## 生成窗口（SpawnWindow）设计勘察结论 — 仅讨论，未动代码

用户在讨论"挥刀后生成刀波实体"的实现方式，问是否要加一条逻辑轨用于指定生成帧与生成物。

### 加轨是架构预留过的
- `CombatActionAsset.MigrateLogicItemStableIds()` 注释原文："后续逻辑轨也必须追加到这个固定扫描顺序中" —— 作者写这条注释就是在为第三条轨留位置。
- `CombatLogicTimelineSource` 的轨是**硬编码固定轨**，`GetTrackTypes()` 返回空 ⇒ 用户不能自己加轨，只能代码加；加轨位置：构造函数 `tracks = { cancelTrack, hitTrack }`。
- 编辑器轨模式：每个窗口类型 2 个文件（Adapters = Track+Clip，Inspectors），`CombatLogicTimelineSource` 里加 Add/Remove/SetFrames/Commit 四组方法。现有 cancel 轨在 `CombatLogicTimelineSource.cs`（约 110 行），hit 轨在 `CombatHitWindowEditorAdapters.cs`（186 行）—— 已重复两次，加第三条是第三次。

### 加一条轨要同步改的位置（编译器管不到的那几处）
`CombatActionAsset`：① `ActionSpawnWindow` 类 ② `spawnWindows` 字段 ③ `ValidateData()` 的 foreach（漏 ⇒ 不 clamp，不报错）④ `MigrateLogicItemStableIds()` 的扫描（漏 ⇒ stableId 不稳定，不报错）⑤ `OnValidate()` 的 `??=`。
`CombatLogicTimelineSource`：① `tracks` 数组（漏 ⇒ 轨道不显示）② `CreateInspector` 分支 ③ `RefreshAfterUndo` 的 `RebuildAdapters`（漏 ⇒ undo 后不刷新）④ `SaveInternal` 无关但 `ValidateData` 要覆盖。

### spawnId 必须拆两层（与项目既有风格一致）
- 证据：`CombatActionAsset` 全体字段**零 UnityEngine.Object 引用**（纯标量 + 内嵌 [Serializable] 结构）；`ActionBuffApply` 用 `int buffId` 弱引用；`ActionHitWindow` 用内嵌 `shape` + `radiusMillimeters`。
- 所以 `ActionSpawnWindow { StartFrame, EndFrame, spawnId, ... }`，`spawnId` 弱引用到逻辑侧 `CombatSpawnProfile`；**视觉（模型/拖尾/爆炸）走表现映射表**，按同一 id 查。
- 若图省事直接在窗口里拖 prefab ⇒ 逻辑资产引用 Unity 资源 ⇒ `ICombatEntity` 注释里"同一套世界循环可以跑在战报校验、服务器重放这类无渲染环境"直接作废。

### 生成是"边沿"而现有缓冲是"区间"——语义冲突与现成解法
- `CombatFrameEventSet` 类注释："只放『按帧区间成立』的东西"；`ActionHitWindow.IsActive` / `ActionCancelWindow.IsOpen` 都是区间包含。
- **现成解法模式**：`HitboxSystem` 读区间窗口 → `Runner.TryAcceptHit(actionInstanceId, windowId, targetId)` 用 `HashSet<CombatHitKey>` 去重。⇒ "窗口是区间语义、去重是消费方责任"这个模式已立住，生成窗口照抄即可（去重键 = `(ActionInstanceId, WindowId)`）。

### 生成当帧能不能被遍历到（确凿结论）
- `EnsureOrder()` 只在 `Tick()` 开头 / `CaptureSnapshot` / `RestoreSnapshot` / `ComputeStateHash` 里调用 —— **TickFrame 内部任何阶段都不调**。
- `TickFrame` 里 `var count = _ordered.Length` 是本帧开头取的。
- ⇒ **Timeline 阶段注册的实体，本帧 `PhaseActions` / `PhasePresentation` / `OrderedEntities` 全看不到 → 下一帧才动、才显示。** 想当帧命中必须绕开遍历直接写 `PendingHits`；想当帧显示必须从 `EntityRegistered` 事件直接建对象。

### 白送的一块：投射物复用实体后命中判定几乎不用写
- `HitboxSystem` 的圆心 = `ToFixedPoint(entity.Position)`，**就是出招者自己的位置**。投射物若自己是 `ICombatEntity`（有 Position）+ 配一个"飞行"的 `CombatActionAsset`（hitWindows 覆盖全程），则飞行命中零成本，追踪 = 每帧改 Position/Rotation，伤害/增益/Death 全程复用。
- 代价 ①：`ICombatEntity` 要求 `Controller`（状态机 + 动作生命周期 + Runner 的完整组合）—— 偏重。代价 ②：`CollectTargets` 无护栏（`IsCombatActive && Life==Alive && Id>0` 全收）⇒ 投射物互相能打、会被刀光砍到，必须补"是否可被选中"标志。
- `CombatHitResolver.AppendResolvedHits` 的第一个参数是 `ActionRunner`（用于 TryAcceptHit 去重）⇒ 投射物要复用命中解析，就得有 ActionRunner。

### 阶段表（唯一事实来源，PhaseCount=8）
Commands(0) → Actions(1) → Timeline(2) → Hitbox(3) → Damage(4) → Buff(5) → Death(6) → Presentation(7)。
生成系统插在 Timeline 阶段（Order 排在 CombatTimelineSystem 之后），它读本帧 SpawnWindows、`world.Register(...)`、触发 `EntityRegistered`。

### 待用户定
1. 编辑器轨：先照抄第三条（快、风险低）还是先抽窗口轨基类（避免第三次重复，但要动已跑通的取消/命中两条轨）。
2. `spawnId` 指向：ScriptableObject 资产 vs 数值表项。
3. 生成物表现走不走 Timeline（影响 profile 里要不要 `TimelineAsset` 字段）。
