
## 编辑器预览链路现状 —— 为什么"刀波"在编辑器里看不到

用户需求：编辑器播挥刀技能时，要能看见"生成刀波实体 → 往前飞 → 碰到敌人结算"。挥刀拖尾属纯表现（已通），刀波须真实体。

### 现状（本轮查明）
- `TimelineWindow` 的"播放" = `OnPlay()` 挂在 `EditorApplication.update` 上，每编辑器帧 `var frame = Asset.TimeToFrame(_playTime)` → `clipView.SetNowFrame(frame)`。**纯表现帧，走表现资产自己的时间换算，与 `BattleWorld` 无关。**
- 预览实体 `TLEntity`（`TimelineWindow.cs:20`）只有 `Viewer`，**不是 `ICombatEntity`**，没有 `CombatController` / `ActionRunner`。
- `BuildPreviewPlan()` 用 `clipView.CurFrame` 手动造 plan（按 `_combatProfile` / `_combatAction` 查资产），**不读 ActionRunner 运行时状态**。
- `BattleWorld` 在编辑器里**只被 EditMode 测试创建过**（`BattleWorldTests.cs`），无任何可视化宿主。
- `CombatEditorWindow`（2179 行）是配置编辑器（内嵌 TimelineWindow），不是战斗模拟器。
⇒ 编辑器只有"表现预演"，逻辑层完全缺席。刀波是逻辑产物，因此永不出现。

### 补上"逻辑回放"需要的最小件
- **最小战场**：`BattleWorld` + 一套与运行时相同的系统插件 + 一个能出招的实体 + 一个靶子（纯逻辑桩即可，不必有 GameObject）。
- **复用 `CombatComponent` 的成本**：它是 `ICombatEntity` 的唯一生产实现，但绑死 `Unit` —— `Id => Unit.ID`、`Position`/`Rotation` 代理 `Unit`、`MoveInput => Unit.Path.MoveVector2`、Profile 名取自 `Unit.CombatProfileName`，且 `SimulationClock` 帧率须与 profile 一致。沙盒要造一个最小 `Unit`。
- **脚本化输入**：逻辑为命令驱动（延迟一帧），沙盒需要"第 0 帧注入起手命令"的入口，不应走 `OperateComponent`（玩家输入）。

### 逻辑实体 → 可见对象 的桥接
- `BattleWorld.EntityRegistered` / `EntityUnregistered` 目前**零订阅者**（只有声明与 Invoke）⇒ 现成落点。
- 模式：表现工厂订阅注册事件生成表现对象（占位 / 美术）、注销时回收，每帧把 `entity.Position` / `Rotation` 同步到 transform。逻辑层（`HotfixBase`）因此不需感知 GameObject。
- 该桥接**运行时本来就需要**（战斗中生刀波），编辑器沙盒只是复用，不是额外工作。

### 可视化三层（建议顺序）
1. Gizmos：画逻辑位置 / 命中半径 / 速度方向。不依赖美术资源，逻辑验证最快，运行时也能开。
2. 占位对象：胶囊 / 球当刀波（同 `CombatVfxHost` 占位思路），`hideFlags` 用 `PreviewObjectHideFlags` 防落进场景文件。
3. 美术资源：拖尾粒子 + 模型。

### 两个坑
- **编辑器帧率 ≠ 逻辑帧率**：`OnPlay` 用 `EditorApplication.timeSinceStartup` 累积真实时间、按 `Asset.TimeToFrame` 换算 ⇒ 逻辑回放必须按逻辑帧率（`profile.FrameRate`）累积推帧，否则高刷屏下刀波飞得过快。
- **沙盒不能抄近路**：若为预览好看而让刀波走"手工摆的轨迹"，预览与逻辑必然分叉。必须跑同一个 `BattleWorld` + 同一套插件。

### 被否决的轻量替代
表现 Timeline 加事件轨标记"此帧生成刀波"、预览时生成纯表现对象按配置轨迹飞 —— 成本低，但飞行与命中都是假的，逻辑改动不会反映。仅在"只给美术看效果"时可用。
