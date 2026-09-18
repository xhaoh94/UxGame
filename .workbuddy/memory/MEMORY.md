# UxGame 项目长期记忆

## 代码风格约定（2026-09-16 用户明确要求）

- **注释只写不变量和坑，不写推导过程。** 类/方法级默认 1-3 行摘要说"它做什么"。
- **不要写**：旧实现对比、"为什么这样设计"的推演、分步举例、`──` 分节标题、"①②③"编号清单。这些是一次性理解成本，用户读懂后就是纯噪声 —— 原话："太多注释反而影响我阅读代码了"。
- 值得留的只有两类：**顺序/时机依赖**（如"这一步必须在 X 之前"）和**易踩的坑**（如"这里必须清 xxx，否则连招第二下不伤害"）。
- 参考量级：清理后 Combat 模块整体约 10%。单文件超过 20% 基本就是写多了。

## Combat 模块（`HotfixBase/Manager/Combat`）

- **目录按职责分四层**（2026-09-17 整理）：`Asset/`（策划资产）/ `Runtime/Core/`（世界与阶段框架）/ `Runtime/Unit/`（单位逻辑）/ `Runtime/Systems/`（阶段插件）/ `Runtime/Presentation/`（只读投影，只有 `CombatTimelinePlayer`）。命名空间统一是 `Ux`，**目录与命名空间无关，挪目录不改任何代码**。
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
- 技能不是状态：`StateLayer.Action` 只有 `Free / Executing`；招式生命周期在 `CombatActionRunner`（`CurrentAsset` × `Current.ActionFrame` 是唯一的"配对"）。
- 命中数据流：`CombatActionAsset.hitWindows` → 阶段 3 `CombatTimelineSystem` 求值成帧事件（帧区间在这一步被消耗，下游看不到 StartFrame/EndFrame）→ 阶段 4 `HitboxSystem` 只做几何 → `PendingHits` → 阶段 5 `CombatDamageSystem` 扣血/挂 Buff。
- 坐标约定：`HitboxSystem.MillimetersPerUnit = 1000f`（1 世界单位 = 1 米 = 1000 毫米）。`CombatFixedPoint` 是整数毫米；`Id <= 0` 会让 resolver 抛异常。
- 属性/增益是最小实现：`AttributeSet` 只有 HP/MaxHp，`CombatBuff` 只有身份/寿命/每帧扣血，都没有修改器栈（属 P1/P4）。
- `UnitCombatSnapshot.CurrentVersion` 已 1 → 2（加入 Attributes / Buffs）。快照当前**无落盘、无网络传输**，所以升级不需兼容旧数据。
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
- **写入 CRLF 文件前先测行尾**：本仓库是 CRLF，用 `newline=''` 读出后新插入的 `\n` 段落会混进 bare LF，写完要统计 `\r\n` 数是否等于 `\n` 总数。
- **Unity 挪文件必须 `.cs` + `.cs.meta` 成对移动**：meta 里的 GUID 决定场景/预制体/资产里 `m_Script` 引用是否断。移动后用 md5 比对可确认字节未变；新目录要补 `.meta`（照同目录现成目录 meta 抄，`folderAsset: yes`，行尾跟项目一致）。
