# UxGame 项目长期记忆

## 程序集分层（决定一个新类该放哪层）

- **三层单向依赖**：`Assembly-CSharp`（`Assets/Hotfix/`，**无 asmdef**）→ `Unity.HotfixBase`（`Assets/HotfixBase/Unity.HotfixBase.asmdef`）→ `Unity.Main`（`Assets/Main/Unity.Main.asmdef`）。
- **前两层都是热更 dll**：`ProjectSettings/HybridCLRSettings.asset` 里 `hotUpdateAssemblies = [Unity.HotfixBase, Assembly-CSharp]`，且 `hotUpdateAssemblyDefinitions` 是空的 —— Hotfix 层没有 asmdef，是直接按程序集名 `Assembly-CSharp` 登记进热更列表的。
- 方向靠 asmdef 的 `autoReferenced: true` 天然成立（默认程序集自动引用所有 asmdef）；**反过来不行** —— asmdef 无法引用 `Assembly-CSharp`。所以"HotfixBase 想用某个 Hotfix 里的类"是编译不过的，只能把接口提到 HotfixBase 做依赖倒置。
- **判断新类放哪**：看它是否碰 `Unit` / `GameObject` / `Animator` / `Transform` / `PathComponent` 这些 Hotfix 侧的东西。碰 → `Assets/Hotfix/`；不碰 → `Assets/HotfixBase/`。
- 实例：Combat 模块被切成两半 —— `HotfixBase/Manager/Combat/` 是纯逻辑（BattleWorld / CombatController / CombatStateMachine / CombatActionRunner / 阶段插件 / CombatMgr），`Hotfix/Common/Combat/CombatComponent.cs` 是唯一的桥接类。两者靠 `ICombatEntity` 倒置：**接口定义在 HotfixBase、实现在 Hotfix**。
- `Assets/Hotfix/Common/` 是 Unity 组件层，每个子目录一个组件：`AStar/` `FogOfWar/` `Playable/` `Camera` `Operate/` `Combat/`。
- 其它定位：`Entity` / `IAwakeSystem` 在 `HotfixBase/Base/ECS/Runtime/`；`SimulationClock` 在 `HotfixBase/Manager/Timeline/TimelineMgr.cs`；`Unit` 在 `Hotfix/Modules/Scene/Unit.cs`。

## 代码风格约定（2026-09-16 用户明确要求）

- **注释只写不变量和坑，不写推导过程。** 类/方法级默认 1-3 行摘要说"它做什么"。
- **不要写**：旧实现对比、"为什么这样设计"的推演、分步举例、`──` 分节标题、"①②③"编号清单。这些是一次性理解成本，用户读懂后就是纯噪声 —— 原话："太多注释反而影响我阅读代码了"。
- 值得留的只有两类：**顺序/时机依赖**（如"这一步必须在 X 之前"）和**易踩的坑**（如"这里必须清 xxx，否则连招第二下不伤害"）。
- 参考量级：清理后 Combat 模块整体约 10%。单文件超过 20% 基本就是写多了。
- **不要写"私有字段 + 转发属性"**（2026-09-21 用户要求把 Combat 模块全部改掉）：`public X Foo => _foo;` 直接写成 `public X Foo { get; private set; }`，省掉那个字段。能不能转看三条：字段是 `readonly` 的要用 `{ get; }`（写 `private set` 会丢掉 readonly 保证）；字段带 `[SerializeField]` 的不能转（会丢 Unity 序列化）；对外类型是只读接口、类内却要拿具体可变类型的（`ICombatEntity[]` / `readonly List<>` 暴露成 `IReadOnlyList<>`）也转不了 —— 只读接口没有可写索引器 / `Add` / `Clear`。另外 `=> _x.y`、`=> _x.Count`、索引器是计算/投影而不是转发，本来就没有 1:1 后置字段。

## 命名约定（2026-09-20 讨论结论）

- **类名前缀表示"属于哪个域"，不表示"操作什么对象"。** 这是判断命名协调性的标准。`UnitStateMachine` 的 `Unit` 表达操作对象、`CombatController` 的 `Combat` 表达域 —— 两者维度不一致，所以看起来别扭。
- `UnitStateMachine` **已于 2026-09-20 改名为 `CombatStateMachine`**（连带 `UnitStateMachineSnapshot` → `CombatStateMachineSnapshot`），与 `CombatStateId` / `CombatController` 成族，并与 Main 层通用状态机划清界限。改名技巧：直接对前缀 `UnitStateMachine` 做整体替换即可，`...Snapshot` 会自动跟着变。
- **反对 `CombatController` → `UnitController`**：它连 `Position` 都不持有（在 `ICombatEntity` 上），叫 Unit 会夸大能力边界，并把它从 `Combat*` 命名族里摘出去。
- `StateMachine` 这个词在项目里已归属 Main 层（`Assets/Main/Base/StateMachine/StateMachine.cs`，`GameStateMachine` / `PatchStateMachine` 继承它）；`Controller` 后缀偏表现层（`FairyGUI.Controller`、`UIController`）。
- 改名成本面（实测）：这两个类型只出现在 **13 个 C# 文件 + COMBAT_DESIGN.md + 2 个编辑器测试**，**零 Lua / 零场景 / 零预制体 / 零 JSON 字符串引用** → 纯编译期改名。连带项：`.cs` + `.cs.meta` 成对改名、`UnitStateMachineSnapshot`、`CombatController.States` 类型声明、文档、测试。
- **帧号的标识符统一是 `Simulation*`，不是 `Logic*`**（2026-09-20 确认）：`LogicFrame` / `LogicClock` 在全仓库（含 Lua / md / prefab）**一次都没出现过**。命名宿主是 Timeline 模块的 `SimulationClock`（+ `SimulationClockMode`，三种帧源 LocalRealtime / External / Replay），Combat 只是沿用同一个词，保证同一个 long 在 clock → world → 状态机 → 命令 → 快照 → state hash 全链路上只有一个名字。Combat 内部 `Logic` 占的是另外两个槽：`TickLogic(frame, …)`（动词，推进一步）和 `CombatLogicTimelineSource`（逻辑源 vs 表现源）。"逻辑帧"只是中文注释里的概念词，对应的标识符是 `SimulationFrame`。

## Combat 模块（`HotfixBase/Manager/Combat`）

- **目录按职责分四层**（2026-09-17 整理）：`Asset/`（策划资产）/ `Runtime/Core/`（世界与阶段框架）/ `Runtime/Unit/`（单位逻辑）/ `Runtime/Systems/`（阶段插件）/ `Runtime/Presentation/`（只读投影，只有 `CombatTimelinePlayer`）。命名空间统一是 `Ux`，**目录与命名空间无关，挪目录不改任何代码**。
- **Unity 侧桥接入口的持有关系**：`CombatComponent` 只有 **1 个真字段** `Controller`（`{ get; private set; }`）；真正 `new` 出 `StateMachine` / `ActionRunner` 的是 `CombatController`（两者都是 `{ get; } = new()`）。对象图是单向链 `CombatComponent → CombatController → {CombatStateMachine, CombatActionRunner}`，无环、无重复存储；后两者互不引用。`ICombatEntity` 上只暴露 `Controller`（`ICombatEntity.cs:21`），世界侧一律写 `entity.Controller.X`。**属性名已由用户改为 `StateMachine` / `ActionRunner`**（原名 `States` / `Actions`），`CombatComponent` 内部调用点已同步为 `Controller.StateMachine.X`。
- **曾有的两个转发属性已于 2026-09-20 删除**：`States => Controller?.States` / `Actions => Controller?.Actions` 是表达式属性（零存储），且**零外部调用者** —— 它们唯一的效果是让 `CombatComponent` 内部出现"两种取法混用"（原 `:387` 一行里既写 `Controller.IsMovementBlocked` 又写 `States.Locomotion`）。**现行规则：此文件内部一律 `Controller.X`**。
- **逻辑帧驱动**，Unity `Update` 不参与战斗逻辑。链路：`SimulationClock.FrameAdvanced` → `CombatMgr.OnFrameAdvanced` → `BattleWorld.Tick`（追赶循环，帧号不跳号）→ `TickFrame` 的 8 个阶段。
- 阶段顺序的唯一事实来源是 `BattleWorld.PhaseOrder` 静态数组（顺序 = 枚举值升序）；`TickFrame` 用 `switch` 只分发 3 个内置核心（Commands / Actions / Presentation），其余阶段全是插件位。新增阶段要同步 4 处：枚举成员、`PhaseCount`、`PhaseOrder`、`_systemBuckets` 的 `new()` 个数 —— **漏改不报编译错**，靠静态构造函数记 Error 日志兜底。
- 内置插件在 **`BattleWorld` 构造函数**里注册（Timeline → Hitbox → Damage → Buff → Death）。每个世界各持实例，别做成共享单例。
- 插件之间**不互相持有引用**，只通过 `BattleWorld` 上的交接缓冲通信：
  - `ActionActiveEntities`（阶段 2 写 → 阶段 3 读）——**唯一由生产者自己复位的**：`PhaseActions` 本来就在遍历全场，顺手收集 `HasAction` 的实体，Timeline 因此少扫一遍
  - `FrameEvents`（阶段 3 写 → 4/5/6 读）
  - `PendingHits`（阶段 4 写 → 5 读）
  后两个每帧开头由 `BattleWorld` 复位；生产者漏注册时下游读到空表，而不是上一帧脏数据。
- 逐帧遍历全场走 `BattleWorld.OrderedEntities`（`_ordered` 数组），**不要 `foreach world.Entities`** —— 后者静态类型是接口，foreach 会装箱 `SortedDictionary` 的结构化枚举器，每帧一个堆对象。该视图在 `Tick` 开头刷新，中途注册的单位下一帧才可见（与内置阶段口径一致）。
- 只需要"出招中的单位"就别再扫全场，用 `ActionActiveEntities`。**索引要建在已经付过遍历成本的地方**：`PhaseActions` 已经在遍历 `_ordered`，且它在 Timeline 阶段之前；起手/取消/结束都发生在 `TickLogic` 里，所以它返回时 `HasAction` 就是本帧权威事实。
- **不能把 Timeline 并进 Actions 阶段** —— 那会拆掉阶段屏障（"所有单位推进完状态，才开始求值"），退回单位优先那套顺序依赖。
- 输入分两类，别混：
  - 移动 `MoveInput` 是**当前值**（`PathComponent.MoveVector2`，可覆盖、无帧号、走状态同步 `UNIT_UPDATE_POSITION`）
  - 技能是**命令**（`CombatCommandBuffer`，按帧号分桶、取走即删；入队帧号 = 当前帧 + 1）
- **`StateLayer` 已从 4 层删到 3 层**（2026-09-21 落地，即此前提案的方案 A 删轴）：现在是 `Locomotion=0 / Control=1 / Life=2`，**选了连续重排而非空洞**（原值 Locomotion0 / Action1 / Control2 / Life3）。连带删除：`ActionState` 枚举、`CombatStateId` 的 4 处 Action 分支、`StateChangeReason.ActionStarted/ActionEnded`、`CombatStateMachine` 的 `SyncAction`/`SetAction`/`Action` 属性/`CaptureSnapshot` 的 Action 层、`CombatStateMachineSnapshot.Action`、`BattleWorld` 哈希里的 Action 层状态 id、`CombatEditorUtility` 的 Action 层标签与文案分支。`CombatEditorWindow`（6 处 `enumValueIndex`）与 `CombatTestProfiles.cs:57` **零改动** —— 这是连续重排的自洽理由：`enumValueIndex` 是"枚举名列表下标"，值从 0 连续时下标恰等于值。全仓源码 `StateLayer.Action|ActionState|SyncAction|ActionStarted|ActionEnded|UnitStateMachine` **零残留**。
- **连续重排在现有数据下无害**（扫过 main / origin / origin-main + 工作区全部 Combat 资产）：`layer` 只有 `0`（`HeroZSCombatProfile.asset` 2 处）、`stateId` 只有 1/2、StableId 只有 `state.0.1.default` / `state.0.2.default`（全仓 `state.[123].` 零命中）。残余风险只剩"别处未知的 `layer: 1/2` 会被静默解读成 Control/Life"，需靠代码注释挡（**当前没写这行注释**）。快照那条路径是**大声失败**（`RestoreSnapshot` 里 `value.Layer != layer` 会抛）。
- 技能不是状态：招式生命周期**只在** `CombatActionRunner`（`CurrentAsset` × `Current.ActionFrame` 是唯一的"配对"），状态机不再有动作层。`CombatActionRunner.ActionChanged`（`:107`）按**动作实例**发 —— 连招那一帧发**两条**（先 `Cancelled` 再 `Started`）；表现层读它（`CombatTimelinePlayer.cs:66` 用 `$"action:{actions.Current.InstanceId}"` 当 owner key，`Start` 里 `++_localSequence` 每次都变 → 强制重播动画）。`CombatStateMachine.StateChanged`（`:24`）按**层状态值**发，**至今零订阅者**。
- **删轴后的 `Tick` 只剩 3 步**（`CombatController.Tick`）：① `StateMachine.AdvanceTo` → ② `ActionRunner.Tick` → ③ Locomotion 三选一。删掉的是原来夹在 ②③ 之间的 `SyncAction` 镜像步。**动作影响状态机的唯一路径是 `ActionRunner.BlocksMovement` 进 `IsMovementBlocked`**，且影响落在 Locomotion 层（压成 Idle）而非任何动作层 —— 所以动作起手现在至多引发一条状态机边（`Locomotion: Move→Idle`，仅当该动作 `movementPolicy=Block` 且当时在走）。第 ② 步必须早于第 ③ 步的顺序不变量**未变**。
- **`(StateLayer)…enumValueIndex` 的坑仍然通用**（本次因连续重排未触发，将来再改枚举值时适用）：`CombatEditorWindow.cs:1016/1051/1061/1065/1118/1505` + `CombatTestProfiles.cs:57` —— **枚举名列表下标 ≠ 枚举值**，只有"值从 0 连续"时才巧合相等。无关项：`CombatEditorUtility.cs:281`（movementPolicy）、`CombatLogicTimelineSource.cs:402` / `CombatRuntimeTests.cs:906`（shape）。
- **`BuildStableId` 把 `(int)layer` 写进 StableId**（`CombatStatePresentation.cs:85-88`，`$"state.{layer}.{stateId}.{variant}"`），`CombatTimelinePlayer.cs:48/55/69` 的 owner key 也用 `(int)StateLayer` —— 改枚举值会影响这两处；现存 StableId 只引用 layer 0。删除提案与审核回执存档在 `.workbuddy/review/StateLayer-Action-removal-proposal.md`。
- **状态层的 `Frame` 是基础时间轴的权威播放头**，不是记账：`AdvanceTo` 只做 `StateLayerRuntime.Frame += delta`；`Advance` 在 `StateId == 0` 时跳过 —— 状态枚举都从 1 起，所以 `0` 唯一表示"未初始化/已清空"，这就是注释里"已存在状态"的含义。消费者只有 `CombatTimelineResolver.Resolve` 三处（Life `:48` / Control `:55` / Locomotion `:69`）→ `CombatTimelineSelection.Frame` → `CombatTimelinePlayer.Evaluate` → `TimelineComponent.EvaluateLayerFrame`（注释原文"按权威本地帧播放指定层"）→ `timeline.EvaluatePlaybackFrame(frame)`。
- **`AdvanceTo` 的顺序不变量**：必须排在同帧 `SetLocomotion` **之前**（`CombatController.Tick` 第 1 步）。因为 `ChangeState` 会 `runtime.Set(stateId, 0)` —— 只清计数器、**不记录 enterFrame**。这个顺序保证"本帧刚进入的状态，`Resolve` 读到 `Frame == 0`"，时间轴从第 0 帧采样；挪到 Tick 末尾则新状态当帧被老化成 1，开场少一帧姿态。同帧重复调用幂等。主循环 delta 恒为 1（`BattleWorld.Tick` 是 `while (Frame < frame) { Frame++; TickFrame(Frame); }`），所以 `Frame` 数值上等于 `SimulationFrame - enterFrame` —— 用累加器而非派生值是为了省一个字段。
- **零消费者的扩展点**（同类）：`CombatStateMachine.StateChanged` 零订阅者；`CombatStateMachine.IsPlaying(StateLayer, int)` 零调用（`AnimComponent.IsPlaying(string)` 是同名的另一回事）；`CombatFrameEventTable.TryFind` 死代码；取消窗口每帧求值但全项目零消费者。
- **`StateChangeReason` / `CombatActionEndReason` 是"标记型枚举"，只写入不读出**：`ChangeState` 收到 reason 后**完全不看它** —— 行为与 reason 无关，只装进 `StateChangedEvent`，而该事件零订阅者。默认参数编码了"通常是谁在改"：`SetLocomotion` 默认 `CodeRule`，`SetControl`/`SetLife` 默认 `ExternalRequest`。两个易记错的点：① **同状态重复 Set 时 reason 被丢弃**（`StateId` 相等就 `return false`，不发事件）；② `RestoreSnapshot` **不走 `ChangeState`**，自己直接 `Invoke`（唯一过滤是 `previous != StateId`）。文档注释声称"确定性调试 / 快照恢复 / 网络纠正"，这三个消费者**一个都不存在**。删 Action 层后只剩 4 个值（Initialize / CodeRule / ExternalRequest / SnapshotRestore）。位置特殊：它是 `Asset/` 下唯一单独成文件的状态枚举。
- 命中数据流：`CombatActionAsset.hitWindows` → 阶段 3 `CombatTimelineSystem` 求值成帧事件（帧区间在这一步被消耗，下游看不到 StartFrame/EndFrame）→ 阶段 4 `HitboxSystem` 只做几何 → `PendingHits` → 阶段 5 `CombatDamageSystem` 扣血/挂 Buff。
- 坐标约定：`HitboxSystem.MillimetersPerUnit = 1000f`（1 世界单位 = 1 米 = 1000 毫米）。`CombatFixedPoint` 是整数毫米；`Id <= 0` 会让 resolver 抛异常。
- 属性/增益是最小实现：`AttributeSet` 只有 HP/MaxHp，`CombatBuff` 只有身份/寿命/每帧扣血，都没有修改器栈（属 P1/P4）。
- `UnitCombatSnapshot.CurrentVersion` 已 1 → 2（加入 Attributes / Buffs）→ 3（删除 Action 层快照）。快照当前**无落盘、无网络传输**，所以升级不需兼容旧数据；`RestoreSnapshot` 会**拒绝**版本不一致。
- `CombatHitResolver.AppendResolvedHits` 的全参重载内部仍会 `new List<int>` + `new HashSet<long>`（每个攻击者每帧 2 次分配）。要消除得传复用 scratch，**未做**。
- `Log.Debug` **没有级别过滤**（`Debug(object)` → `Debug("{0}", msg)` → `UnityEngine.Debug.LogFormat`）：每次调用都分配 `params object[]` + 插值 string + Unity 抓堆栈，约 1-10μs —— 比一次全场遍历（约 0.5μs）还贵。`HitboxSystem` 的命中日志是**无条件**打的（不在 `Verbose` 判断里）。用户表示这些日志后面会删除。
- `CombatFrameEventTable.TryFind` **零调用方**（死代码）；`AppendCancelWindows` 每帧求值取消窗口但**全项目零消费者**（只有 Timeline 自己的 Verbose 日志用 `HasCancelWindows`）。
- 已知问题：A* 点击寻路失效 —— `SeekerComponent.OnPathComplete` 把整条 `vectorPath` 交给 `SetPoints`，而后者只取 `points[0]` 当方向向量（原路径跟随代码被注释）。

## 编辑器

- 技能配置入口是「技能双源时间轴」= 表现源（`profile.ActionPresentations[i].Timeline`，真 `TimelineAsset`）+ 逻辑源（`CombatLogicTimelineSource` 读写 `action.hitWindows` / `cancelWindows`）。资产必须先被某个 `CharacterCombatProfile` 引用才能打开。
- 资产关联键是 **`ActionId`**；clip 的 `stableId` 只用于命中去重与编辑器 diff。
- 战斗编辑器按**字段名**访问资产（`FindProperty`），所以给 `CombatActionAsset` 加字段是安全的。

## 工具坑

- 同一批工具调用里对**同一个文件**发多个 Edit 会互相覆盖（甚至报 success 但不落盘）。同文件多处修改必须逐次串行发。
- 校验"注释-only 改动"的可靠做法：对比前后版本时**只剥注释、保留字符串内容**，去空行后逐行比对。若把字符串也替换成占位符，日志文案被改错会被掩盖。
- 本沙箱**无法编译 C#**（`dotnet restore` 报 NuGet `path1` null，`csc` 被安全策略拦），所以所有改动只能做静态校验 + 剥注释比对，必须让用户在 Unity 里过一遍编译。
- **bash heredoc 会吃掉正则里的反斜杠**（Git Bash 路径转换）：`python - <<'PY'` 里的 `r'guid:\s*...'` 实测变成 `guid:/s*...`，`re.search` 静默返回 `None` —— 脚本不报错，但等于什么都没查。**含正则或反斜杠的 Python 一律先 Write 成 `.py` 再执行**。
- **Python 字符串里的转义序列会变成真字符**：往 md / 代码里追加"表示 CRLF 两字节的转义写法"时，直接写进普通字符串会被解释成真正的换行，把一行劈成两行 —— `MEMORY.md` 第 79 行就中过一次。要原样写入反斜杠，用 `chr(92)` 拼接，别依赖转义层数；写完用 `repr()` 单行回读确认。
- **本仓库行尾是混的，别假设 CRLF**：`Assets/HotfixBase/Manager/Combat/` 下既有纯 LF 也有纯 CRLF 的 .cs（2026-09-21 实测：10 个**未改动**的文件就是纯 LF）。`newline=''` 读出的 `\r\n` 直写会改变行尾 → 用 `git diff --stat` 自检：换行符若被翻转，diff 行数会等于文件总行数；只动几十行就说明没翻。**别用 `git show HEAD:<file>` 比行尾** —— `core.autocrlf=true` 时仓库里存的本来就是 LF，这个比法恒为 LF、测不出任何东西（我在这上面误报过一次"行尾回归"）。
- **Unity 挪文件必须 `.cs` + `.cs.meta` 成对移动**：meta 里的 GUID 决定场景/预制体/资产里 `m_Script` 引用是否断。移动后用 md5 比对可确认字节未变；新目录要补 `.meta`（照同目录现成目录 meta 抄，`folderAsset: yes`，行尾跟项目一致）。
