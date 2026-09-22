# 审核报告：`StateLayer.Action` 删轴落地

> 审核对象：工作区未提交的改动（删 Action 轴 + `UnitStateMachine→CombatStateMachine` 改名 + Controller 属性改名）
> 代码基线：`HEAD = 9947ea6b 重构技能模块`，全部改动**未提交**
> 审核时间：2026-09-21 14:35-14:45
> 审核方式：只读核查（diff / 全仓 grep / 行尾与空白客观检查 / Unity 编译产物时间戳）。**审核方未修改任何一行代码。**

---

## 0. 结论

**功能正确、删除完整、且已通过 Unity 编译。** 有 **4 处**需要修，其中 1 处是原则性问题（与既定注释约定相反）。

| 等级 | 问题 | 位置 |
|---|---|---|
| **P1** | 删掉了最该保留的"坑"注释，替换进去的是无价值的复述 | `CombatActionRunner.cs` `TryCancel` / `AdvanceActionFrame` |
| **P2** | 3 处行尾空白（含 2 处原代码行/注释行变成空白行） | `CombatActionRunner.cs:248 / 550 / 589` |
| **P3** | 1 处步骤号失效 + 2 处属性名失效 | `CombatController.cs:74 / 100`、`COMBAT_DESIGN.md:168` |
| **P4** | 删测试时连带丢掉了 `CombatValidationIssue.Context` 的唯一覆盖 | `CombatEditorWorkflowTests.cs` |

---

## 1. 编译实证（本模块第一次拿到）

| 证据 | 值 |
|---|---|
| `Library/ScriptAssemblies/Unity.HotfixBase.dll` | 2026-09-21 **14:35:43** |
| `Library/ScriptAssemblies/Assembly-CSharp.dll` | 2026-09-21 **14:35:44** |
| `Library/ScriptAssemblies/Assembly-CSharp-Editor.dll` | 2026-09-21 **14:35:45** |
| 最晚修改的源文件 | **14:34:03**（`COMBAT_DESIGN.md`；最晚的 `.cs` 是 14:30:34 `CombatActionRunner.cs`） |
| 比程序集产物更新的源文件数 | **0** |
| `Unity/Logs/AssetImportWorker0.log`（14:35:54，1.12 MB）中 `error CS` | **0 次** |

三个程序集（含 Editor 与测试）都在源码全部落盘之后重新生成 → 编译成功。

---

## 2. 做对的部分

### 2.1 删除完整，零残留

全仓（排除 `Library/`）搜索 `StateLayer.Action` / `ActionState` / `SyncAction` / `ActionStarted` / `ActionEnded` / `UnitStateMachine`：

- 源码命中 **0 处**
- 唯一命中是 `SyncActionDurationToTimeline`（编辑器工具，同名前缀，无关）、`UnityEngine.InputSystem.InputActionState`（Unity 包）、`Unity/Logs/*.log`（改名前的旧导入记录）

19 个文件的改动都能对应到清单项。

### 2.2 快照版本处理正确

```csharp
// CombatActionRunner.cs:31-35
/// 1 → 2：加入 Attributes 与 Buffs；2 → 3：移除 CombatStateMachineSnapshot 的 Action 层快照。
public const int CurrentVersion = 3;
```

不仅涨了版本号，还补了版本历史。这比"只删字段不涨版本"稳（`RestoreSnapshot` 会拒绝版本不一致的快照）。

### 2.3 测试同步得体

- `CombatRuntimeTests.cs`：删掉 3 处以 Action 层为断言的**重复**断言（`ActionRunner.HasAction` / `Current.ActionFrame` 的断言均已保留）
- `CombatEditorWorkflowTests.cs`：删掉 2 条 Action 层断言 —— 规则本身已不存在，删得对（但见 P4）

### 2.4 枚举值选择是自洽的

选了**连续重排**（`Locomotion=0 / Control=1 / Life=2`，原值 `0/1/2/3`）。

这带来的实际好处是**编辑器零改动**：`CombatEditorWindow.cs` 的 6 处 `enumValueIndex` 与 `CombatTestProfiles.cs:57` 全都不用动。理由是自洽的 —— `SerializedProperty.enumValueIndex` 是"枚举名列表里的下标"，只有在值从 0 连续时它才恰好等于枚举值；连续重排保住了这个巧合。

**对现有数据的实测影响：无。**

| 数据 | 实测（main / origin / origin-main + 工作区） |
|---|---|
| Combat 资产里的 `layer:` | **只有 `0`**（`HeroZSCombatProfile.asset` 2 处），无 1/2/3 |
| `stateId:` | 只有 1 / 2 |
| `stableId`（含 `(int)layer`） | 只有 `state.0.1.default` / `state.0.2.default`，全仓 `state.[123].` **零命中** |
| `CombatTimelinePlayer` 的 owner key | 运行时字符串，不持久化 |

残余风险只有一条：**其它分支 / 同事工作副本里若存在未知的 `layer: 1` 或 `2`，会被静默解读成 `Control` / `Life`**（原来分别是 Action / Control）。空洞方案（`Control=2 / Life=3`）在这种情况下会报错而不是猜语义。当前**没有一行代码注释**记录"这两个值被重排过"，建议补一句：

```csharp
    public enum StateLayer : byte
    {
        Locomotion = 0,
        Control = 1,   // 原为 2；Action 层已删除，此处做了重排（旧数据里的 1 曾是 Action）
        Life = 2,      // 原为 3
    }
```

（快照那条路径不受影响：`RestoreSnapshot` 里 `value.Layer != layer` 会抛异常，属于大声失败。）

---

## 3. 需要修的 4 处

### P1 —— 删掉了最该保留的注释，方向反了（原则性问题）

`CombatActionRunner.TryCancel` 的**整个 doc 注释被删除**，其中包含：

```csharp
/// ⚠ 这正是"狂点打不出伤害"的成因：普攻 1001 的取消窗口是 [5,26) 且指向自己，
/// 帧 5-26 之间每次按键都会把动作重置回帧 0，永远走不到帧 41 的命中窗口。
```

这属于"**易踩的坑**"（还带具体数字），是明确的保留项。而替换进去的是：

```csharp
//这里要看取消窗口的目标动作是否和命令的动作一致，并且当前动作是否在取消窗口内
```

这句是**把紧邻的 `if` 条件翻译了一遍** —— 正是应当避免的噪声。

**净效果：删掉了有价值的，加上了没价值的。**

同类问题：`AdvanceActionFrame`（约 `:550`）里

```csharp
// 播完了自动收尾。这里的 DurationFrames 就是资产里的 70。
```

被删成一行纯空白。那是"这个数从哪来"的唯一线索。

**建议改法**：恢复 `TryCancel` 的 `⚠` 两行（其余解释可以不要），删掉那句复述 `if` 的注释；`DurationFrames` 那句恢复或直接删行（别留空白行）。

### P2 —— 3 处行尾空白（`git diff --check` 客观确认）

```
CombatActionRunner.cs:248: trailing whitespace.
+            {                
CombatActionRunner.cs:550: trailing whitespace.
+                        
CombatActionRunner.cs:589: trailing whitespace.
+                
```

- `:248` —— `{` 后面跟了 16 个空格
- `:550` —— 整行只有空格（原本是一句注释）
- `:589` —— 一个空的续行位置

**建议改法**：`:550` 恢复注释或整行删除；`:248` / `:589` 去掉尾部空白。

### P3 —— 步骤号与属性名失效

| 位置 | 现状 | 应改为 |
|---|---|---|
| `CombatController.cs:74` | doc：「执行顺序固定为"先推进动作、后判定移动"，顺序不能换，**原因见第 4 步**。」 | **第 3 步**（删掉 SyncAction 后 Locomotion 的编号从 4 变 3，见 `:89` 的 `// ── 3. 判定 Locomotion`） |
| `CombatController.cs:100` | 「注意 IsMovementBlocked 里含 **Actions**.BlocksMovement」 | `ActionRunner.BlocksMovement` |
| `COMBAT_DESIGN.md:168` | `\| 2 \| \`States.AdvanceTo\` + \`Actions.Tick\` \|` | `StateMachine.AdvanceTo` + `ActionRunner.Tick` |

`COMBAT_DESIGN.md` 的目录树还有一处对齐问题：`CombatStateMachine` 比 `UnitStateMachine` 长 2 字符，替换后描述列右移了 2 格（`CombatActionRunner` 那行没跟着移）。纯排版。

### P4 —— 删测试时连带丢了一个功能的唯一覆盖

被删的 `StatePresentationValidationUsesTimelineOrProfileContext`（3 个 `[TestCase]`）里，主断言确实关于 "Action 层"，但它还包含：

```csharp
Assert.AreSame(timeline, issue.Context);   // 或 Assert.AreSame(profile, issue.Context)
```

这是**全仓库唯一**覆盖 `CombatValidationIssue.Context`（校验错误挂到哪个对象上）的断言 —— 已 grep 确认 `Assets/Editor/Combat/Tests` 下再无任何 `.Context` 断言。

**建议改法：把用例改成另一个非法层，而不是删除。**

```csharp
CombatTestProfiles.AddStatePresentation(
    profile,
    StateLayer.Control,                 // 原来: StateLayer.Action
    (int)ControlState.Normal,           // 原来: (int)ActionState.Executing
    CombatStatePresentation.DefaultVariantId,
    timeline);
```

`Control.Normal` 走的是**同一个校验分支**（`CombatEditorUtility.cs` 里 `!CombatStateId.IsStatePresentationMappable(...)` → Error），所以改完其余一字不动，`Context` 的覆盖就完整保留了。同时把 `value.Message.Contains("Action 层")` 改成不含该串（或改成匹配新文案）。

---

## 4. 审核方自己的误判（已撤回）

初审时报告"9 个 `.cs` 从 CRLF 变成 LF 是回归"——**这是假警报，撤回**。两条理由：

1. `git diff --stat` 里这些文件只动了几十行（`CombatState.cs` 43/143、`BattleWorld.cs` 33/603、`HitboxSystem.cs` 4/128）。**换行符若被翻转，diff 行数会等于文件总行数。**
2. 同目录有 **10 个未改动的 `.cs` 本来就是纯 LF**（`CombatActionAsset` / `CombatActionPresentation` / `CombatMgr` / `CombatProfileMgr` / `BattleWorldSnapshot` / `CombatStageBuffers` / `ICombatEntity` / `CombatBuffSystem` / `AttributeSet` / `CombatBuff`）。LF 是该目录的**既存状态**。

误判的方法论原因（值得记下）：第一次用 `git show HEAD:<file>` 比行尾 —— 但 `core.autocrlf=true` 时仓库里存的**本来就是 LF**，这个比法恒为 LF，测不出任何东西。改用"同目录未改动文件做基线"才是对的。

---

## 5. 不算问题、但需要知道

- **热更 dll 严重过期**：`Assets/Data/Res/Code/Unity.HotfixBase.dll.bytes` 与 `Assembly-CSharp.dll.bytes` 的时间是 **2026-01-08**，比本次改动早 8 个月。`Library/ScriptAssemblies/` 的新 dll 不会自动同步到 `Res/Code/`。若要跑热更流程，这两份必须重新生成。
- **改动尚未提交，且混了三个独立变更**：`UnitStateMachine→CombatStateMachine` 改名、Controller 属性改名（`States`/`Actions`→`StateMachine`/`ActionRunner`）、删 Action 轴。建议拆成三个提交，否则删轴的 diff 与改名混在一起没法逐条核对。
- **`CombatStateMachine.SyncAction` 已随轴删除**，这是对的（它失去了唯一调用点）。将来翻 git 历史看到它消失时不必以为误删。
- 仍未处理、且本次未触及的既有项：`HasAction` 与 `Current.InstanceId > 0` 在当前所有路径下等价（可改为派生并删快照字段）；`StartAction` 仍是 public。

---

*审核方式：只读。审核方未修改工作区任何文件。*

---

## 6. 修复回执（2026-09-21 15:10）

P1 / P2 / P3 / P4 已全部处理；另修 **4 处失效行号** + **1 处措辞歧义**。

| 项 | 处理 | 落点 |
|---|---|---|
| **P1** | 恢复 `TryCancel` 的 ⚠ 坑注释（压缩为 2 行）；删掉复述 `if` 的注释与复述 `for` 的注释 | `CombatActionRunner.cs:578-584` |
| **P1** | `AdvanceTo` 里那行空白直接删掉 —— 原句与方法自身 doc（`:520-526`"累加到 DurationFrames 就自动 Completed"）完全重复，留着是噪声 | `CombatActionRunner.cs:549` |
| **P2** | 清掉 3 处行尾空白 / 空白行 | `CombatActionRunner.cs:248 / 549 / 592` 附近 |
| **P2** | 清掉 4 处行尾空白，并补回被误删的空行分隔 | `CombatController.cs:88-98` |
| **P3** | tick doc 的"原因见第 3 步"改为直述原因（步骤编号随重写一起消失了，编号引用已失效） | `CombatController.cs:74-75` |
| **P4** | 恢复 `StatePresentationValidationUsesTimelineOrProfileContext`：`Action/Executing` → `Control/Normal`（同一"不允许映射"分支），消息过滤改 `"不是允许配置表现映射"` | `CombatEditorWorkflowTests.cs:551-600` |
| 追加 | 文档行号全量核验，修 4 处失效：`CombatController.cs:149→138`、`CombatActionRunner.cs:76→131`、`:212→16`、`:285→517` | `COMBAT_DESIGN.md:20 / 82 / 231 / 407` |
| 追加 | "停掉 Action 层" → "停掉 Action 播放层"（`StateLayer.Action` 已删，原文与 `TimelinePlaybackLayer.Action` 同名易混） | `COMBAT_DESIGN.md:22` |

验证：

| 检查 | 结果 |
|---|---|
| `git diff --check` | 空输出（无行尾空白、无空白行） |
| 4 个改动文件行尾 | 全 CRLF，`CRLF == LF` |
| `StateLayer.Action` / `ActionState` / `SyncAction` / `UnitStateMachine` 全仓 | 0 命中 |
| `profile.FrameRate` / `TimelineAsset.SetFrameRate` / `CombatValidationIssue.Context` | 签名均已存在，测试调用合法 |
| 编译 | **未验证**（本环境无法编译 C#，源码改动全部是注释/空白/测试恢复） |

仍留着、本次刻意未动的一项：`CombatActionRunner.Tick` 里还剩 `// 0.` / `// 1.` 两个序号注释，
而 `2.` / `3.` 已被改成无序号散文 —— 序号体系只剩一半。属排版一致性，不影响语义。

> 本节由后续修复方写入，前 5 节仍是只读审核结论。
