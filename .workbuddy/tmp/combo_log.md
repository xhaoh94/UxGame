
## 三连击示例 + 表现层特效轨接入（默认普攻 1001 / 1002 / 1003）

### 需求
给现有普攻例子加三连击，并把表现层接上特效轨（特效随便弄，只验证"有没有播出来"）。

### 连招：数据载体是取消窗口，输入层只认识链头
- `CombatActionRunner.ResolveComboTarget(int chainRootActionId)`（`Runtime/Unit/CombatActionRunner.cs:128`）：遍历 `CurrentAsset.CancelWindows`，返回第一条 `IsOpen(Current.ActionFrame, Current.HasHitConfirmed)` 命中的窗口的 `TargetActionId`；没有动作、或窗口都没开就返回 `chainRootActionId`。
- `CombatComponent.RequestComboAttack(int chainRootActionId)`（`Hotfix/Common/Combat/CombatComponent.cs:138`）：先查表再走 `RequestAction`。所以"按同一个键、出第几段"由资源回答，输入层只认识链头。
- `OperateComponent`（`OnFire` / `OnKey`）由 `RequestAction(AttackActionId)` 改成 `RequestComboAttack(AttackActionId)`，类头链路注释同步。
- **等价性**：`ResolveComboTarget` 与 `TryCancel`（`:622`）共用 `ActionCancelWindow.IsOpen`，所以查表结果下一帧一定能被接受。唯一例外是窗口最后一帧按下 —— 命令要下一帧才被消费，那时窗口已关。这是"命令延迟一帧"的固有代价而非缺陷，已用测试固化。

### 表现层：粒子轨此前根本没有运行时绑定（这是本次唯一的真缺口）
- 定位：编辑器预览路径 `TimelineWindow.AutoBindMissingTracks` 会给粒子轨自动挑一个系统，但运行时 `CombatTimelinePlayer.SyncLayer` 只绑 `Animator` → **特效轨配了数据也永远不播**。
- 修法：新增 `CombatTimelinePlayer.BindTracks(asset, animator, vfx)`（`Runtime/Presentation/CombatTimelinePlayer.cs:111`），`AnimationTrackAsset → Animator`、`ParticleAssetTrack → ParticleSystem`。
- **顺序不变量**：必须在 `PlayOnLayer` **之后**绑。`PlayOnLayer` 会 `FadeOut` 旧实例并新建实例，先绑会被新实例丢掉。`Synchronize` / `SyncLayer` 各加 `ParticleSystem vfx = null` 参数（默认 null → 该轨静默不播，非特效单位不受影响）。
- 链路核对：`SetBinding`（`TimelineComponent.cs:239`，签名 `(TimelineTrackAsset, UnityEngine.Object)`）→ `RebindTimelines()` → `Timeline.OnBinding()` → `TLParticleTrack.OnBinding()` 读 `Component.GetBinding<ParticleSystem>(Asset)`。
- 占位特效：新建 `Hotfix/Common/Combat/CombatVfxHost.cs`（guid `991f7fbf5770412baeb17c773aef3123`）。`Ensure(Transform parent)` 优先 `parent.GetComponentInChildren<ParticleSystem>(true)` 复用美术已摆好的系统，否则新建 `__CombatVfxHost`。项目是 URP → Shader 依次试 `Universal Render Pipeline/Particles/Unlit` → `URP/Unlit` → `Sprites/Default` → `Unlit/Color`，全找不到时 `Log.Error` 一次并置 `_shaderMissing`。
- `CombatComponent.EnsureVfx()`：只在 `_framePlan.Base/.Action` 里真存在 `ParticleAssetTrack` 时才建宿主（不给所有角色悄悄挂空系统）。
- `ParticleClipAsset` 新增 `public Color startColor = Color.white`（原来只有 `ClipType`）；`TLParticleClip.OnEnable` 在 `Stop` 之前覆盖 `main.startColor`。三段用浅蓝 / 橙 / 红区分第几段。
- **粒子系统必须 `playOnAwake = false` 且保持停止**：`TLParticleClip.OnEvaluate` 用 `particle.Simulate(localTime, restart:true, ...)` 按权威帧重建状态，自动播放会和 `Simulate` 叠加。
- 编辑器：`TimelineAssetEditorSource.CreateInspector` 加 `ParticleClipAsset` 分支 → 新增 `ParticleClipInspector`（名称 / 起止帧 / 起始颜色）。颜色只在 Clip 激活时生效，改完要让 Timeline 预览重播才看得到。

### 资产（脚本生成 + 静态校验）
- `Data/Res/Combat/HeroZS/HeroZSAttack01.asset` 改写：dur 70，命中 `[16,20)`，取消 `[20,36) → 1002`，damage 10。
- 新建 `HeroZSAttack02.asset`（guid `df5c55665ccb4cceb28669de4efbd78f`）：命中 `[18,22)`，取消 `[22,40) → 1003`，damage 12。
- 新建 `HeroZSAttack03.asset`（guid `3a0daab5585b46358429a7bbac37f885`）：命中 `[26,32)`，取消 `[32,60) → 1001`（回到一段，可无限连），damage 24。
- 三条 Timeline（`Data/Res/Timeline/HeroZS/HeroZSAttack0N.asset`）各 2 条轨：`AnimationTrackAsset`（`Hero_ZS@Attack.FBX`）+ `ParticleAssetTrack`（窗口 `[16,40)` / `[18,42)` / `[26,62)`，颜色不同）。
- `HeroZSCombatProfile.asset`：`actions` 与 `actionPresentations` 各从 1 条扩到 3 条。
- 生成脚本 `.workbuddy/tmp/make_combo_assets.py`（幂等可重跑）；校验脚本 `.workbuddy/tmp/verify_combo.py` → ALL OK。

### 测试
`Assets/Editor/Combat/Tests/CombatRuntimeTests.cs` 新增 6 项，另加 helper `Frame` / `QueryThenCancel` / `CreateComboChain` / `ConfigureCancelWindow` / `DestroyChain`：
`ResolveComboTargetFallsBackToChainRoot`、`ResolveComboTargetReturnsWindowTargetOnlyWhileOpen`（半开区间）、`ResolveComboTargetHonoursHitConfirmRequirement`、`ResolveComboTargetWalksFullThreeSegmentChain`（1001→1002→1003→1001）、`ComboQueryResultIsCancellableOnNextFrame`、`ComboQueryOnLastWindowFrameFallsOutsideAfterOneFrameDelay`。

### 静态校验（沙箱编不了 C#）
- 新建 `.workbuddy/tmp/verify_cs.py`：剥掉注释与字符串字面量后数括号配平。10 个改动文件全部 OK。
- **坑**：`[a,b)` 这类写在注释里的半开区间会让"括号计数"假失衡 —— 必须先剥注释再数，否则会误判改动坏了。
- `verify_combo.py` 的 guid 检查初版写成"全仓出现次数 == 1"，是错的：新资产 guid 本来就会出现在 Profile 引用里。改成**持有文件集合精确等于预期**（比计数强，能逮到多发的一处引用）。
- 同一脚本的 profile 引用检查还踩了"`statePresentations` 与 `actionPresentations` 缩进相同"的坑（`^    timeline:` 把两条状态时间轴也匹配进来，3 变 5），改成先 `split('actionPresentations:')` 再匹配。
- `git diff --stat` 自检未见行尾翻转（无文件的 diff 行数等于其总行数）。

### 待用户在 Unity 里做
编译 + 重编热更 dll：`CombatVfxHost.cs` 是新增类型，`ParticleClipAsset` 加了字段，`CombatTimelinePlayer.Synchronize` 改了签名。
