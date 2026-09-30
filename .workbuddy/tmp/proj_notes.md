
## 投射物（子弹 / 冲击波）实体化 —— 设计勘察（只讨论，未改码）

用户确定要做**真子弹**（部分需自动跟随目标）。本轮查清以下事实，是后续动手的前提。

### 双源时间轴的分层（本轮明确）
- 逻辑时间轴 = `CombatActionAsset.hitWindows/cancelWindows` → 阶段 3 `CombatTimelineSystem` → `BattleWorld.FrameEvents`。纯逻辑，无 Unity 依赖。
- 表现时间轴 = `TimelineAsset`（Animation / Particle 轨）→ 阶段 8 `TickPresentation` → `CombatTimelinePlayer`。
- 两者关联键 = `ActionId`。

### 生成"投射物实体"会撞上的既有约束
- **遍历序延后一帧**：`EnsureOrder()` 只在 `BattleWorld.Tick` 开头调用，`TickFrame` 里 `count = _ordered.Length` 本帧已固定 ⇒ 本帧中途 `Register` 的新实体最早下一帧才被 `PhaseActions` 遍历到。想要"生成即同帧命中"必须绕开遍历、直接写 `PendingHits`。
- **快照严格一一对应**：`RestoreSnapshot` 在实体集合与快照不匹配时直接 `throw` ⇒ 生成必须完全由帧号+命令确定性重演，不能由外部输入触发。
- **哈希不含位置**：`ComputeStateHash` 明确排除位置与朝向（浮点跨机器不一致）⇒ 投射物的位置分歧**不会被帧同步校验发现**，是个盲区（除非位置量化成定点再入哈希）。
- **目标收集无护栏**：`HitboxSystem.CollectTargets` 收所有 `IsCombatActive && Id > 0` 的实体为可命中目标 ⇒ 投射物会被别的攻击选中（刀光砍到子弹），需加"是否可被选中"的标志或阵营过滤。
- **命中几何原点 = 出招者自身**：`CombatHitQuerySource(set.EntityId, ToFixedPoint(entity.Position))` ⇒ 现有模型没有"判定区域脱离出招者"的概念，这是投射物需要新东西的根本原因。
- **Id 既是标识也是排序键**：`_entities` 是 `SortedDictionary`，遍历序 = Id 升序。给大 Id 可让投射物稳定排在角色之后（同帧末尾结算）。
- **`ICombatEntity` 偏重**：要求 `Controller`（完整状态机 + 动作生命周期 + Runner），对"只会飞"的实体过度设计。

### Timeline 复用成本
- `TimelineComponent` 挂在 `Unit` 上（`Unit.Timeline`），本身是框架 `Entity`。
- `CombatTimelineResolver.Resolve` 需要 `CharacterCombatProfile` + `CombatStateMachine` + `CombatActionRunner`（按 Life/Control/Locomotion 选 Base 层、按 ActionId 取 Action 层）⇒ 投射物无 profile / 无状态层 / 无 ActionId，**无法直接套**。
- `TimelineComponent` 有独立 API：`Play(asset)` / `Set(frame)` / `TickCurrentFrame()` / `Stop()` / `SetBinding` / `GetBinding` ⇒ 可脱离 Plan 单独用（编辑器预览即此路径）。
- `TLAnimationTrack` 绑 `Animator`，经 `TLAnimationRoot` + `AnimationMixerPlayable` 驱动**骨骼**，不直接写根 Transform ⇒ 动画轨默认不与逻辑位置冲突（除非动画带 root motion）。
- `TimelinePlaybackLayer` = `Base=0 / Action=100 / Reaction=200 / Additive=300`；**`Reaction` 目前无生产者**，原用途待确认。

### 本轮结论
- 投射物**逻辑侧不套 `CombatActionAsset`**：其寿命取决于飞行距离 / 是否命中，非先验固定时长；命中是几何相交而非帧窗口；取消窗口 / 连招 / 状态层对其无意义。
- 投射物表现**不能挂在施法者的 `CombatTimelinePlayer` 上**：施法者动作结束即 `StopLayer`，两者时间尺度不匹配。
- 表现两条路：① 自持 `TimelineComponent`，按自身逻辑帧 `Set(frame - spawnFrame)`；② 不上 Timeline，prefab 自带循环拖尾 + 命中时一次性特效（**爆炸时机必须由逻辑帧驱动，不能用碰撞回调**）。
- 判据：投射物有没有"帧编排"需求（飞行中途变形 / 展开 / 分裂）——没有就别上 Timeline。
- **位移与朝向的权威归逻辑**；表现 Timeline 只能作用在不含位移的层（或子节点），否则与逻辑位置互相覆盖。
