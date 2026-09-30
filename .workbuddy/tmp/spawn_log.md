
## 生成窗口配置侧落地（批次 K：抽基类 + 加第三条逻辑轨）

用户拍板三件事：抽基类按最好方式做；生成物先用 Unity 资产（无配置表）；生成物自身表现走不走 Timeline 待定（我给了大厂做法：事件驱动播发，不接 Timeline）。

### 运行时侧：抽 `CombatLogicWindow`
- 新建 `HotfixBase/Manager/Combat/Asset/CombatLogicWindow.cs`：`stableId` / `StartFrame` / `EndFrame` / `ContainsFrame` / `ValidateData` / `RegenerateStableId`。
- `ActionCancelWindow` / `ActionHitWindow` 改为继承它，各自的业务字段留在子类（`ValidateData` 用 `override` 调 `base`）。
- **YAML 字段名不变**（继承字段在 Unity 序列化里是展平的），旧资产直接兼容。
- 新增 `ActionSpawnWindow : CombatLogicWindow`，带 `spawnProfile` 引用；`CombatActionAsset` 加 `spawnWindows` 列表。
- 新增 `CombatSpawnProfile`（纯逻辑：spawnId / flightAction / 速度 / `SpawnHomingMode` / 最大寿命）与 `CombatSpawnPresentation`（表现映射挂 `CharacterCombatProfile`，**以资产引用做键**，与 `CombatActionPresentation` 同构）。

### 编辑器侧：抽轨基类
- 新建 `Editor/Combat/Timeline/CombatWindowEditorBase.cs`：
  - `ICombatLogicTimelineInspectorSource`（`Owner` + `CreateInspector()`）—— Source 的 inspector 分派从 8 分支 switch 缩成两行，**数据源不再认识任何具体轨类型**。
  - `CombatWindowEditorTrackBase<TWindow, TClip>`：帧区间校验 / `RebuildAdapters` / `MakeClip` / 增删窗口（`SerializedProperty` + `WriteNewWindowFields` 钩子）/ `CommitStructureChange`。
  - `CombatWindowEditorClipBase<TWindow>`：`Drag`（三态统一算法）/ `SetFrames`（带早退）/ `ApplyWindowFrames`（统一夹取）/ `BeginDrag` / `CommitEdit`。
- 取消轨从 `CombatLogicTimelineSource.cs` 移到 `CombatCancelWindowEditorAdapters.cs`；命中轨改继承；新增生成轨。
- `CombatLogicTimelineSource.cs` 瘦身成纯数据源（~250 行）：新增 `NotifyStructureChanged` / `NotifyChanged` / `FindCancelWindow` / `FindHitWindow` / `FindSpawnWindow`，删除全部窗口专用方法。

### 行为等价性（逐条核对既有测试的精确断言）
- `saveCount == 4`（2 次 CreateClip + 2 次 SetFrames）—— 保持 ✓
- `clip.SetFrames(-5,100)` → (0,30)；`Drag(Right,0,30)` → (0,1)；`SetFrames(29,30)` + `Drag(Move,40,29)` → (29,30) —— 三态拖拽算法等价（cancel 的 `Clamp(start+delta)` 与 hit 的边界修正结果一致）✓
- undo key **各自保留**（配置成虚属性），不统一，避免无谓行为变更。
- 唯一的行为提升：`SetFrames` 统一加了"值没变就早退"（原本只有 hit 轨有）。

### 测试同步（三处硬约束，漏改会红）
1. `document.TrackCount` 断言 3 → **4**（1 表现 + 3 逻辑）。
2. `CoreViewsDoNotReachThroughTimelineWindowAsset` 契约测试补 `CombatSpawnWindowEditorTrack` / `CombatSpawnWindowEditorClip` 两个类型名。
3. 新轨**必须追加到 `tracks` 末尾** —— 测试按 `Tracks[0]`/`[1]` 取轨，插中间会静默改含义。
新增 3 个测试：`LogicSourceEditsSpawnWindowsWithProfileReference` / `SpawnWindowClampsRangeAndSharesItemIdDomain` / `ProfileValidationRejectsSpawnWindowWithoutSpawnProfile`。

### 校验
- `.workbuddy/tmp/verify_spawn.py`：13 个文件括号配平（剥注释 **+ 剥字符串字面量**）/ 14 个新类型定义 / 加轨 5 处与映射 5 处同步 / 三子类的 8 个抽象成员 / Source 无具体类型 switch / 测试同步 —— ALL OK。
- **校验脚本自身的坑**：只剥注释不够。`"[0,10) 与 [10,20) 仅接触"` 这类消息里的 `[` 会让括号计数假报警（实测 2 个文件 FAIL）。必须连字符串字面量一起剥：`"(?:\\.|[^"\\])*"` → `""`。
- 行尾：`git ls-files --eol` 逐个核对，Edit/Write **保留了每个文件原有的工作区行尾**（CRLF 仍是 CRLF，LF 仍是 LF），无回归。

### 待做（批次 L）
生成系统的运行时消费（阶段 Timeline 求值 → 注册实体 → `EntityRegistered` 通知表现层）、投射物实体、`CollectTargets` 的"可被选中"护栏、编辑器可视化沙盒。
