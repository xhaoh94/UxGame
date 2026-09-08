# 战斗模块设计方案

> 结论先行：**不要引入第三方战斗框架**。项目已有的 `SimulationClock` + `CombatController` + `Timeline`
> 已经是一套标准的动作游戏战斗内核，缺的是"战斗内容层"（属性 / 命中 / 伤害 / Buff / 世界），
> 而不是"再来一套架构"。

---

## 一、现状盘点

### 已有实现（高质量，不要重写）

| 能力 | 文件 | 评价 |
|---|---|---|
| 逻辑帧时钟 | `HotfixBase/Manager/Timeline/TimelineMgr.cs:27` `SimulationClock` | 固定步长 + 追帧上限 + 插值 alpha；三种帧源（LocalRealtime / External / Replay） |
| 逐帧编排 | `HotfixBase/Manager/Timeline/Runtime/Base/Timeline.cs` | 帧驱动而非时间驱动；`ShouldTriggerFrame` 保证跳帧不漏事件 |
| 帧事件语义 | `Timeline.cs:44` `ShouldTriggerFrame` | Seek/初始化不触发事件，播放区间严格 `(prev, cur]`——命中帧判定就靠它 |
| 宏观状态机 | `Manager/Combat/Runtime/UnitStateMachine.cs` | 4 层（Locomotion/Action/Control/Life），代码驱动规则，非资源求值 |
| 动作生命周期 | `Manager/Combat/Runtime/CombatActionRunner.cs` | 命令消费、取消窗口（连招）、优先级、预测/确认/拒绝 |
| 预测与回滚 | `CombatActionRunner.cs:175` `Confirm` / `:202` `Reject`；`CombatController.cs:109` `CaptureSnapshot` | 客户端预测 + 服务器纠偏的骨架已完备 |
| 输入命令 | `Manager/Combat/Asset/CombatCommand.cs` | 逐帧命令队列，本地 / 网络 / 录像共用 |
| 表现桥接 | `Hotfix/Common/Combat/CombatComponent.cs:243` `ResolveTimelineOwner` | Life > Control > Action > Locomotion 优先级选 Timeline |
| 编辑器 | `Assets/Editor/Timeline/*` | TimelineWindow / TrackView / ClipView / Inspector 齐全 |

### 缺失（全项目零实现）

| 缺口 | 搜索证据 |
|---|---|
| 属性系统（HP/攻防/暴击） | 无 `Attribute` 战斗数值类（`Base/Attributes` 是 C# 特性，无关） |
| 命中判定 Hitbox/Hurtbox | 无任何 Hitbox/Hurtbox 实现 |
| 伤害管线 | 无 Damage / 伤害计算 |
| Buff / GameplayEffect | 无 Buff 实现 |
| 战斗世界（实体管理与 tick 顺序） | `Scene` 只做场景，无战斗世界 |
| Gameplay Track | Timeline 只有 `Animation` / `Particle` 两种 Track |
| 网络协议 | 场景同步已有（`Proto/protofiles/protofile/client/scene/`：Entity / AOI 广播 / 移动），**战斗协议无**（无帧命令、无伤害事件） |

---

## 二、主流方案对比

| 方案 | 代表 | 适用 | 本项目适配度 |
|---|---|---|---|
| **帧同步 Lockstep** | 王者荣耀、皇室战争 | MOBA/RTS，单位少、强对抗 | 中。时钟入口已有（`StartExternal`），需补定点数——当前仅 16 处浮点，改造量可控 |
| **状态同步（服务器权威）** | 大部分 MMO/ARPG | 单位多、弱实时 | **最高**。项目已有 `Pb.Entity` / `roleId` / AOI 广播，不要求确定性 |
| **客户端权威 + 战报校验** | 异步 PVP、放置类 | 无实时对抗 | 仅适用于 Arena 异步 PVP 这一特定玩法，不是通用主路线 |
| **UE GAS 移植** | `unity-gameplay-ability-system` | RPG/MOBA 技能系统 | 低。只解决"技能/属性/Buff"，不解决帧驱动与命中判定，且与自研 Timeline 职责重叠 |
| **行为树驱动** | 传统 ARPG AI | 怪物 AI | 低。仅适用于 AI 层，不是战斗内核 |

### 选型结论：按玩法分层，不锁死单一方案

上一版推断"客户端权威 + 战报校验"是**不完整的**——当时没查 Proto 目录。补查后发现
项目已有完整的 MMO 场景同步协议，选型要重新分层。

**关键证据：`Proto/protofiles/protofile/client/scene/`**

| 协议 | 含义 |
|---|---|
| `C2S_EnterScene` / `S2C_EnterScene` | `Entity self` + `repeated Entity others` |
| `Bcst_UnitIntoView` / `Bcst_UnitOutofView` | **AOI 视野管理**，MMO 的标志性机制 |
| `Bcst_UnitMove` / `Bcst_UnitUpdatePosition` | 服务器广播其他单位位置 |
| `C2S_Move` | 客户端上报移动路径（`repeated Vector3`） |

`IntoView/OutofView` 说明服务器架构**本来就是按 MMO 场景同步 + AOI 设计的**。
所以不能简单说"客户端权威"。

#### 联网就绪度评估

**已具备的地基：**

| 能力 | 位置 | 说明 |
|---|---|---|
| KCP 传输 | `NetMgr.cs:15-20` | KCP / TCP / WebSocket 三选一，KCP 是可靠 UDP，实时战斗首选 |
| 外部帧驱动 | `TimelineMgr.cs:107 AdvanceExternalTo` | 帧同步的时钟入口已预留 |
| 命令缓冲 | `Asset/CombatCommand.cs` | 注释明写"本地 / 网络 / 录像共用" |
| 快照 + 回滚 | `Runtime/CombatController.cs:109-134` | 配 `Replay` 模式可重放 |

**确定性审计（结论：这套代码是照着确定性写的）：**

扫描 `Manager/Combat/` 全部源码，未发现任何 `UnityEngine.Random` / `System.Random` /
`Time.deltaTime` / `Physics.*` / `DateTime.Now`。两个易踩的坑也规避了：

- `CombatActionRunner.cs:76` 的 `HashSet<string>` 用了 `StringComparer.Ordinal`，
  避开 .NET 随机哈希种子，且只用于初始化去重校验，不在每帧逻辑里
- `CombatActionRunner.cs:392-396 CompareAction` 是**全序排序**（priority 相同再比 `ActionId`），
  `CompareWindow` 同理 —— 不会出现"同优先级时顺序不确定"

原作者显然考虑过帧同步。但**还差临门一脚**：

- 16 处浮点：`CombatCommand.AimDirection` 是 `Vector3`，
  `CharacterCombatProfile.moveSpeedPerSecond` / `turnDegreesPerSecond` 是 `float`。
  浮点跨 CPU 不一致（x86 80 位扩展精度 vs ARM NEON、编译器优化差异），帧同步必然发散
- 快照只覆盖状态机 + 当前动作，**不含位置与血量**（属性系统尚未实现）

#### 三种玩法适配度

| 玩法 | 推荐模式 | 可行性 | 关键代价 |
|---|---|---|---|
| **MMO** | 状态同步 + AOI | **最高** | 协议已就绪，不要求确定性。补 BattleWorld + 属性/伤害，逻辑放服务器 |
| **ARPG** | 房间帧同步 或 状态同步 | 中高 | 单位少（4~20），帧同步带宽可控；走帧同步则须先做定点数 |
| **MOBA** | 房间帧同步 | 中 | 单位多（英雄 + 小兵），须定点数 + 完整回滚 + 带宽优化 |

**建议路径**：先走 MMO / ARPG 的状态同步路线（不依赖确定性，能最快跑通），
把 BattleWorld 和属性/伤害做出来。定点数改造推迟到真要做实时帧同步时——
但期间要把移动、朝向、伤害计算**隔离成纯函数**，将来换定点数只改这几处。
（越晚引入确定性约束，改造成本越高，见第七章风险。）

---

## 三、推荐架构

### 分层

```
BattleWorld           战斗世界：实体注册 + 固定 tick 顺序
  └─ CombatUnit       战斗实体：持有 Controller / Attribute / Hitbox / Buff
       ├─ CombatController   (已有)
       ├─ AttributeSet       (待建)
       ├─ HitboxSet          (待建)
       └─ EffectContainer    (待建)
```

### 一个逻辑帧的固定执行顺序（关键）

顺序错了会出现"打死了还能还手""buff 晚一帧生效"这类问题。

| 序 | 阶段 | 说明 |
|---|---|---|
| 1 | `ConsumeCommands` | 取出本帧命令 |
| 2 | `States.AdvanceTo` + `Actions.Tick` | 状态与动作推进（已有） |
| 3 | `Timeline.EvaluateFrames` | 求值 → 触发 Gameplay Track（开关命中框、位移） |
| 4 | **Hitbox Query** | 形状查询，产出 `HitEvent`，同目标去重 |
| 5 | **Damage Resolve** | 属性快照 → 修改器栈 → 伤害公式 → 应用 |
| 6 | **Effect Tick** | Buff 周期结算、过期清理 |
| 7 | **Death Check** | HP<=0 → `SetLifeState(Dead)` |
| 8 | `Presentation Sync` | 逻辑结果 → 表现层（Timeline / 飘字 / 血条） |

---

## 四、核心设计

### 4.1 逻辑/表现分离（最容易踩的坑）

现有 Timeline 的 Track 是**表现层**，依赖 `PlayableGraph` 与 Unity 对象。
伤害、命中框这类**逻辑**事件必须在无渲染环境下可跑（战报重放、服务器校验）。

**因此 Track 要分两类，不要混在一条 Timeline 求值路径上出问题：**

| 类别 | 现有/待建 | 依赖 | 运行环境 |
|---|---|---|---|
| 表现 Track | Animation、Particle（已有）；音效、相机（待建） | PlayableGraph / GameObject | 仅客户端 |
| Gameplay Track | 命中框开关、伤害帧、位移、生成物（待建） | 纯 C# | 客户端 + 重放 |

建议做法：给 `TimelineTrackAsset` 加一个 `IsGameplay` 标记，
`Timeline.EvaluateInternal` 里在无渲染模式下跳过表现 Track。

### 4.2 命中判定（动作游戏的核心手感）

参考 `TLParticleClip` 的写法扩展 `TLHitboxClip`：

```csharp
// HotfixBase/Manager/Timeline/Runtime/Gameplay/TLHitboxClip.cs
public class TLHitboxClip : TimelineClip
{
    private HitboxClipAsset _clipAsset;
    private readonly HashSet<uint> _hitTargets = new();

    protected override void OnEnable()
    {
        _hitTargets.Clear();              // 每次激活重置，实现"多段伤害"
        BattleWorld.Ins.RegisterHitbox(this);
    }

    protected override void OnDisable()
    {
        BattleWorld.Ins.UnregisterHitbox(this);
    }

    protected override void OnEvaluate(in TimelineEvaluationContext context)
    {
        if (Status != TLClipStatus.Ing) return;
        // 由 BattleWorld 在阶段 4 统一查询，不在 Clip 内直接结算
    }

    public void Query(in HitQueryResult result) { /* 形状相交 → 产出 HitEvent */ }
}
```

要点：
- **命中框查询由 BattleWorld 统一调度**（阶段 4），不在 Clip 里各自结算，保证顺序确定
- 用 `HashSet<uint>` 对同一激活周期内的目标去重
- 形状用**纯数学**（球/胶囊/OBB/扇形），**不要用 Unity 物理**——`Physics.Overlap` 不确定且无法在服务端/重放环境跑
- 命中判定结果记为 `HasHitConfirmed`（`CombatActionRunner.cs:212` 已预埋），供取消窗口做"命中确认后才能取消"

### 4.3 属性系统

```csharp
public sealed class AttributeSet
{
    // 当前值 = (基础值 + 加法修改) × 乘法修改 × (1 + 百分比) + 覆盖
    // 参考 GAS，但保持纯 C#、可序列化（战报校验需要）
    public float Get(AttributeType type);
    public void ApplyModifier(in AttributeModifier modifier);
}
```

- 修改器分类：`Add` / `Multiply` / `Override`，按序求值
- **必须可完整序列化**，否则战报重放与回滚无从谈起
- HP 建议用 `Current/Max` 双值，Max 受 buff 影响时 Current 按比例缩放

### 4.4 伤害管线

```
HitEvent → 取攻方属性快照 → 取守方属性快照
        → 命中类型判定（暴击/闪避/格挡）
        → 伤害公式 → 修改器栈（增伤/减伤/护盾）
        → 应用 → 产出 DamageEvent（飘字/受击表现）
        → HP<=0 → DeathEvent
```

**随机数必须可控**：暴击等随机要用带种子 PRNG（如 xorshift），种子写进战报，
否则重放结果不一致。禁止 `UnityEngine.Random`。

### 4.5 Buff / Effect

借鉴 GAS 的最小集，别贪多：

| 概念 | 说明 |
|---|---|
| Duration Policy | `Instant` / `Duration` / `Infinite` |
| Modifier | 属性修改器列表 |
| Stacking | 叠加规则（叠加层数 / 刷新时长 / 独立） |
| Tag | 用于互斥、免疫、条件触发 |

### 4.6 确定性约束（战报校验的生命线）

| 禁止 | 替代 |
|---|---|
| `UnityEngine.Random` | 带种子 PRNG，种子入战报 |
| `Time.deltaTime` / `Time.time` | 一律用 `SimulationClock.CurrentFrame` |
| `Physics.*` | 自写形状相交数学 |
| `float` 累积误差敏感运算 | 关键路径改定点数（可后置优化） |
| 字典遍历顺序依赖 | 实体遍历按稳定 ID 排序 |

> 建议：`float` 先跑通，但把命中判定与伤害计算隔离成纯函数，
> 将来要上真帧同步时只替换这两处。

---

## 五、实施路线

每阶段都有可验证产物，不要一口气铺开。

| 阶段 | 内容 | 验证方式 |
|---|---|---|
| **P0** ✅ | `CombatMgr` + `BattleWorld` + 8 阶段 tick + 实体注册 | `BattleWorldTests` 8 个用例；同输入两次运行状态 hash 一致 |
| **P1** | `AttributeSet` + 最小伤害管线（固定伤害） | 攻击 → 掉血 → 死亡，无随机 |
| **P2** | `TLHitboxClip` + Gameplay Track + 形状查询 | Timeline 上摆命中框，能打中人 |
| **P3** | 多段命中、去重、位移 Track、受击表现 | 连招命中不重复扣血 |
| **P4** | `EffectContainer`（Buff）+ 修改器栈 | 加攻 buff 影响伤害数值 |
| **P5** | 战报录制/回放 + 确定性校验 | 同一输入序列重放结果一致（hash 比对） |
| **P6** | 网络接入：命令上传 + 快照纠偏 | 联机对打，延迟下表现平滑 |

**P0 之前先补一个编辑器**：`Assets/Editor/Timeline` 已有窗口，
给 Gameplay Track 加可视化（命中框形状预览）能省下大量调试时间，建议和 P2 同期做。

### 5.1 P0 落地说明

**命名：为什么是 `CombatMgr` 而不是 `BattleWorldMgr`**

项目惯例是 `Manager/<模块>/<模块>Mgr.cs` 作为模块级单例入口（`TimelineMgr`、`ConditionMgr`、
`FogOfWarMgr`、`WayfindingMgr`…），所以运行时入口叫 `CombatMgr`，用 `CombatMgr.Ins` 访问。
但**它没有取代 `BattleWorld`，而是管理它**——因为世界不能是单例：

- 战报校验需要在同一进程里跑第二份战斗，单例做不到；
- 主世界（场景战斗）与回放世界必须能同时存在；
- 快照回滚是"世界级"概念，不是全局概念。

对应关系和 `ResMgr` 管 `ResHandle`、`ModuleMgr` 管 `ModuleBase` 一致。

**新增文件**

| 文件 | 职责 |
|---|---|
| `Manager/Combat/CombatMgr.cs` | 模块入口单例，管理多世界并分发逻辑帧 |
| `Manager/Combat/Runtime/BattleWorld.cs` | 战斗实例：实体注册、8 阶段 tick、快照、状态 hash |
| `Manager/Combat/Runtime/ICombatEntity.cs` | 单位抽象，刻意不依赖 `Unit`/`GameObject` |
| `Manager/Combat/Runtime/BattlePhase.cs` | 阶段枚举 + `ICombatSystem` 插件接口 |
| `Manager/Combat/Runtime/BattleWorldSnapshot.cs` | 世界级快照（含位置与朝向） |

**一帧的八个阶段**（`BattleWorld.TickFrame`）

```
1 Commands     插件 → 各单位 ConsumeCommands
2 Actions      插件 → 各单位 TickLogic（状态机 + 动作 + 位移）
3 Timeline     插件（P2 的 Gameplay Track 接入后才有内置实现）
4 Hitbox       插件（P2）
5 Damage       插件（P1）
6 Buff         插件（P4）
7 Death        插件（P1）
8 Presentation 插件 → 各单位 TickPresentation
```

每个阶段内**插件先执行、内置核心后执行**。1/2/8 是内置核心，3-7 目前只有插件位，
这样后续阶段不用改动核心循环。

**驱动链变更**

```
旧：SimulationClock → Scene.OnSimulationFrame → foreach players → Unit.TickSimulationFrame
新：SimulationClock → CombatMgr.OnFrameAdvanced → 各 BattleWorld.Tick(frame)
```

单位在 `CombatComponent.OnAwake`/`OnDestroy` 里自行注册/注销，Scene 不再感知 tick 细节；
`Scene.OnDestroy` 调 `CombatMgr.Ins.DestroyAllWorlds()` 做场景级收口。
`Unit.SimulationFrame` 改为透传 `Combat.SimulationFrame`，不再维护会过期的副本。

**确定性保障**

- 实体按 `SortedDictionary<long, ICombatEntity>` 的 Id 升序遍历，插件按 `(Order, 注册序号)` 全序排序；
- `ComputeStateHash()` 用 FNV-1a 64 覆盖状态机、动作 ID 与动作帧；
  位置与朝向**不**参与哈希——P0 还没上定点数，浮点跨机器不一致会把哈希变成噪声；
- 逻辑帧只允许正向推进，回退帧告警并忽略。

**已知遗留**：远端玩家目前同样参与本地 tick，`TickMovement` 会和服务器下发的位置打架。
这是 P0 之前就存在的行为，未做改动；修法是给 `ICombatEntity` 加"本地模拟/远端插值"标记，
现在加只改一处。

---

## 六、风险提示

1. **别让 Timeline 承担过多**：它只负责"什么时候发生什么"，
   "发生了什么"的具体规则要放代码层（`CombatController` 已经这么做了，保持一致）。
2. **Section 4.1 的逻辑/表现分离是硬约束**，一旦把伤害写进 Playable Track，
   后面想做服务器校验就要推倒重来。
3. **战报校验倒逼确定性**，这个约束越早引入成本越低；拖到后期补，等于重写。
4. 现有 `CombatActionRunner.Clear()` 会置空 `ActionChanged`（`:285`），
   注意对象池复用时的事件解注册，否则会串事件。

---

## 七、当前编辑器工作流（Combat 重构后的边界）

`CombatEditorWindow` 现在是 **角色技能编辑器 + 状态表现映射配置器**，不再复制
`TimelineWindow` 的轨道/Clip 编辑能力。

### 7.1 Profile

Profile 只保存角色级参数、动态状态表现列表和动态技能资源列表。技能通过
`CharacterCombatProfile.Actions` 组织，不能为 Attack、Skill01 等动作增加固定字段。

### 7.2 状态表现映射

状态表现只允许配置代码定义的宏观状态：

- `Locomotion`: `Idle`、`Move`、`Airborne`
- `Control`: `Stunned`、`Knockback`、`Frozen`
- `Life`: `Dead`

同一逻辑状态可以有多个 `variantId`。状态页只负责选择 `StateLayer`、状态 ID、变体、优先级
和 Timeline；状态切换仍由 `CombatComponent`/业务状态规则负责。`StateLayer.Action` 不应出现在
这个列表中。

### 7.3 技能

技能页独立显示 `CombatActionAsset` 列表，可编辑 ActionId、命令、优先级、持续帧数、移动策略、
取消窗口和 Timeline 入口。Timeline 具体轨道继续通过 `TimelineWindow.Open(...)` 编辑。

基础普攻示例使用 `actionId = 1001`、`CombatCommandType.Attack` 和 30 帧 Timeline。业务代码可调用：

```csharp
combat.RequestAction(1001);
// 或继续使用兼容的：combat.EnqueueCommand(CombatCommandType.Attack);
```

`RequestAction` 只提交逻辑命令，不直接播放 Timeline；目标逻辑帧由 `CombatActionRunner` 根据
ActionId 选择技能资源。这样既保留旧的按命令优先级选择行为，也支持明确请求某个动作。
