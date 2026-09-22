# 提案：删除 `StateLayer.Action` 整轴

> 供独立审核用。本文自包含，不需要读对话历史。
> 代码位置：`C:/Project/UxGame/Unity/Assets/HotfixBase/Manager/Combat/` 与 `C:/Project/UxGame/Unity/Assets/Hotfix/Common/Combat/`
> **本提案状态：未执行。`StateLayer.Action` 整轴在当前工作区中完好。**

### 代码基线（必读）

本文所有行号以**工作区当前状态**为准，而当前工作区**并不干净** —— 存在一组与本提案无关的未提交改动：

| 项 | git 状态 |
|---|---|
| `Runtime/Unit/UnitStateMachine.cs` → `CombatStateMachine.cs`（`.cs` + `.cs.meta` 成对） | `D` + `??`（未提交重命名） |
| `CombatController` / `CombatComponent` 的属性 `States`→`StateMachine`、`Actions`→`ActionRunner` | 未提交 |
| `CombatStateMachine.SyncAction(bool)` 新增，`CombatController.cs:90` 改用它 | 未提交 |
| `Runtime/` 与 `Editor/Combat/Tests/` 下若干文件（注释文案调整等） | 未提交 |

**审核方请注意**：本提案不得与上述改动混在同一次提交里。落地前应先把它固化成一个独立基线提交，
否则"删除 Action 轴"的 diff 会与重命名、属性改名混在一起，无法逐条核对。

---

## 0. 一句话结论

`StateLayer.Action`（"动作层"）与 `CombatActionRunner` 表达的是同一个事实，但它只承载两个值（`Free` / `Executing`）。
它在**表现层**已经被明确拒绝（不是"重复"，是根本没有消费者），在**状态哈希与快照**里与
`CombatActionRunner.HasAction` 构成同一事实的第二种编码，生产代码里**零读者**，
并且与该枚举自己的文档注释**直接矛盾**。

更高的代价是：**这条镜像的一致性不是被强制的不变量** ——
`CombatActionRunner.StartAction` 是 public、`CombatController.RestoreSnapshot` 又从快照独立恢复两套数据，
所以"镜像与真相分叉"在类型层面是可能的（见 §2.2）。

**建议整轴删除。** 这是当前架构下**推荐的最小复杂度方案**（措辞见 §5.3 ——
它不是"唯一可能的完整方案"，但它的收益不需要新增任何机制，代价都是可逐条核对的纯删除）。

**但这是一次跨 11 个核心文件的手术，另有 7 处 `StateLayer` 序列化字段用法需要复核**（见 §6）。
在当前开发阶段采用连续枚举值时，这 7 处不构成必须随本提案修改的兼容迁移；它们仍可作为独立的编辑器健壮性清理处理。
所以这份文档的目的是先让第二双眼睛验证：**证据是否成立、影响面是否有遗漏、有没有我没想到的反对理由。**

### 推荐的两个决定（本提案的立场）

| 决定点 | 推荐 | 位置 |
|---|---|---|
| 修哪个方案 | **A：删掉 `StateLayer.Action` 整轴**，并且把 D（入口收口）**降级为后续独立项、不作为前置** | §5.3 |
| 枚举值怎么排 | **B：连续重排**（`Locomotion=0 / Control=1 / Life=2`） | §7 |

这里的 A/B 分别指两件不同的事：A 是“删除 Action 整轴”的主方案，B 是“删除后如何重新编号”的枚举方案。
当前项目仍处于开发阶段，已核实仓库内没有 `Control/Life` 的历史序列化值、录像、网络协议或外部数据契约，因此本提案采用 B。

落地顺序：**先固化 §0 的独立基线提交 → 再做删轴与连续重排（一个提交）→ 之后单独评估 D 与 `HasAction` 冗余**。
不要把基线重命名、删轴和后续快照清理压成一个提交。

---

## 1. 触发这个提案的具体问题

在审核 `CombatController.Tick` 的第 3 步时发现：

```csharp
// CombatController.cs:90
StateMachine.SyncAction(ActionRunner.HasAction);
```

（这一行原本是并列三元 `SetAction(HasAction ? Executing : Free, HasAction ? ActionStarted : ActionEnded)`，
2026-09-21 已由此前的镜像同步重构为状态机内部的派生方法。）

用户提出两个疑问，第二个是真正把问题逼出来的那个：

1. **"一直有动作的话，不就会一直发 `ActionStarted` 吗？"**
   → 不会。`ChangeState` 里 `runtime.StateId == stateId` 直接 `return false`（`Runtime/Unit/CombatStateMachine.cs:195`），
   只有 Free↔Executing 的**边沿帧**才发事件。他随即指出"但读代码时感觉这里每帧都要报"——这个观感是对的，见 §3.3。

2. **"当前有正在执行的动作，新命令打断它并起手新动作，`SyncAction` 前后都是 `hasAction == true`，那不是没有变化？正常吗？"**
   → **正常，而且这正是暴露问题的地方。** 取消接起手（连招）时：
   - `CombatActionRunner.TryCancel` 是 `EndCurrent(CombatActionEndReason.Cancelled)` 紧跟 `Start(...)`，两步都在同一个 `ActionRunner.Tick` 内完成；
   - 所以 `Tick` 返回时 `HasAction` 又是 `true` → 第 3 步读到 `Executing → Executing` → **状态机层面完全静默**；
   - "换了一招"这个事实只记在 `CombatActionRunner.Current.InstanceId`（每次 `Start` 都 `++_localSequence`）上。

   也就是说：**Action 层根本没有能力表达"动作变了"，因为它只有两个值。** 它的存在没有带来任何表达能力，
   却带来了一个额外的、与 `ActionRunner` 平行的时间线（`StateLayerRuntime.Frame`），而这条时间线在连招时会与真实动作帧**分叉**（见 §5.2）。

---

## 2. 关键现状事实（全部已核对到行）

### 2.1 枚举定义与自我矛盾

```csharp
// Asset/CombatState.cs:6-13
/// <summary>战斗宏观状态层。具体攻击和技能不作为状态节点，而由 CombatActionRunner 管理。</summary>
public enum StateLayer : byte
{
    Locomotion = 0,
    Action = 1,   // ← 与上一行注释直接矛盾
    Control = 2,
    Life = 3,
}
```

`ActionState` 只有两个值：

```csharp
// Asset/CombatState.cs:22-26
public enum ActionState : byte
{
    Free = 1,
    Executing = 2,
}
```

注：StateLayer 的值从 0 起连续，而四个"状态值"枚举（`LocomotionState` / `ActionState` / `ControlState` / `LifeState`）
**全都从 1 起**，`0` 统一表示"未初始化 / 已清空"（`Runtime/Unit/StateSnapshot.cs:73-77` 的 `Clear()`）。

### 2.2 唯一的数据源在另一条链上

```csharp
// Runtime/Unit/CombatActionRunner.cs:110-111
public CombatActionAsset CurrentAsset { get; private set; }
public bool HasAction => CurrentAsset != null;
```

```csharp
// Runtime/Unit/CombatActionRunner.cs:640-645（EndCurrent 内）
Current = default;
CurrentAsset = null;
```

```csharp
// Runtime/Unit/CombatActionRunner.cs:620-622（Start 内，Start 定义在 :615）
Current = new CombatActionSnapshot
{
    InstanceId = ++_localSequence,
```

**一致性是"约定"，不是被强制的不变量。** 在正常 `CombatController.Tick()` 路径下，Action 层会根据
`ActionRunner.HasAction` 镜像同步（第 3 步 `SyncAction`），中间没有任何分支能让两者不一致。
问题在于同步责任分散在多类路径中：有的路径完全绕过 `SyncAction`，有的路径手写维护镜像，还有的路径独立恢复两套数据。

| 路径 | 说明 |
|---|---|
| `CombatActionRunner.StartAction(int, long, long, bool)`（`CombatActionRunner.cs:258`，**public**） | 直接起手，不会调用 `SyncAction`。当前调用方**只有测试**（`CombatRuntimeTests.cs:685/734/784/822/869/884/933/937/945`、`BattleWorldTests.cs:104`，共 10 处），生产代码 0 处；这是可直接制造镜像分叉的入口 |
| `CombatController.SetControl`（`:122-136`） / `SetLife`（`:138-152`）的非正常态分支 | `ActionRunner.Interrupt()` 后手写 `SetAction(Free, ActionEnded)`。它不是绕过后制造分叉，而是另一套与 `SyncAction` 并列的同步逻辑 |
| `CombatComponent.ConfirmAction` / `RejectAction`（`:252-286`） | 不调用 `SyncAction`；动作仍存续时依赖原有镜像，动作确认完成或拒绝时由调用方手写设为 `Free` |
| `CombatController.RestoreSnapshot`（`:174-199`） | `StateMachine.RestoreSnapshot(snapshot.StateMachine)` 与 `ActionRunner.Restore(snapshot.Action, snapshot.HasAction, ...)` 是**两次相互独立的恢复**，中间**没有校验** `snapshot.StateMachine.Action` 与 `snapshot.HasAction` 是否一致 |

所以可以构造出 `ActionRunner.HasAction == true` 而 `StateMachine.Action == Free`（或反过来）的状态。
**这意味着"删掉镜像层"的理由比"它冗余"更强：它冗余，而且它的冗余是靠分散的约定和手写同步维持的。**

### 2.3 完整引用面（已 grep 全仓 `Assets`，排除 `Main/HybridCLR/Generated` 与 `Library`）

| 位置 | 用途 | 性质 |
|---|---|---|
| `Asset/CombatState.cs:63` | `ToId(this ActionState)` | 定义 |
| `Asset/CombatState.cs:72` | `GetDisplayName` 的 Action 分支 | 定义 |
| `Asset/CombatState.cs:90-92` | `IsDefined` 的 Action 分支 | 定义 |
| `Asset/CombatState.cs:134-137` | `IsStatePresentationMappable` **对 Action 直接 `return false`** | 定义 |
| `Asset/CombatState.cs:154` | `GetStateEnumType` 的 Action 分支 | 定义 |
| `Asset/StateChangeReason.cs:8-9` | `ActionStarted` / `ActionEnded` | 定义 |
| `Runtime/Unit/CombatStateMachine.cs:14` | `Layers` 数组 | 存储 |
| `Runtime/Unit/CombatStateMachine.cs:19-22` | `_layers` 4 个槽 | 存储 |
| `Runtime/Unit/CombatStateMachine.cs:32-33` | `public ActionState Action` 属性 | 对外 API |
| `Runtime/Unit/CombatStateMachine.cs:47` | `Initialize` 里设 `Free` | 写入 |
| `Runtime/Unit/CombatStateMachine.cs:79-82` | `SetAction` | 写入 |
| `Runtime/Unit/CombatStateMachine.cs:84-93` | `SyncAction`（2026-09-21 新增） | 写入 |
| `Runtime/Unit/CombatStateMachine.cs:126` | `CaptureSnapshot` | 快照 |
| `Runtime/Unit/CombatStateMachine.cs:216` | `LayerIndex` 分支 | 索引 |
| `Runtime/Unit/StateSnapshot.cs:36` / `:45` | `CombatStateMachineSnapshot.Action` / `GetLayer` | 快照 |
| `Runtime/Unit/CombatController.cs:90` | `SyncAction` | 写入 |
| `Runtime/Unit/CombatController.cs:132` / `:148` | `SetAction(Free, ActionEnded)` | 写入 |
| `Runtime/Core/BattleWorld.cs:367` | 状态哈希 | 读取 |
| `Hotfix/Common/Combat/CombatComponent.cs:271` / `:284` | `SetAction(Free, ActionEnded)` | 写入 |
| `Editor/Combat/CombatEditorUtility.cs:46` | 层名显示文案 | 编辑器 |
| `Editor/Combat/CombatEditorUtility.cs:660-662` | 校验错误文案（"使用了 Action 层；技能表现必须放在动态技能列表中"） | 编辑器 |
| `Editor/Combat/Tests/CombatRuntimeTests.cs:170,171,1026,1060` | 断言 | 测试 |
| `Editor/Combat/Tests/CombatEditorWorkflowTests.cs:517,523,525,573` | 断言 + `[TestCase]` | 测试 |
| `Editor/Combat/COMBAT_DESIGN.md:428` | 文档 | 文档 |

**生产代码里没有任何地方读 `StateMachine.Action` 或 `GetStateFrame(StateLayer.Action)`。** 唯一读者是测试。

`GetStateFrame` 的生产消费者只有三处，全在 `Runtime/Presentation/CombatTimelinePlayer.cs:48 / 55 / 69`，
分别是 `Life` / `Control` / `Locomotion`——**动作的播放头用的是 `actions.Current.ActionFrame`**（同文件 `:66`）。

---

## 3. 它与 `CombatActionRunner` 的三处重叠（逐条给出成立条件）

### 3.1 状态哈希里是同一个布尔值的第二种编码（**有条件**）

```csharp
// Runtime/Core/BattleWorld.cs:366-372
hash = AppendHash(hash, (ulong)controller.StateMachine.GetCurrentStateId(StateLayer.Locomotion));
hash = AppendHash(hash, (ulong)controller.StateMachine.GetCurrentStateId(StateLayer.Action));   // ← 367
hash = AppendHash(hash, (ulong)controller.StateMachine.GetCurrentStateId(StateLayer.Control));
hash = AppendHash(hash, (ulong)controller.StateMachine.GetCurrentStateId(StateLayer.Life));
hash = AppendHash(hash, (ulong)controller.StateMachine.SimulationFrame);
hash = AppendHash(hash, controller.IsGrounded ? 1UL : 0UL);
hash = AppendHash(hash, controller.ActionRunner.HasAction ? 1UL : 0UL);                          // ← 372
```

**在 §2.2 那个一致性约定成立时**，367 与 372 是同一个布尔值的两种编码（`Free/Executing` ↔ `false/true`）。
但该约定不是被强制的，所以这两个字段**实际承担了双重作用**：一致时是重复信息，不一致时是唯一能暴露镜像分叉的地方。

不过要说清这条"分叉检测"能力的**实际价值**：`ComputeStateHash()` 全仓库**只有定义、没有任何非测试调用方**
（无落盘、无网络传输），所以今天它检测不到任何东西 —— 这是**理论上的**价值，不是现存的防线。
删掉 367 行不会削弱今天任何一条已有保障；真正的保障应该建在 §2.2 那三条入口上（见 §4 收口建议）。

### 3.2 快照里重复保存了"是否有动作"这个事实（**但不完全等价**）

```csharp
// Runtime/Unit/CombatActionRunner.cs:28-45（UnitCombatSnapshot）
public CombatStateMachineSnapshot StateMachine;   // 里面含 Action 层状态 + 帧号
public CombatActionSnapshot Action;               // 动作实例真相
public bool HasAction;                            // ← 与上面重复
public CombatAcceptedHitSnapshot[] AcceptedHits;
public long LocalActionSequence;
```

**重复的是"是否有动作"这个事实**：`CombatStateMachineSnapshot.Action`（`Runtime/Unit/StateSnapshot.cs:36`）
与 `UnitCombatSnapshot.HasAction`（`CombatActionRunner.cs:39`）说的是同一件事。

但两者**并不完全等价**，说清差别很重要：

| | `CombatStateMachineSnapshot.Action` | `UnitCombatSnapshot.HasAction` |
|---|---|---|
| 载荷 | `StateLayer` + `StateId` + `StateFrame` 三个字段（`StateSnapshot.cs:23-29`） | 一个 `bool` |
| 帧语义 | "进入该状态后经过的帧数" | 无（帧在 `UnitCombatSnapshot.Action.ActionFrame` 上，语义是"当前动作播到第几帧"） |

那两个 `Frame` **本来就是两个不同的概念**，而且连招时会分叉 —— 详见 §5.2。
所以这一节只能说"**那个 bool 事实重复了**"，并且删除时必须明确**那个独立的 Action 层状态帧是彻底废弃**，
而不是只删掉一个字段名。

### 3.3 表现层早已明确拒绝它

```csharp
// Asset/CombatState.cs:132-137
public static bool IsStatePresentationMappable(StateLayer layer, int stateId)
{
    if (layer == StateLayer.Action)
    {
        return false;   // 整层禁止
    }
    ...
```

配套的三处一致性：

- 编辑器可选层列表不含它：`Editor/Combat/CombatEditorWindow.cs:99-104` 的 `PresentationLayers` 只有 Locomotion / Control / Life；
- 校验器有专门的错误文案：`Editor/Combat/CombatEditorUtility.cs:660-662`；
- 有测试守着这条规则：`Editor/Combat/Tests/CombatEditorWorkflowTests.cs:517`（`Assert.IsEmpty(GetMappableStateIds(StateLayer.Action))`）。

---

## 4. 提案内容

**删除 `StateLayer.Action` 整轴**，连带删除只为它服务的 `ActionState` 与两个 `StateChangeReason` 值。
`CombatActionRunner` 保持不动——它是唯一的动作真相，且已经通过 `ActionChanged` 事件按**动作实例**粒度对外广播。

### 逐文件改动清单

| # | 文件 | 改动 |
|---|---|---|
| 1 | `Asset/CombatState.cs` | 删 `StateLayer.Action`（`:10`）、`ActionState`（`:22-26`）、`ToId(this ActionState)`（`:63`）、`GetDisplayName` 分支（`:72`）、`IsDefined` 分支（`:90-92`）、`GetStateEnumType` 分支（`:154`）；`IsStatePresentationMappable` 里 `:134-137` 的 Action 提前返回随之失去意义 → 删除（`Life.Alive` / `Control.Normal` 两条保留） |
| 2 | `Asset/StateChangeReason.cs` | 删 `ActionStarted`（`:8`）、`ActionEnded`（`:9`）——删轴后它们变成"无人写入"的值 |
| 3 | `Runtime/Unit/CombatStateMachine.cs` | `Layers`（`:14`）、`_layers` 槽数 4→3（`:19-22`）、`Action` 属性（`:32-33`）、`Initialize` 那一行（`:47`）、`SetAction`（`:79-82`）、`SyncAction`（`:84-93`）、`CaptureLayer` 调用与 `CaptureSnapshot` 字段（`:126`）、`LayerIndex` 分支（`:216`） |
| 4 | `Runtime/Unit/StateSnapshot.cs` | `CombatStateMachineSnapshot.Action`（`:36`）、`GetLayer` 分支（`:45`） |
| 5 | `Runtime/Unit/CombatController.cs` | `:90` `SyncAction`；`:132` / `:148` 的 `SetAction(Free, ActionEnded)` —— 这两行**紧跟在 `ActionRunner.Interrupt()` 之后**，删轴后它们只是"把镜像同步到已被清空的事实"，属冗余 |
| 6 | `Hotfix/Common/Combat/CombatComponent.cs` | `:271` / `:284` —— 同上，`ActionRunner` 已 `Reject` / `Confirm` 回来了，镜像行冗余 |
| 7 | `Runtime/Core/BattleWorld.cs` | `:367` 哈希行（`:372` 的 `HasAction` 已覆盖该信息） |
| 8 | `Editor/Combat/CombatEditorWindow.cs` | **6 处** `StateLayer` 的 `enumValueIndex` 用法——方案 B 下本提案无需修改；见 §6 的独立健壮性清理说明 |
| 9 | `Editor/Combat/CombatEditorUtility.cs` | `:46` 层名文案；`:660-662` 的 Action 专用分支（规则消失后该分支不可达） |
| 10 | `Editor/Combat/Tests/CombatRuntimeTests.cs` | `:170` / `:171` → 保留 `:172` 已有的 `HasAction` 断言（实际是删重复）；`:1026` / `:1060` 同理，`:1025` / `:1061` 已覆盖 |
| 11 | `Editor/Combat/Tests/CombatEditorWorkflowTests.cs` | `:517` / `:523` / `:525` 三行断言；`:573-574` 是个 `[TestCase]`，整个用例 `StatePresentationValidationUsesTimelineOrProfileContext` 会因"Action 层"规则消失而需要重写或删除 |
| 12 | `Editor/Combat/Tests/CombatTestProfiles.cs` | `:57` 的 `layer.enumValueIndex = (int)layer`——方案 B 下本提案无需修改；若以后采用空洞方案，必须改为 `intValue` |
| 13 | `Editor/Combat/COMBAT_DESIGN.md` | `:428` 那条"Action 层整层由技能系统管理"；`:390` 哈希描述；`:373` 快照版本描述；8 阶段表 |

---

## 5. 为什么"实例变了就把帧号归零"是补丁，以及删轴为何是当前推荐方案

### 5.1 补丁覆盖不了 Confirm 路径

```csharp
// Runtime/Unit/CombatActionRunner.cs:274-298（Confirm 内，节选）
var current = Current;
current.InstanceId = authoritativeInstanceId;                              // 权威值覆盖实例身份
current.StartSimulationFrame = Math.Max(0, authoritativeStartFrame);
current.ActionFrame = (int)Math.Min(int.MaxValue, ...);                    // 按权威帧号重算
```

`Confirm` 会用权威值**覆盖 `InstanceId`** 并把 `ActionFrame` **重算成跳变值**（而不是从 0 开始数）。
所以"检测到实例变了就把状态机那层的 `Frame` 归零"只会在 Confirm 路径上重新分叉——补丁把窗口收窄了，没有消除。
更糟的是它给 Action 层的 `Frame` 一个**与其余三层不同的含义**（别处是"进入该状态后经过的帧数"，
这里会变成"当前动作播到第几帧"），等于用一处新不一致换掉旧的那处。

### 5.2 删轴后随之消失的真实隐患

`ChangeState` 的提前返回**跳过了 `runtime.Set(stateId, 0)`**：

```csharp
// Runtime/Unit/CombatStateMachine.cs:194-201
var runtime = _layers[LayerIndex(layer)];
if (runtime.StateId == stateId)
{
    return false;            // ← 不走 Set(stateId, 0)，Frame 不归零
}
var previous = runtime.StateId;
runtime.Set(stateId, 0);
```

于是连招（或"播完同帧接起手"）时：

| | 第一次起手之后 | 连招帧 |
|---|---|---|
| `CombatStateMachine.GetStateFrame(Action)` | 0 → 1 → 2 → … | **继续累加**（不重置） |
| `CombatActionRunner.Current.ActionFrame` | 0 → 1 → 2 → … | **归零重新数** |

现状**完全无害**——该层的 `GetStateFrame` 生产代码零消费者，
全仓唯一读它的是 `Editor/Combat/Tests/CombatRuntimeTests.cs:171`，而那句断言恰好落在第一次起手那一帧（值还是 0），所以测试也照过。

**风险在将来**：另外三层（Life / Control / Locomotion）的 `Frame` 是**直接喂给 `TimelineComponent.EvaluateLayerFrame` 当播放头**的
（`Runtime/Presentation/CombatTimelinePlayer.cs:108`）。哪天有人照着这个写法把 Action 层的 `Frame` 接上，
连招就会继承上一招的播放时间——而且它**看起来"和别的层一样对"**，靠读代码很难发现。

删轴后这条隐患**连同它的载体一起消失**。

### 5.3 其他候选方案，以及为什么本提案推荐删轴

真正的根因是：**同一个动作生命周期由 `CombatActionRunner` 和状态机镜像共同维护，而镜像的一致性没有被强制保证。**
从这个根因出发，理论上有多个完整方案。逐一对比：

| 方案 | 评价 |
|---|---|
| **A. 删掉 `StateLayer.Action` 整轴**（本提案） | 删除负载冗余 + 删除未强制的不变量 + 删除会分叉的帧。改动面最大（13 文件），但每一步都是**纯删除**，没有新增机制 |
| **B. 保留 Action 层，让它跟踪 `InstanceId` 而不是只有 `Free/Executing`** | ❌ 不推荐。这等于把一个"实例身份"引进一个设计前提只有两个值的层，`ActionState` 要变成含 id 的结构，`IsDefined` / `GetMappableStateIds` / 快照 / 哈希全要跟着改 —— 换来的是"状态机也有了动作身份"，而 `ActionRunner` 早就有，重复没消除反而加重 |
| **C. 保留 Action 层，但删掉它的状态帧** | 半程。能消掉 §5.2 的分叉，但 `Free/Executing` 与 `HasAction` 的重复、以及 §2.2 那三条绕过镜像的入口都还在。不列为推荐 |
| **D. 把 `StartAction` / `Confirm` / `Reject` / `RestoreSnapshot` 全部收口到 Controller，由 Controller 原子同步两者** | ✅ **应当做，但它是 A 的补充而不是替代** —— 收口解决的是"入口能绕过镜像"，不解决"这条镜像本来就多余"。若选 A，收口仍然是独立的好事；若选 C，收口是必须的前置 |
| **E. 不做任何改动** | 现状零 bug、零性能问题。唯一代价是 §5.2 的隐患留在纸面上 |

### 5.4 D 为什么应当排在 A 之后，而不是作为 A 的前置

审核意见提出"D 是否应当作为前置条件"。**不应。** 三条理由：

1. **D 要守的不变量，正是 A 要删掉的那个东西。** D 的目的是强制"镜像与真相一致"；A 之后镜像不存在，
   这条不变量随之消失。把 D 作为前置 = 先加固一个即将被删除的对象。
2. **A 会让 D 的改动面缩小。** D 当前的入口有 4 条（`StartAction` / `Confirm` / `Reject` / `RestoreSnapshot`）。
   A 删掉后，`SetControl` / `SetLife` / `CombatComponent` 那 4 行 `SetAction(Free, ActionEnded)` 一起消失，
   D 真正剩下的只有两条：`StartAction`（public，10 处测试调用、生产 0 处）与 `RestoreSnapshot`。
   先 A 再 D，D 处理的是更小的问题。
3. **`Reject` / `Confirm` 从不绕过镜像。** 它们直接改 `Current` / `CurrentAsset`（含 `:293` 的 `EndCurrent(Completed)`），
   而镜像由第 3 步在 `Tick` 末尾统一派生 —— 它们本来就不需要"被收口"。

**D 剩余部分中真正值得单独处理的一条**：`RestoreSnapshot` 对 `snapshot.Action` 与 `snapshot.HasAction` 是两次独立恢复。
但实测这条**已有一半守卫** —— `CombatActionRunner.Restore`（`:440`）在 `hasAction && snapshot.InstanceId <= 0` 时抛异常，
而 `Confirm` 也强制 `authoritativeInstanceId > 0`（`:276`）。所以唯一未守的方向是 `hasAction == false` 却带 `InstanceId > 0`，
而该方向只是"静默忽略快照里的动作字段"。**详见 §13 的后续项。**

**结论（措辞已收紧）**：删轴（A）是**当前架构下推荐的最小复杂度方案**；在当前仍属开发阶段、没有外部数值兼容契约的前提下，枚举编号采用 B（连续重排）是更干净的配套选择。
本提案**不主张**删轴是“唯一可能的完整方案”：D 是应当做的补充（但排在 A 之后），而枚举方案 A 只应在进入发布/外部兼容阶段后采用。

> 修订说明：本节初稿写作"只有删轴才是根除"，措辞过强，已按审核意见收紧。
> 但仍有两点需要审核方注意：① C 只能解决 §5.2 而不解决 §2.2；② B 会把重复建模变得更重而不是更轻。

---

## 6. 需要复核：7 处 `StateLayer` 序列化字段用法

> 修订说明：本节初稿写的是"5 处"，但自己列了 6 个位置 —— 计数与内容自相矛盾，
> 且漏了第 7 处（`Editor/Combat/Tests/CombatTestProfiles.cs:57`）。已修正，感谢审核方指出。
>
> 当前提案选择连续重排方案 B，因此这 7 处不会因为删除 `StateLayer.Action` 而产生下标错位：
> 删除中间枚举项后，`Control` 和 `Life` 的枚举值分别变成 1、2，仍与编辑器枚举列表下标一致。
> 如果未来采用保留空洞的方案 A，这 7 处才必须统一改为 `intValue` 读写。

```csharp
// Editor/Combat/CombatEditorWindow.cs
:1016   var currentLayer = (StateLayer)layer.enumValueIndex;
:1051   layer.enumValueIndex = (int)PresentationLayers[nextLayer];
:1061   ids = CombatEditorUtility.GetDefinedStateIds((StateLayer)layer.enumValueIndex);
:1065   stateNames[i] = CombatStateId.GetDisplayName((StateLayer)layer.enumValueIndex, ids[i]);
:1118   CombatEditorUtility.GetStatePresentationTimelineSuffix((StateLayer)layer.enumValueIndex, ...)
:1505   var layer = (StateLayer)element.FindPropertyRelative("layer").enumValueIndex;

// Editor/Combat/Tests/CombatTestProfiles.cs
:57     element.FindPropertyRelative("layer").enumValueIndex = (int)layer;
```

`SerializedProperty.enumValueIndex` 是**枚举名列表里的下标，不是枚举值**。
当前它能当值用**纯属巧合**——`StateLayer` 的值恰好是 0/1/2/3 且从 0 起连续。

因此：

- 采用本提案的方案 B 时，这 7 处可以保持现状，不构成本次删轴的阻塞项；
- 若未来改用方案 A 保留数值空洞，这 7 处必须统一改成 `intValue`，否则编辑器会把 `Control` / `Life` 的枚举列表下标误当成底层枚举值，造成 UI 错位和测试辅助数据错误；
- 即使采用 B，也可以另开独立清理，把这些位置改成明确的 `intValue` 语义，但那是代码质量改进，不是本次枚举重排的必要条件。

**不属于本问题的 `enumValueIndex`**（已核对，改枚举值不影响它们）：
`CombatEditorUtility.cs:281`（`movementPolicy`）、`CombatLogicTimelineSource.cs:402` 与 `CombatRuntimeTests.cs:906`（命中形状 `shape`），
以及 `CombatEditorWindow.cs:2130` / `:2162` 两个与字段无关的通用辅助方法。

---

## 7. `StateLayer` 的枚举值怎么处理（本提案开发阶段推荐 **B 连续重排**）

已知现存资产只有 `layer: 0`：

```
Unity/Assets/Data/Res/Combat/HeroZS/HeroZSCombatProfile.asset:21  layer: 0
Unity/Assets/Data/Res/Combat/HeroZS/HeroZSCombatProfile.asset:28  layer: 0
```

（同文件 `stateId: 1` / `stateId: 2` 是 Locomotion 的 Idle/Move，与本问题无关。）
全仓 `.asset` 中**没有 `layer: 1` / `2` / `3`**。

**枚举值还流进了两个字符串产物**（审核方补充指出的影响面，已核实）：

```csharp
// Asset/CombatStatePresentation.cs:85-88
public static string BuildStableId(StateLayer layer, int stateId, string variantId)
    => $"state.{(int)layer}.{stateId}.{NormalizeVariantId(variantId)}";
```

- **StableId**：`BuildStableId` 把 `(int)layer` 写进 StableId，而 StableId **是持久化字段**。
  但实测现存 StableId 只有 `state.0.1.default` 与 `state.0.2.default`（`HeroZSCombatProfile.asset:20` / `:27`），
  全仓 grep `state.[123].` **零命中** —— 也就是**当前没有任何 Control/Life 的 StableId 存在**，
  所以方案 B 不会重解释任何已持久化的数据。真正的风险在将来：若 B 之后又新增 Control/Life 映射，
  新旧 StableId 会混用两套层编号。
- **表现层 owner key**：`Runtime/Presentation/CombatTimelinePlayer.cs:48 / 55 / 69` 用 `$"state:{(int)StateLayer.Life}:…"` 拼 key。
  这是**运行时字符串**、不持久化，值变了只会导致一次强制重铺，无副作用。

| 选项 | 枚举 | 需改的 `enumValueIndex` | 开发阶段影响 |
|---|---|---|---|
| **A 保留数值空洞** | `Locomotion=0 / Control=2 / Life=3`，1 留空 | 7 处必须改成 `intValue`，并维护空洞说明 | 对未知旧数据更安全，但永久保留一个无效值，编辑器和测试处理更复杂 |
| **B 连续重排（当前推荐）** | `Locomotion=0 / Control=1 / Life=2` | 本次删轴无需改动；枚举下标仍与底层值一致 | 开发阶段结构干净，现有资产只有 `layer: 0`，没有兼容迁移成本 |

当前已核实：

- 全仓资产没有 `layer: 1` / `2` / `3`；
- 持久化 StableId 只有 `state.0.1.default` / `state.0.2.default`；
- 没有已发布的网络协议、录像、外部表格或快照兼容契约需要保留 `Control=2` / `Life=3`。

因此，本开发阶段选择 B。它不是因为“兼容风险被证明不存在于所有未来环境”，而是因为当前项目尚未进入需要枚举数值稳定性的阶段，保留空洞会把尚未存在的兼容约束提前固化成永久技术债。

**选择 B 的边界：**

1. 方案 B 只适用于当前开发基线；进入发布、网络同步、录像回放、持久化快照、热更包或外部工具契约之前，必须冻结 `StateLayer` 数值，不得再连续重排。
2. 如果之后发现其他分支或工作区存在 `layer: 2` / `layer: 3` 数据，不能静默合并，必须先判断其语义并显式迁移。
3. `BuildStableId()` 未来生成的 Control/Life StableId 从此使用新编号，不应再混用旧编号规则。
4. 7 处 `enumValueIndex` 在本提案中可保持不变；将其改为明确的 `intValue` 语义可以另开代码质量提交，不与本次删轴混淆。

**何时改选 A：**

只要 `StateLayer` 数值已经进入任何外部或长期数据契约，就改为：

```csharp
Locomotion = 0,
// 1 保留为空洞
Control = 2,
Life = 3,
```

并将 7 处 `StateLayer` 序列化字段统一改为 `intValue` 读写，同时为数值空洞增加明确注释和校验。
**关于 `LayerIndex()` 的一处澄清（对审核意见的修正）**：`CombatStateMachine.LayerIndex`（`:211-221`）
**本来就是显式 switch**，不是"把枚举值当数组下标"。无论采用 A 还是 B，删除 Action 后都只需删除
`StateLayer.Action => 1` 那个分支，并让剩余三层映射到内部数组索引 `0/1/2`。要守的是另一条：
`Layers` 数组长度、`_layers` 的 `new()` 个数、`LayerIndex` 的分支数三者必须同时从 4 降到 3
（**漏改不报编译错**）。

---

## 8. 明确保留、不在本次范围内

| 项 | 理由 |
|---|---|
| `CombatActionRunner.ActionChanged` + `CombatActionEndReason`（7 个值，12 处写入） | 这是"按动作**实例**"那条通道，与状态机的"按状态**值**"通道正交，本提案不动它 |
| `StateLayer.Locomotion` / `Control` / `Life` 三轴 | 三者都有真实业务状态值与生产消费者（表现层按优先级解析） |
| `StateChangeReason` 枚举本身及其 4 个剩余值 | 见下条已知问题，但删它不在本提案范围 |
| `CombatActionRunner` 整体 | 唯一的动作真相，删轴后职责反而更清晰 |

### 顺带记录的两个"零消费者"扩展点（**不属于本提案**，供参考）

- `CombatStateMachine.StateChanged` 事件：全项目**零订阅者**（只有定义、2 处 `Invoke`、`Release` 里置 null）。
- `CombatStateMachine.IsPlaying(StateLayer, int)`：全项目零调用。

这条与 §9 的审核点相关：**如果 `StateChanged` 永远没有订阅者，那么"删掉 Action 层会不会损失可观测性"这个反对理由并不成立**——
今天 Action 层的变化本来就没有任何人在听。

---

## 9. 请重点质疑的几点（给审核方）

以下是本提案最可能出错的地方，请优先反驳：

1. **"生产代码零读者"是否查漏？** 我的 grep 面是 `Unity/Assets` 全目录（排除 `Main/HybridCLR/Generated/AOTGenericReferences.cs` 的噪声命中与 `Library`），
   关键词 `StateLayer.Action` / `ActionState.` / `SyncAction` / `SetAction(` / `ActionStarted` / `ActionEnded`。
   **请独立复核**：有没有通过反射、序列化字段名、Lua 字符串、`SendMessage` 这类非编译期路径引用到它？
   （此前的同类改名经验是：本模块零 Lua / 零 prefab / 零 .asset 字符串引用，但请别采信我的结论，重新查一遍。）
2. **"哈希里重复"是否成立？** 367 与 372 只有在 §2.2 的一致性约定成立时才等价 ——
   而 `StartAction`（`:258`，public）与 `CombatController.RestoreSnapshot`（`:174-199`，两次独立恢复、无交叉校验）
   就是绕过该约定的入口。**请复核这两条路径，并判断"删 367 行是否真的零信息损失"**：
   我的立场是"零损失"，因为 `ComputeStateHash()` 无任何非测试调用方，那个"分叉检测"能力今天等于不存在；
   但如果你认为哈希应当在删轴后**新增**一条一致性断言（而非删掉重复项），请明确说出来。
3. **`Confirm` / `Reject` / `RestoreSnapshot` 三条外部路径**是否会绕过 `SyncAction`、让镜像与真相不一致？
   （`CombatComponent.cs:271/284`、`Runtime/Unit/CombatController.cs:132/148` 就是为此存在的直接赋值。）
   **并请评估 §5.3 的方案 D（把所有入口收口到 Controller）是否应当作为本提案的前置条件，而不是可选项。**
4. **`StateLayer.Action` 是不是为将来某个特性预留的挂点？** 例如网络纠正 / 录像回放的事件订阅。
   `StateChangeReason` 的文档注释写的是"仅用于确定性调试、快照恢复和网络纠正"——
   但这三个消费者**目前一个都不存在**。请判断这个"预留"是否值得保留一条轴。
5. **§6 的判断是否正确？** `enumValueIndex` 与枚举值的关系我确认过语义。
   本项**初稿计数写错**（写"5 处"却列 6 个位置，且漏了 `CombatTestProfiles.cs:57`），
   现已按 7 处修正。请复核：**还有没有第 8 处**？我的排除依据是"只看与 `StateLayer` 序列化字段相关的"，
   已核对的无关项见 §6 末段。
6. **§7 选 A 还是 B？** 我对"残留脏数据应当报错还是静默重解释"的偏好可能偏保守，欢迎反驳。
   补充事实（供判断）：**现存的 StableId 只有 `state.0.1.default` / `state.0.2.default`**，
   全仓 `state.[123].` 零命中 —— 所以两方案对已持久化数据都无影响，差别只在编号规则的自洽性与将来。

---

## 9.1 本轮的审核回执（哪些意见已采纳、哪些未采纳）

独立审核指出了 7 项修正。处理结果：

| # | 审核意见 | 处理 |
|---|---|---|
| 1 | "`HasAction` 与 Action 层永远同步"不成立 —— 公开入口与快照恢复可造成分叉 | ✅ **已采纳**，§2.2 重写（含 `StartAction` 与 `RestoreSnapshot` 两条反例表） |
| 2 | "哈希中一定是重复"需加前提 | ✅ **已采纳**，§3.1 重写，同时补上"该分叉检测能力目前是理论上的"这一限定 |
| 3 | "快照里完全重复"不准确（Action 层还带独立帧） | ✅ **已采纳**，§3.2 重写为对照表。注：初稿 §5.2 已明确写过"两个 Frame 概念不同且会分叉"，此处是收紧 §3.2 的表述 |
| 4 | "只有删轴才是根除"表述过强 | ✅ **已采纳**，§0 / §5 标题 / §5.3 / §11 均已收紧为"当前架构下推荐的最小复杂度方案"，并补了 4 个候选方案的对比表 |
| 5 | `enumValueIndex` 应为 7 处而非 5/6 处 | ✅ **已采纳**，§4 表 / §6 / §9 全部改为 7 处，并新增 `CombatTestProfiles.cs:57` 一行 |
| 6 | 方案 B 还会影响 `BuildStableId` 与 owner key | ✅ **部分采纳**，已列入 §7 影响面。实测全仓无 `state.1/2/3.*`，owner key 是运行时字符串，故对当前已持久化数据零影响；基于开发阶段无外部契约，本轮最终选择 B |
| 7 | 工作区"无半成品改动"不成立（存在未提交的 `UnitStateMachine → CombatStateMachine` 重命名等） | ✅ **已采纳**，新增 §0「代码基线」小节，列出全部未提交项并要求先固化独立基线提交 |

**未采纳 / 反向修正的 1 项**：

| 审核意见 | 结论 |
|---|---|
| "采用 A 后 `LayerIndex()` 不能再把枚举值直接当数组下标，必须显式映射" | ❌ **事实有误**。`CombatStateMachine.LayerIndex`（`:211-221`）**本来就是显式 switch**，从来不是把枚举值当数组下标。无论采用 A 还是 B，删除 Action 后只需删掉 Action 分支，并让三层映射到内部索引 `0/1/2`。已在 §7 末段写明 |
| "方案 A 应优先于方案 B" | ❌ **在当前开发阶段不采纳**。A 适用于已存在外部数值契约的发布阶段；当前仓库已核实没有这类契约，因此本提案采用 B，避免提前固化空洞。 |

---

## 10. 验证方式（重要：本环境无法编译 C#）

沙箱里 `dotnet restore` 失败（NuGet `path1` null）、`csc` 被安全策略拦截，
所以**所有改动都只能静态验证**，必须由人在 Unity 内完成编译与测试。

本次提案若执行，验证清单：

| 项 | 方法 |
|---|---|
| 枚举槽位一致性 | `Layers` 数组长度必须等于 `_layers` 的 `new()` 个数必须等于 `LayerIndex` 的分支数（**漏改不报编译错**，需人工对齐） |
| 行尾 | 本仓库为 CRLF；写入后统计 `crlf == lf` 且 `bare_lf == 0`（必须读**原始字节**，不能读归一化后的文本） |
| 编辑器 UI | 打开 `CombatEditorWindow`，确认"状态表现"区域的层下拉：显示 3 项、切换层后 `stateId` 与文案都正确 |
| 测试辅助方法 | 方案 A 时需将 `CombatTestProfiles.cs:57` 改为 `intValue` 并验证 Control/Life 映射；本提案方案 B 下保留现状，但仍要验证新建映射显示正确 |
| 测试 | `CombatRuntimeTests` / `CombatEditorWorkflowTests` / `BattleWorldTests` 三套全跑 |
| 资产 | 打开 `HeroZSCombatProfile.asset`，确认两条 `layer: 0` 的表现映射仍能正确解析 |
| 热更 dll | `Unity.HotfixBase` 与 `Assembly-CSharp` 都在热更列表（`ProjectSettings/HybridCLRSettings.asset`），**两个 dll 都要重编译**，否则热更包与代码的类型对不上 |

---

## 11. 附：判断标准与既有约定（供审核方理解取舍口径）

这个项目里有一条用户明确认可的判断标准：

> **类名前缀表示"属于哪个域"，不表示"操作什么对象"。**

以及一条沟通偏好（与本提案的取舍有关）：

> **不做临时补丁，也不"等以后出问题再处理"——要么现在完整修复，要么明确保留并写清风险。**

本提案之所以倾向删轴而不是修补，正是按第二条标准：修补（§5.1）无法覆盖 `Confirm` 路径，
而删轴同时消除了 §5.2 的隐患载体，并且不需要新增任何机制 —— 这是"当前架构下最小复杂度"的依据（§5.3 已列出全部候选方案的对比）。

---

## 12. 附：本次触发的完整链条（读代码时的直觉路径）

```
用户读 CombatController.cs:90 → 质疑 SyncAction 每帧重复
  → 确认去重在 ChangeState（:195），只有边沿发事件（观感问题，非 bug）
  → 用户追问"连招时前后都是 hasAction，没变化，正常吗"
  → 确认正常，但发现 Action 层无法表达"换了哪一招"
  → 顺带发现 Frame 在连招时不重置（ChangeState 提前返回跳过 :201）
  → 用户要求"完整修复，不要补丁"
  → 结论：删掉 StateLayer.Action 整轴
```

---

## 13. 后续项（**不在本提案范围内**，立项与排期由用户单独决定）

本提案（删 `StateLayer.Action`）落地后，下面几项仍然存在。列出是为了**明确保留并写清风险**，不是偷偷塞进本提案。

### 13.1 同一个模式在下一层：`UnitCombatSnapshot.HasAction`

`UnitCombatSnapshot.HasAction`（`CombatActionRunner.cs:39`）与 `CombatActionSnapshot.InstanceId > 0` 是同一个布尔值的两种编码 ——
和 §3.1 是同一个模式，只是发生在快照而不是哈希里。

现有守卫只覆盖一个方向：

| 方向 | 现状 |
|---|---|
| `hasAction == true` 但 `InstanceId <= 0` | ✅ 会抛异常（`CombatActionRunner.Restore` `:440`）；`Confirm` 也强制 `authoritativeInstanceId > 0`（`:276`） |
| `hasAction == false` 但 `InstanceId > 0` | ⚠️ **静默忽略快照里的动作字段**（`:447-454` 直接 return）。不是 bug，但意味着这个 bool 无法被交叉校验 |

**建议方向**：把 `HasAction` 从快照里删掉，改为派生 `HasAction => Current.InstanceId > 0` ——
与 A 同一哲学（删掉不变量，而不是加固它）。代价：`UnitCombatSnapshot.CurrentVersion` 要 2 → 3
（该字段自己的注释写明"版本号必须跟着字段一起涨"）；快照无落盘、无网络传输，升级不需兼容旧数据。

**为什么不并入本提案**：它不属于 Action 轴，是"快照里存了一个可派生的位"。两件事分开决策、分开提交。

### 13.2 `CombatActionRunner.StartAction` 的 public 面

`:258` 是 public，且能绕过任何控制器层收口（当前 10 处调用全在测试、生产 0 处）。
若将来要收口，最小改法是降为 `internal` 或加 `[Obsolete]` 指向 Controller 的入口 —— 属于 §5.3 方案 D 的残余部分。

### 13.3 两个零消费者扩展点

`CombatStateMachine.StateChanged`（零订阅者）与 `IsPlaying(StateLayer, int)`（零调用），见 §8 末。

### 13.4 落地时的两条实现注意（避免被当成"漏改"）

1. **`CombatController.Tick` 的步骤编号注释要跟着改。** 现在的注释是"第 2 步推进动作 / 第 3 步同步镜像 / 第 4 步 Locomotion 三选一"，
   第 3 步整行删除后编号会断。**但 §5.2 那条顺序不变量不受影响**：仍是"推进动作（2）必须在 Locomotion（4）之前"。
2. **`SyncAction`（2026-09-21 在此前的镜像同步重构中新增）会变成死代码，应当一并删除。**
   记录在案，避免以后翻 git 历史时误以为它被错误删除 —— 它是为镜像层服务的，镜像层没了它就没有调用方。

---

*文档生成时间：2026-09-21。代码基线：工作区当前状态（`StateLayer.Action` 完好，未执行本提案）。*
