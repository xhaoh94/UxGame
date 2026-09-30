import io, os, shutil, sys

MEM = r'C:\Project\UxGame\.workbuddy\memory\MEMORY.md'
BK  = r'C:\Project\UxGame\.workbuddy\tmp\backup_MEMORY.md'

with io.open(MEM, 'r', encoding='utf-8', newline='') as f:
    old = f.read()
crlf = old.count('\r\n'); lf = old.count('\n')
print('OLD bytes=%d chars=%d LF=%d CRLF=%d' % (len(old.encode('utf-8')), len(old), lf, crlf))

if not os.path.exists(BK):
    shutil.copy2(MEM, BK)
    print('backup -> %s' % BK)

NEW = u'''# UxGame 项目长期记忆

过程细节见同目录 `YYYY-MM-DD.md` 日志；本文件只留跨会话有效的规则与坑。

## 分层与放置
- 单向三层：`Assembly-CSharp`（`Assets/Hotfix/`，**无 asmdef**，按程序集名登记进热更列表）→ `Unity.HotfixBase` → `Unity.Main`。前两层都是热更 dll（`ProjectSettings/HybridCLRSettings.asset`）。
- asmdef 引用不了 `Assembly-CSharp` → HotfixBase 要用 Hotfix 的类，只能把接口提到 HotfixBase 做依赖倒置（Combat 即如此：`ICombatEntity` 定义在 HotfixBase、实现在 Hotfix）。
- 新类放哪：碰 `Unit`/`GameObject`/`Animator`/`Transform`/`PathComponent` → `Assets/Hotfix/`；不碰 → `Assets/HotfixBase/`。
- `Entity`/`IAwakeSystem` 在 `HotfixBase/Base/ECS/Runtime/`；`SimulationClock` 在 `HotfixBase/Manager/Timeline/TimelineMgr.cs`；`Unit` 在 `Hotfix/Modules/Scene/Unit.cs`。

## 代码风格（用户明确要求）
- **注释只写不变量和坑，不写推导过程**：不写旧实现对比、设计推演、分步举例、`──` 分节标题、①②③清单；类/方法级 1-3 行说"它做什么"。原话"太多注释反而影响我阅读代码了"。
- **不写"私有字段 + 转发属性"**：`public X Foo => _foo;` 一律 `public X Foo { get; private set; }`。转不了的三类：字段 `readonly`（改成 `{ get; }`）、带 `[SerializeField]`、对外只读接口而类内要可变类型（`IReadOnlyList<>` 没有可写索引器和 `Add`/`Clear`）。`=> _x.y`、索引器属投影不是转发。

## 命名
- **帧号标识符统一 `Simulation*`**，全仓没有 `LogicFrame`/`LogicClock`；宿主是 Timeline 的 `SimulationClock`。Combat 里 `Logic` 只作动词（`TickLogic`）和"逻辑源"（`CombatLogicTimelineSource`）。
- 类名前缀表示**域**；`StateMachine` 一词归 Main 层（`Main/Base/StateMachine/`），`Controller` 后缀偏表现层。`UnitStateMachine` 已改名 `CombatStateMachine`（连带 `CombatStateMachineSnapshot`）。

## Combat 模块（`HotfixBase/Manager/Combat`）
- 目录 `Asset/` `Runtime/Core/` `Runtime/Unit/` `Runtime/Systems/` `Runtime/Presentation/`；命名空间统一 `Ux`，**挪目录不改代码**。
- 对象图 `CombatComponent → CombatController → {CombatStateMachine, CombatActionRunner}`，后两者互不引用；`CombatComponent` 内部一律 `Controller.X`。
- 逻辑帧驱动：`SimulationClock.FrameAdvanced` → `CombatMgr.OnFrameAdvanced` → `BattleWorld.Tick`（追赶循环，不跳号）→ `TickFrame` 8 阶段。`Update` 不参与。
- **阶段顺序唯一事实来源是 `BattleWorld.PhaseOrder`**（枚举值升序）；新增阶段同步 4 处（枚举成员、`PhaseCount`、`PhaseOrder`、`_systemBuckets` 的 `new()` 个数）——**漏改不报编译错**，靠静态构造函数记 Error 兜底。内置插件在 `BattleWorld` 构造函数注册，每世界各持实例。
- 插件互不引用，只走 World 上的交接缓冲：`ActionActiveEntities`（阶段2写→3读，唯一由生产者复位）、`FrameEvents`（3→4/5/6）、`PendingHits`（4→5）；后两个每帧开头由 World 复位。
- 遍历全场走 `OrderedEntities`，**别 `foreach world.Entities`**（装箱 `SortedDictionary` 枚举器，每帧一个堆对象）；只要"出招中的单位"就用 `ActionActiveEntities`。**别把 Timeline 并进 Actions 阶段**（会拆掉阶段屏障）。
- 输入两类：移动 `MoveInput` 是当前值（可覆盖、无帧号）；技能是命令（`CombatCommandBuffer` 按帧分桶，入队帧号 = 当前帧 + 1）。
- `StateLayer` 现为 **3 层**：`Locomotion=0 / Control=1 / Life=2`（Action 层已删）；`CombatStateId` 的 `enumValueIndex` 只在值从 0 连续时才等于枚举值。
- 招式生命周期**只在** `CombatActionRunner`；动作影响状态机的唯一路径是 `ActionRunner.BlocksMovement` → `IsMovementBlocked`（落在 Locomotion 层）。`CombatController.Tick` 顺序不变量：① `StateMachine.AdvanceTo` ② `ActionRunner.Tick` ③ Locomotion 三选一，②必须早于③。
- 状态层的 `Frame` 是基础时间轴的**权威播放头**（`Advance` 在 `StateId == 0` 时跳过，状态枚举从 1 起）。`AdvanceTo` 必须排在同帧 `SetLocomotion` 之前，否则新状态当帧被老化成 1，开场少一帧姿态。
- `StateChangeReason`/`CombatActionEndReason` 是**标记型枚举，只写不读**：同状态重复 Set 时 reason 被丢弃；`RestoreSnapshot` 不走 `ChangeState`。
- 零消费者的扩展点：`StateChanged`、`IsPlaying(StateLayer,int)`、`CombatFrameEventTable.TryFind`、取消窗口。
- 命中链路：`hitWindows` → 阶段3 求值成帧事件 → 阶段4 几何 → `PendingHits` → 阶段5 扣血/挂 Buff。坐标 1 世界单位 = 1 米 = 1000 整数毫米（`MillimetersPerUnit = 1000f`）。
- `UnitCombatSnapshot.CurrentVersion = 3`；快照**无落盘、无网络**，`RestoreSnapshot` 拒绝版本不一致。
- 未做：`AttributeSet`/`CombatBuff` 无修改器栈；`CombatHitResolver.AppendResolvedHits` 全参重载每攻击者每帧 2 次分配；`Log.Debug` 无级别过滤且 `HitboxSystem` 命中日志无条件打（比一次全场遍历还贵），用户说后面删。

## 编辑器与资产
- 技能配置 =「双源时间轴」：表现源 `profile.ActionPresentations[i].Timeline` + 逻辑源 `CombatLogicTimelineSource`（读写 `hitWindows`/`cancelWindows`）。资产须先被某个 `CharacterCombatProfile` 引用才能打开。
- 资产关联键是 `ActionId`，clip 的 `stableId` 只用于去重与编辑器 diff；编辑器按**字段名**取属性（`FindProperty`），给 `CombatActionAsset` 加字段是安全的。

## 环境与工具坑
- 沙箱**不能编 C#**（`dotnet restore` 报 NuGet `path1` null，`csc` 被拦）→ 只能静态校验 + 剥注释比对，必须让用户在 Unity 里过一遍编译；改了热更里的类型名要重编 `Data/Res/Code/*.dll.bytes`。
- 同一批里对**同一文件**发多个 Edit 会互相覆盖（甚至报成功但不落盘）→ 串行发。
- **bash heredoc 吃掉正则里的反斜杠** → 含正则或反斜杠的 Python 先 Write 成 `.py` 再执行。
- Python 普通字符串里的换行转义会变成真换行（把一行劈成两行）→ 要原样写入改用 `chr(92)` 拼，写完 `repr()` 回读。
- **本仓库行尾是混的**（同目录既有纯 LF 也有纯 CRLF）→ 按文件保留、别归一化，写完用 `git diff --stat` 自检（行尾被翻则 diff 行数 = 文件总行数）；**别用 `git show HEAD:` 比行尾**（`core.autocrlf=true` 时恒为 LF，测不出东西，误报过一次）。
- Unity 挪文件必须 `.cs` + `.cs.meta` 成对，新目录要补 `.meta`（照现成目录抄）。
- 校验"注释-only 改动"：只剥注释、保留字符串内容，去空行后逐行比对。
'''

out = NEW.replace('\n', '\r\n') if crlf == lf and crlf > 0 else NEW
with io.open(MEM, 'w', encoding='utf-8', newline='') as f:
    f.write(out)

with io.open(MEM, 'r', encoding='utf-8', newline='') as f:
    chk = f.read()
print('NEW bytes=%d chars=%d LF=%d CRLF=%d' % (len(chk.encode('utf-8')), len(chk), chk.count('\n'), chk.count('\r\n')))
print('lines=%d' % (chk.count('\n') + 1))
