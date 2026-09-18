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
| 宏观状态机 | `Manager/Combat/Runtime/Unit/UnitStateMachine.cs` | 4 层（Locomotion/Action/Control/Life），代码驱动规则，非资源求值 |
| 动作生命周期 | `Manager/Combat/Runtime/Unit/CombatActionRunner.cs` | 显式 ActionId 命令消费、取消窗口（连招）、预测/确认/拒绝 |
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
| 快照 + 回滚 | `Runtime/Unit/CombatController.cs:109-134` | 配 `Replay` 模式可重放 |

**确定性审计（结论：这套代码是照着确定性写的）：**

扫描 `Manager/Combat/` 全部源码，未发现任何 `UnityEngine.Random` / `System.Random` /
`Time.deltaTime` / `Physics.*` / `DateTime.Now`。两个易踩的坑也规避了：

- `CombatActionRunner.cs:76` 的 `HashSet<string>` 用了 `StringComparer.Ordinal`，
  避开 .NET 随机哈希种子，且只用于初始化去重校验，不在每帧逻辑里
- `CombatCommand.Compare` 对 SimulationFrame、RequestId、ActionId、TargetId 和 AimDirection 原始位序
  建立**完整稳定次序**，`CombatFrameCommands` 还会复制并排序输入列表，不依赖调用方容器顺序

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

### 代码目录结构

`HotfixBase/Manager/Combat/` 按**职责**分层。目录不影响编译 —— 命名空间统一是 `Ux`，
所以调整目录结构本身不带动任何代码改动。

```
Manager/Combat/
├─ CombatMgr.cs / CombatProfileMgr.cs    模块入口、配置加载与查询
├─ Asset/                                策划可配的资源（ScriptableObject）
│    CombatActionAsset                   动作：时长 / 命中窗口 / 取消窗口 / 伤害 / 附带增益
│    CharacterCombatProfile              角色：表现映射 / 移速 / 最大生命
│    CombatCommand                       逐帧输入命令（本地 / 网络 / 录像共用）
│    CombatState / CombatStatePresentation / CombatActionPresentation / StateChangeReason
├─ Runtime/
│    Core/                               世界与阶段框架，与具体玩法无关
│         BattleWorld                    实体注册 + 八阶段 tick + 快照 + 状态 hash
│         BattlePhase                    阶段枚举 + ICombatSystem 插件接口
│         ICombatEntity                  单位抽象（不依赖 Unit / GameObject）
│         BattleWorldSnapshot            世界级快照
│         CombatStageBuffers             阶段之间的交接缓冲
│    Unit/                               单位侧逻辑
│         CombatController               单位逻辑总装（状态机 + 动作 + 属性 + 增益）
│         UnitStateMachine               宏观状态机（Locomotion / Action / Control / Life）
│         CombatActionRunner             动作生命周期 + UnitCombatSnapshot
│         AttributeSet / CombatBuff / StateSnapshot
│    Systems/                            阶段插件，按 BattlePhase 挂载
│         CombatTimelineSystem           阶段 3：求值本帧帧事件
│         HitboxSystem                   阶段 4：几何查询 → 待结算命中
│         CombatDamageSystem             阶段 5：扣血 + 施加附带增益
│         CombatBuffSystem               阶段 6：周期结算 + 到期移除
│         CombatDeathSystem              阶段 7：HP ≤ 0 → Dead
│         CombatHitResolution            命中几何的纯函数内核（被 HitboxSystem 调用）
│    Presentation/                       只读投影，不反向写逻辑
│         CombatTimelinePlayer
```

`Hotfix/Common/Combat/CombatComponent.cs` 单独留在 Hotfix 程序集：它既是 `ICombatEntity` 的实现，
也是 Unit（Unity 侧）与 BattleWorld（纯逻辑侧）之间唯一的桥接，因此刻意与 HotfixBase 分开。

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
| `Manager/Combat/Runtime/Core/BattleWorld.cs` | 战斗实例：实体注册、8 阶段 tick、快照、状态 hash |
| `Manager/Combat/Runtime/Core/ICombatEntity.cs` | 单位抽象，刻意不依赖 `Unit`/`GameObject` |
| `Manager/Combat/Runtime/Core/BattlePhase.cs` | 阶段枚举 + `ICombatSystem` 插件接口 |
| `Manager/Combat/Runtime/Core/BattleWorldSnapshot.cs` | 世界级快照（含位置与朝向） |

**一帧的八个阶段**（`BattleWorld.TickFrame`）

```
1 Commands     插件 → 各单位 ConsumeCommands
2 Actions      插件 → 各单位 TickLogic（状态机 + 动作 + 位移）
3 Timeline     CombatTimelineSystem  求值本帧帧事件（命中窗口 / 取消窗口）
4 Hitbox       HitboxSystem          几何查询，产出待结算命中
5 Damage       CombatDamageSystem    固定伤害扣血 + 施加附带增益
6 Buff         CombatBuffSystem      周期结算 + 到期移除
7 Death        CombatDeathSystem     HP ≤ 0 → LifeState.Dead
8 Presentation 插件 → 各单位 TickPresentation
```

每个阶段内**插件先执行、内置核心后执行**。1/2/8 是内置核心；3-7 由 `BattleWorld`
构造函数注册的默认插件填充。这些插件只是"默认组合"，槽位本身允许被替换或再叠加，
所以后续接入更完整的实现不需要改动核心循环。

**阶段之间怎么交接数据**（不要互相持有引用）

```
ActionActiveEntities   阶段 2 写入 → 阶段 3 读取      阶段 2 开头清空
FrameEvents            阶段 3 写入 → 阶段 4/5/6 读取  每个逻辑帧开头由 BattleWorld 复位
PendingHits            阶段 4 写入 → 阶段 5 读取      同上
```

这些缓冲都挂在 `BattleWorld` 上，生产者在自己的阶段里填充，消费者在后面的阶段里读取，
插件之间零耦合。"每帧复位"由框架负责而不是消费者自己清，这样生产者漏注册时下游读到的
是空表，而不是上一帧的残留数据。

`ActionActiveEntities` 是唯一的例外：它由 Actions 阶段的内置核心自己复位。这么做是因为
那一层本来就在遍历全场推进逻辑，顺手记下"谁在出招"就能让 Timeline 阶段免掉一次全场扫描 ——
索引应该在已经付过遍历成本的地方建，而不是让下游再扫一遍。

**插件状态**

| 阶段 | 实现 | 状态 |
|---|---|---|
| Timeline | `CombatTimelineSystem` | 已落地。只遍历 Actions 阶段收集的 `ActionActiveEntities`（不再扫全场）；求值命中窗口与取消窗口，取消窗口目前无消费者（留给连招提示类 UI） |
| Hitbox | `HitboxSystem` | 已落地。几何查询 + 同窗去重，结果写入 `PendingHits` |
| Damage | `CombatDamageSystem` | **最小实现**：固定伤害，无修改器栈/暴击/减免（完整管线属 P1） |
| Buff | `CombatBuffSystem` | **最小实现**：身份/寿命/每帧扣血三字段，无叠层/驱散（完整形态属 P4） |
| Death | `CombatDeathSystem` | **最小实现**：判据只有 HP ≤ 0 |

属性系统同理只有 `AttributeSet`（MaxHp/Hp），设计文档 4.3 描述的修改器栈尚未实现。
`UnitCombatSnapshot.CurrentVersion` 已因加入属性与增益从 1 升到 2；
快照目前只在模块内部使用（没有落盘、没有网络传输），所以这次升级不需要兼容旧数据。

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

`CombatEditorWindow` 现在是 **角色技能编辑器 + 状态表现映射配置器 + 双源时间轴宿主**。
技能页默认以内嵌 Timeline 面板编辑逻辑轨和表现轨；`TimelineWindow` 仍作为独立窗口入口保留。

### 7.1 Profile

Profile 保存角色级参数、动态状态表现列表、动态技能逻辑资源列表，以及独立的技能表现映射。
技能逻辑通过 `CharacterCombatProfile.Actions` 组织，表现通过 `ActionPresentations` 将逻辑 Action 引用
关联到客户端 `TimelineAsset`；不能为 Attack、Skill01 等动作增加固定字段。

### 7.2 状态表现映射

状态候选由 `CombatStateId.GetMappableStateIds` 反射对应层的枚举生成。新增枚举值会自动进入
编辑器下拉和“添加默认映射”流程，不再维护手写状态白名单。只保留三项结构性排除：

- `StateLayer.Action` 整层由技能系统管理，不作为宏观状态表现映射；
- `ControlState.Normal` 不应抢占 Locomotion 表现；
- `LifeState.Alive` 不应以最高层优先级长期覆盖其它表现。

同一逻辑状态可以有多个 `variantId`。状态页只负责状态到 Timeline 的表现映射，状态切换仍由
`CombatComponent` 和业务状态规则负责。

### 7.3 技能（批次 A、B 已完成）

`CombatActionAsset` 现在只保存 ActionId、逻辑持续帧、移动策略和取消窗口，不再序列化
`TimelineAsset`。客户端表现由 `CharacterCombatProfile.ActionPresentations` 独立关联；
`CombatActionRunner` 不读取表现资源，`CombatComponent` 仅在表现阶段按 ActionId 向 Profile 查询 Timeline。
逻辑持续帧仍由 `CombatActionAsset` 作为运行时权威，必须显式大于 0；但编辑器在新建技能、打开角色配置或调整双源时间轴时，
会在表现时长超过逻辑时长时将逻辑时长向上扩展到表现末尾，避免动作提前结束而截断动画。该同步不会缩短逻辑时长，
也不会改写已有取消/命中窗口。

技能启动与取消命令只携带显式 `ActionId`。动作资产已删除 `TriggerCommand` 和启动 `Priority`，
取消窗口已删除 `AcceptedCommand` 与取消 `Priority`，只通过 `TargetActionId` 匹配；后续不得向逻辑资产
加入动画、Prefab、粒子、音效等客户端资源引用。

基础普攻示例当前仍可通过：

```csharp
combat.RequestAction(1001);
```

触发代码只提交逻辑命令，不直接播放 Timeline；不存在按命令类型或 Profile 顺序自动选技能的兼容路径。

---

## 八、目标技能创作架构：统一界面、双数据源

### 8.1 决策背景

目标是让策划在同一帧标尺上编辑技能逻辑时序和客户端表现，但分别保存数据：

```text
统一技能编辑器
├─ 逻辑数据  → CombatActionAsset（未来导出服务端配置）
└─ 表现数据  → TimelineAsset（客户端 Animation/VFX/SFX/Camera）
```

当前阶段可以不实现导出器，但必须从数据模型上禁止逻辑数据依赖客户端 Unity 资源。以后增加导出
应是序列化转换，而不是重新拆分技能资产。

### 8.2 数据归属

| 数据 | 权威归属 | 编辑方式 |
|---|---|---|
| ActionId | `CombatActionAsset` | 技能基础信息 |
| 逻辑持续帧 | `CombatActionAsset` | 全局逻辑配置或逻辑时间轴 |
| 取消窗口 | `CombatActionAsset` | 在统一时间轴显示为区间 Clip |
| 命中激活窗口 | `CombatActionAsset` | 在统一时间轴显示为区间 Clip |
| 移动锁定、逻辑位移、子弹生成 | 后续逻辑数据 | 在统一时间轴显示为逻辑轨道 |
| 动画、粒子、音效、镜头 | `TimelineAsset` | 表现轨道 |
| 输入/按键到 ActionId 的映射 | 业务代码 | 不在技能资源配置 |
| 伤害公式和规则解释 | 业务代码/服务端逻辑 | Timeline 不直接执行 |

`CombatActionAsset` 目标上是逻辑创作资产，不再持有 `TimelineAsset`。角色 Profile 增加独立的技能表现
映射，引用 `CombatActionAsset` 和对应 `TimelineAsset`；ActionId 只保存在逻辑资产中，避免重复填写。

```text
CharacterCombatProfile
├─ Actions: CombatActionAsset[]                // 逻辑技能集合
└─ ActionPresentations                         // 客户端表现映射
   └─ Action reference + TimelineAsset
```

### 8.3 字段收口

现有技能字段按以下方式处理：

- 删除 `TriggerCommand`：代码直接调用 `RequestAction(actionId)`；
- 删除启动 `Priority`：显式 ActionId 不需要按命令分组自动选择；
- 删除取消窗口的 `AcceptedCommand`：窗口直接匹配 `TargetActionId`；
- 不保留含义模糊的取消 `Priority`；取消请求只按 `TargetActionId` 匹配，重叠窗口不参与目标选择；
- 保留逻辑 `DurationFrames` 作为运行时权威；编辑器只允许在表现时长更长时向上扩展它，禁止用 Timeline 静默缩短逻辑时长；
- 保留或细化 `MovementPolicy`，它属于权威逻辑而不是表现；
- 保留取消窗口数据，但将编辑体验迁移为时间轴 Clip。

所有逻辑区间统一采用 `[StartFrame, EndFrame)` 半开区间，避免当前取消窗口闭区间与 Timeline Clip
半开区间之间产生一帧误差。

### 8.4 TimelineWindow 可扩展性审计

现有窗口不能直接安全接入第二数据源，主要耦合包括：

1. `TimelineWindow.Asset`、`TimelineWindow.Timeline`、保存和刷新入口都是静态单例；
2. `TimelineTrackView` 直接增删 `TimelineWindow.Asset.tracks`；
3. `TimelineTrackItem`/`TimelineClipItem` 只接受 `TimelineTrackAsset`/`TimelineClipAsset`；
4. Undo 固定记录单个 `TimelineAsset`，无法正确记录逻辑资产；
5. Inspector 使用具体类型 switch，缺少可注册的编辑器适配器；
6. 通用 Track Item 内含动画混入和重叠限制，不适用于允许重叠的逻辑窗口；
7. `IsValid` 把“可编辑文档”和“存在表现预览对象”混为一体。

可复用的是时间尺、缩放、滚动、播放头和 Clip 几何交互，不能通过给现有代码散布
`if (logicTrack)` 分支来实现双数据源。

### 8.5 编辑器目标抽象

引入 Editor-only 的文档/数据源适配层：

```text
TimelineEditorSession
└─ TimelineEditorDocument
   ├─ PresentationTimelineSource → TimelineAsset
   └─ CombatLogicTimelineSource  → CombatActionAsset
```

统一 Track/Clip View 面向编辑器适配接口，不要求逻辑领域对象继承表现用的
`TimelineTrackAsset`/`TimelineClipAsset`。每个数据源必须提供：

- 自己的 Undo Owner；
- Track/Clip 枚举与增删改命令；
- 帧率和持续帧；
- 区间重叠策略；
- Inspector 创建方式；
- Validate、SetDirty 和 Save 路由；
- 稳定的 SourceId + ItemId，不能只依赖托管对象引用。

播放预览只驱动 `PresentationTimelineSource`。逻辑轨道可在当前帧高亮和显示预览信息，但不得接入
PlayableGraph 作为服务端权威执行路径。

### 8.6 分阶段实施

1. **数据拆分（已完成）**：将 `CombatActionAsset` 收口为逻辑资产，新增 Profile 的 Action 到 Timeline 表现映射；
2. **命令收口（已完成）**：统一显式 ActionId 请求，移除 TriggerCommand 分组与 AcceptedCommand；
3. **运行时解耦（已完成）**：`CombatActionRunner` 只读取逻辑资产，`CombatComponent` 按 ActionId 解析表现 Timeline；
4. **双资产 Combat 编辑器（已完成）**：统一展示逻辑资产与 Profile 表现映射，但分别保存；
5. **编辑器抽象（已完成）**：把现有 Timeline View 从静态 `TimelineWindow.Asset` 改为文档/数据源接口；
6. **双源时间轴（取消窗口与最小命中窗口阶段已完成）**：接入 Combat 逻辑区间轨道，与表现轨道共用帧标尺并分别保存；
7. **后续导出**：把逻辑资产转换为服务端配置，客户端 Timeline 不参与导出。

不得先把逻辑 Track 临时存入 `TimelineAsset` 再计划以后拆分；这会让运行时、资源引用和编辑器操作
再次形成混合依赖，增加而不是减少未来导出成本。

### 8.7 可独立验收的实施批次

每一批必须保持可编译、可回归，不做一次性大爆炸重写。

#### 批次 A：先切断数据依赖（已完成）

- 新增 Action 到 Timeline 的 Profile 表现映射；
- `CombatActionRunner` 完全停止读取 Timeline；
- `CombatComponent` 通过 Profile 按 ActionId 获取表现 Timeline；
- 迁移当前已有技能资源，随后从 `CombatActionAsset` 删除 Timeline 字段；
- `DurationFrames` 改为严格逻辑值，缺失或非法时直接校验失败，不再从 Timeline 推导。

验收标准：逻辑技能资产序列化内容中不存在动画、Prefab、粒子或 Timeline 引用。

当前验收结果：现有三个 `CombatActionAsset` 已完成迁移；默认 HeroZS 的 Idle/Move 状态表现也已从旧固定字段
迁移到动态列表。手动 Roslyn 编译中 `Unity.HotfixBase` 与业务程序集通过，编辑器程序集未出现 Combat 新错误；
Unity EditMode Test Runner 的 Combat/Timeline 回归集已通过。

#### 批次 B：收口显式 ActionId 命令（已完成）

- 输入层明确把按键/操作映射到 ActionId；
- Runner 不再建立 `_startActions` 命令分组；
- 移除启动 Priority、取消 AcceptedCommand 和取消 Priority；
- 取消窗口改为 `[StartFrame, EndFrame)`；
- 同帧命令比较器加入完整稳定次序，禁止比较结果相同但对象内容不同。

验收标准：不存在“同一命令自动选择优先级最高技能”的运行时路径；未知 ActionId 不启动其它技能。

当前验收结果：`CombatCommand` 已收口为单一 `ActionId`，输入层将 Q/E/Space 显式映射到
1001/1002/1003；`CombatActionRunner` 直接按 ActionId 启动或取消动作，不再维护命令分组和优先级选择。
动作资产与取消窗口已删除 `TriggerCommand`、启动 `Priority`、`AcceptedCommand` 和取消 `Priority`，
取消窗口统一采用 `[StartFrame, EndFrame)`，并在 Profile、Runner 与编辑器校验中拒绝越界区间；
当前仓库三个技能资产的取消窗口均为空，因此本批次不存在旧闭区间数据需要执行 `EndFrame + 1` 迁移。
迁移边界明确为：批次 B 不直接兼容仓库外、旧分支或已构建 AssetBundle 中的旧闭区间数据；此类资源
合入前必须把旧 `EndFrame` 安全加一（`int.MaxValue` 必须报错而非溢出），旧 AssetBundle 必须重建。
同理，批次 A 之前仍使用固定状态 Timeline 字段的仓外 Profile 必须先迁移到动态表现列表。
`CombatFrameCommands` 会复制并按 SimulationFrame、RequestId、ActionId、TargetId、AimDirection 原始位序排序，
避免调用方传入顺序影响结果；未知 ActionId 回归测试确认不会回退启动其它技能。手动 Roslyn 编译中
`Unity.HotfixBase`、业务程序集和 Combat/Timeline 编辑器过滤程序集均通过；Unity EditMode Test Runner 的
Combat/Timeline 回归集已通过。

#### 批次 C：先让现有 Combat 编辑器适配双资产（已完成）

- 技能列表同时显示逻辑资产和表现映射，但分别通过各自 `SerializedObject` 保存；
- “打开 Timeline”和预览只操作表现 Timeline；
- 校验逻辑时长与表现 Timeline 时长；表现时长更长时，编辑器只向上扩展逻辑时长以避免动画被截断，不会缩短逻辑时长或改写逻辑窗口；
- 资源创建流程一次创建逻辑资产、表现 Timeline 和 Profile 映射。

验收标准：即使尚未有逻辑时间轴轨道，用户也能在统一技能条目中安全维护两类资产。

当前验收结果：技能导航与详情页会同时显示 `CombatActionAsset` 逻辑资产和 Profile 中的表现 Timeline；
逻辑字段与取消窗口只通过逻辑资产的 `SerializedObject` 保存，表现 Timeline 则通过 Profile 的独立
`SerializedObject` 映射保存。编辑器按技能资产引用定位映射，避免编辑过程中临时重复的 ActionId 导致
表现串线；打开、预览及动画 Clip 快捷编辑只操作表现 Timeline。创建技能时会在同一 Undo 事务中一次生成
逻辑资产、Timeline 和 Profile 映射，失败时回滚新资产与 Profile 修改；缺失映射也可单独补建。表现时长
超过逻辑时长时，创建技能、打开角色配置或调整双源时间轴会自动向上同步逻辑时长；表现较短时仍保留逻辑
时长，避免无意改变战斗窗口。新增编辑器测试覆盖持久化创建与 Undo/Redo、映射身份
解析、两类资产隔离保存、外部技能拒绝关联及持续帧差异警告。
`Unity.HotfixBase` 与 Combat/Timeline 编辑器过滤程序集已通过手动 Roslyn 编译；Unity EditMode Test Runner
的 Combat/Timeline 回归集已通过。

#### 批次 D：重构 Timeline 编辑器为单源适配器（已完成）

- 先只实现 `TimelineAsset` 数据源适配器，保持现有窗口功能不变；
- View/Item 不再直接访问静态 `TimelineWindow.Asset.tracks`；
- Undo、保存、Inspector 和重叠规则由数据源/轨道适配器提供；
- 增加编辑器回归测试后再进入双源阶段。

验收标准：动画 Timeline 编辑体验和运行时资源格式不变，且 View 层已不知道具体 Asset 类型。

当前验收结果：新增 `TimelineEditorDocument`、`ITimelineEditorSource`、`ITimelineEditorTrack`、
`ITimelineEditorClip` 以及当前唯一实现 `TimelineAssetEditorSource`。轨道、Clip、标尺和通用 Inspector View
只消费文档与适配器接口，不再引用 `TimelineWindow.Asset`、`TimelineTrackAsset` 或 `TimelineClipAsset`；
具体动画/粒子 Inspector 与资源类型分派收口在 TimelineAsset 数据源侧。轨道和 Clip 的增删、重命名、
拖拽、帧区间、动画替换、时长适配、重叠与混入混出计算均通过适配器执行，并统一以完整
`TimelineAsset` 为 Undo owner 和保存 owner。`UxUndo` 已改为按 Unity Undo group 精确匹配回调，并由 source
在修改提交后显式完成 Undo group，既不会在调用方写入对象前提前折叠并丢弃记录，也不会让无差异编辑与后续
无关对象共享 group。Undo 回调按 owner 路由到当前 Document source，资源切换后不会刷新已脱离文档的旧
adapter；每次 Undo/Redo 都重建当前 managed-reference adapter，清理旧 Inspector selection，
并按稳定 Track ID 恢复预览绑定。播放期间的数据源写入口统一拒绝修改。
新增编辑器测试覆盖轨道/Clip 增删、半开区间接触与重叠规则、所有已覆盖变更的 Undo owner/保存路由、
编辑锁、无关 Undo group 隔离，以及核心 View 的具体资产类型静态隔离。`Unity.HotfixBase` 与
Combat/Timeline 编辑器过滤程序集均通过手动 Roslyn 编译；Unity EditMode Test Runner 实际执行 61 项
Combat/Timeline 测试并全部通过。

#### 批次 E：接入 Combat 逻辑轨道（取消窗口阶段已完成）

- 增加 `CombatLogicTimelineSource`；
- 第一条逻辑轨只实现取消窗口，验证区间拖拽、Undo、保存和半开区间；
- 稳定后再增加命中、移动、位移、子弹等轨道；
- 表现预览仍只运行客户端 Timeline，逻辑轨由确定性 Runner 在逻辑帧解释。

验收标准：同一帧标尺可显示两类轨道，任一编辑操作只标脏并保存正确的 Owner。

当前验收结果：`TimelineEditorDocument` 已升级为有序多数据源文档，表现轨位于逻辑轨上方，帧率继续由
表现 `TimelineAsset` 或所属 Profile 提供，而文档宽度采用各 source 持续帧的最大值。新增
`CombatLogicTimelineSource`、固定取消窗口轨道及 Clip adapter；取消窗口按稳定 ID 定位，支持创建、删除、
Inspector 修改、区间拖拽与 `[StartFrame, EndFrame)` 边界钳制，并允许多个取消窗口重叠。逻辑技能时长
非法时不会回退到表现时长或编辑器默认值，也不会创建伪合法区间。旧取消窗口缺失或重复的稳定 ID 会在首次
接入时完成一次性持久化迁移。

Timeline 通用 View/Item 仍只依赖 `ITimelineEditorSource`、`ITimelineEditorTrack` 和
`ITimelineEditorClip`；Inspector 按 selection 所属 source 分派，并拒绝资源切换后已脱离 Document 的旧
adapter。表现与逻辑 source 分别以完整 `TimelineAsset` 和 `CombatActionAsset` 作为 Undo/保存 owner；
逻辑轨编辑和纯逻辑资源切换不会触发表现 Timeline 的重播或 seek。新增测试覆盖多 source 合并与事件路由、
独立保存、稳定 ID 持久化、半开区间钳制、重叠策略、拖拽 Undo/Redo、双 owner 交错 Undo/Redo、非法逻辑
时长以及 detached Inspector 隔离。Unity EditMode Test Runner 实际执行 72 项 Combat/Timeline 测试并全部
通过。移动、位移、子弹等逻辑轨仍按后续批次逐条接入。

#### 批次 F：接入最小命中逻辑轨（已完成）

- `CombatActionAsset` 新增纯逻辑 `ActionHitWindow` 列表，区间统一采用 `[StartFrame, EndFrame)`；
- 取消窗口与命中窗口共享单个 Action 内的逻辑 ItemId 唯一域，旧资源只迁移身份，不在打开编辑器时静默修正业务区间；
- `CombatLogicTimelineSource` 增加第二条固定命中窗口轨，复用通用帧标尺、拖拽、Inspector、Undo 与独立保存；
- `CombatActionRunner` 无状态枚举当前动作帧激活的命中窗口，按资产序列化顺序返回值快照；
- 本批次不接入形状、目标查询、Unity Physics、伤害公式或表现资源。

验收标准：命中时序完全属于逻辑资产；Runner 在相同动作快照与帧上得到相同窗口序列；编辑逻辑轨只记录并
保存 `CombatActionAsset`，表现 Timeline 不参与解释或导出。

当前验收结果：新增 `ActionHitWindow`、`CombatActiveHitWindow` 与
`CombatActionRunner.AppendActiveHitWindows()`。查询结果复制窗口 ID 和帧边界，不暴露可变资产对象；重复查询
无副作用，重叠窗口按序列化顺序稳定追加，snapshot restore 后无需额外命中状态即可重建相同结果。Profile、
Runner 与编辑器校验均拒绝空窗口、重复逻辑 ItemId、非法区间及超出动作逻辑时长的终点。命中窗口轨支持创建、
删除、Inspector 修改、半开区间钳制、重叠与拖拽 Undo/Redo，并继续与表现轨分别保存。Unity EditMode Test Runner
实际执行 83 项 Combat/Timeline 测试并全部通过。

#### 批次 G：逻辑命中查询与同窗口去重（已完成）

本批次将命中窗口从“时间声明”推进到“可验证的逻辑命中候选”，但仍不执行伤害结算：

- `ActionHitWindow` 当前只支持整数毫米逻辑圆形；
- `CombatHitResolver` 只接收上层提供的固定坐标目标快照，不调用 Unity Physics、Collider 或 Transform；
- 目标快照必须拥有唯一正数 `TargetId`，Resolver 按 `TargetId` 排序后计算；
- 同一动作实例、同一窗口、同一目标只产生一次候选命中；去重集合属于动作快照和世界状态哈希的一部分；
- 命中结果只表达攻击者、目标、动作实例、窗口和逻辑帧，不计算伤害、不修改目标生命值。

当前验收结果：已完成逻辑圆形查询、确定性目标排序、非法/重复目标 ID 拒绝、同窗口跨帧去重、
预测动作确认时的去重键迁移，以及去重集合与本地动作序列的快照恢复。世界帧、动作实例、命中确认、
本地序列和稳定排序后的命中集合均进入状态哈希；世界恢复会在修改前拒绝不一致的 active 实体集合。
编辑器可修改命中窗口形状参数并分别保存逻辑资产，关联 Profile 时会阻止表现 Timeline 帧率失配。
Unity EditMode Test Runner 实际执行 94 项 Combat/Timeline 测试并全部通过，reviewer 最终复审无阻断或中风险。
阵营过滤、命中形状扩展、命中事件消费和伤害结算仍留待独立批次，Resolver 不得直接依赖 Unity 场景对象。

#### 批次 H：窗口编辑入口收口与右栏分区（已完成）

批次 C 的过渡产物是：取消窗口 / 命中窗口既能在这里的表单里增删改，又能在时间轴逻辑轨上拖动与改属性。
同一份数据两个写入入口会持续分叉（区间约束、Undo、脏标记、测试面都要维护两套），并且新增一类逻辑窗口
就要在本窗口加一节表单，界面随逻辑类别线性膨胀。本批次把边界重新收口到 8.2 已定的归属：

- `CombatEditorWindow` 不再提供窗口增删改表单，只显示只读摘要（帧区间、目标、形状半径）并提供
  「在时间轴中编辑」入口；
- 逻辑窗口的唯一写入入口是时间轴逻辑轨（`CombatLogicTimelineSource` + 对应 Clip Inspector），
  Undo/Save owner 仍然是 `CombatActionAsset`；
- 右栏改为分区折叠（逻辑数据 / 逻辑窗口 / 客户端表现映射 / 诊断），展开状态按分区记入 EditorPrefs；
- 数据归属与运行时行为不变：窗口数据仍在 `CombatActionAsset`，不进入 Timeline，不参与表现解释。

因此后续新增逻辑类别（移动锁定、逻辑位移、子弹生成等）只需在时间轴增加逻辑轨，本窗口最多扩展一行摘要，
不再新增表单分区。
