# UxGame 项目长期记忆

## 分层与约定
- 单向三层：`Assembly-CSharp`（`Assets/Hotfix/`，**无 asmdef**，按程序集名登记热更）→ `Unity.HotfixBase` → `Unity.Main`；前两层都是热更 dll。
- asmdef 引用不了 `Assembly-CSharp` → HotfixBase 要用 Hotfix 的类只能依赖倒置（Combat：`ICombatEntity` 定义在 HotfixBase、实现在 Hotfix）。
- 新类放哪：碰 `Unit`/`GameObject`/`Animator`/`Transform` → `Assets/Hotfix/`；不碰 → `Assets/HotfixBase/`。
- `Assets/Editor/` **无 asmdef** ⇒ 编进 `Assembly-CSharp-Editor`，可引用 `Assets/Hotfix/` 与 `Unity.HotfixBase`。
- 命名：类名前缀表示**域**；帧号标识符统一 `Simulation*`（宿主 `SimulationClock`）。
- **不写"私有字段 + 转发属性"**：`public X Foo => _foo;` 改 `{ get; private set; }`；例外：`readonly` 字段、带 `[SerializeField]`、对外只读但类内要可变的类型。

## Combat 模块（`HotfixBase/Manager/Combat`）
- 对象图 `CombatComponent → CombatController → {CombatStateMachine, CombatActionRunner}`；内部一律 `Controller.X`。
- 逻辑帧驱动：`SimulationClock.FrameAdvanced` → `CombatMgr.OnFrameAdvanced` → `BattleWorld.Tick` → `TickFrame` 8 阶段；`Update` 不参与。
- **阶段顺序唯一事实来源是 `BattleWorld.PhaseOrder`**；新增阶段同步 4 处（枚举成员、`PhaseCount`、`PhaseOrder`、`_systemBuckets` 的 `new()` 数）——**漏改不报编译错**。内置插件在 `BattleWorld` 构造函数注册。
- 插件不互引用，只走 World 交接缓冲：`ActionActiveEntities`（阶段2写→3读）、`FrameEvents`（3→4/5/6）、`PendingHits`（4→5）。
- 遍历全场走 `OrderedEntities`（**别 `foreach world.Entities`**，装箱枚举器）；只要"出招中的单位"就用 `ActionActiveEntities`。**别把 Timeline 并进 Actions 阶段**。
- `StateLayer` **3 层**：`Locomotion=0 / Control=1 / Life=2`；`enumValueIndex` 只在值从 0 连续时才等于枚举值。
- 招式生命周期**只在** `CombatActionRunner`；动作影响状态机唯一路径是 `BlocksMovement` → `IsMovementBlocked`。`CombatController.Tick` 顺序不变量：① `AdvanceTo` ② `ActionRunner.Tick` ③ Locomotion 三选一（②必须早于③）。
- **连招只靠取消窗口**：`ResolveComboTarget(chainRoot)` 与 `TryCancel` 共用 `IsOpen`，输入层只认识链头；窗口最后一帧按下会落到窗口外（命令延迟一帧的固有代价，非缺陷）。
- 状态层 `Frame` 是基础时间轴的**权威播放头**（`StateId == 0` 时跳过）；`AdvanceTo` 必须排在同帧 `SetLocomotion` 之前。
- 表现层"要不要换轨道"靠 `CombatTimelineSelection.SameOwner`（**Asset 引用 + InstanceId**）。**`Frame` 必须不参与比较**；`InstanceId` 必须参与（连招第二下要重播）。
- **粒子轨**：运行期唯一绑定入口 `CombatTimelinePlayer.BindTracks`，必须在 `PlayOnLayer` **之后**（先绑会被新实例丢掉）；编辑期 `AutoBindMissingTracks`/`ResolvePreviewVfx`，两边都经 `CombatVfxHost.Ensure`。`TLParticleClip` 用 `Simulate` 重建 ⇒ 宿主须 `playOnAwake = false` 且保持停止。位姿 `positionOffset`/`rotationEuler`/`scaleFactor` 是**相对美术值的偏移**；`scaleFactor <= 0` 回落 1（旧资产反序列化成 0 会吃掉特效）；覆盖/还原成栈。

## 编辑器与资产
- 技能配置 =「双源时间轴」：表现源 `ActionPresentations[i].Timeline` + 逻辑源 `CombatLogicTimelineSource`（读写 `hitWindows`/`cancelWindows`）。资产须先被某 `CharacterCombatProfile` 引用才能打开。
- 关联键是 `ActionId`；编辑器按字段名取属性（`FindProperty`）⇒ 加字段安全。本项目 `TimelineAsset` 是 `Ux.*`，非 UnityEngine.Timeline。
- **加逻辑轨**：同步位置见 `COMBAT_DESIGN.md` 批次 K；**漏改 `ValidateData`/`MigrateLogicItemStableIds`/`tracks` 不报错**。
- 编辑器面板混排宽度坑：`GUILayout.Width(n)` 限制的是**标签列 + 输入框**整体，标签列由 `EditorGUIUtility.labelWidth`（默认 150）占，`n` 不够大时输入框被压成 0、只剩标签。固定宽度列别和可伸缩控件放同一 `HorizontalScope` —— 用 `EditorGUILayout.GetControlRect` 取整行手动切分（`CombatEditorWindow.DrawPresentationRow` 的 variantId/优先级 行是范例）。

## 环境与工具坑
- 沙箱**不能编 C#** → 只能静态校验 + 剥注释比对，必须让用户在 Unity 里过编译；改了热更类型名要重编 `Data/Res/Code/*.dll.bytes`。
- 同一批里对**同一文件**发多个 Edit 会互相覆盖 → 串行发。
- **本仓库行尾是混的** → 按文件保留、别归一化。`core.autocrlf=true` ⇒ **`git diff --stat` 测不出行尾翻转**（git 归一化后比较；实测整文件 LF 化仍只显示纯插入）。真实行尾看 `git ls-files --eol <p>` 的 `w/` 列；判断"原本是什么"只能靠动手前的备份。
- Unity 挪文件必须 `.cs` + `.cs.meta` 成对，新目录要补 `.meta`。

## 资源与命名
- YooAsset 定位地址 = **组名_文件名**（`AddressByGroupAndFileName`；分组见 `Settings/YooAsset/AssetBundleCollectorSetting.asset`），**与文件夹层级无关** → 挪目录不影响加载，关键是文件名。前缀来自分组：`Prefab_` / `Combat_` / `Timeline_`。
- 单位资源根（2026-09-30 起，原 Hero_ZS 已改名）：预制 `Res/Prefab/Unit/Role/Role_Dummy.prefab`（地址 `Prefab_Role_Dummy`），配置 `Res/Combat/Role/Role_DummyCombatProfile.asset`，时间轴 `Res/Timeline/Role/`。这些名字在 `Scene.cs`、`SceneModule.cs`、`Unit.cs` 三处硬编码，改名必须同步。
- 单位 Prefab 必须带 `Pathfinding.Seeker`（`Unit.LoadModel` 取 `Model.GetComponent<Seeker>()`）和带 Avatar 的 Animator（动画走 AnimationClipPlayable）。
- **Rig 规则（本项目裸 Playables 只支持 Generic）**：Dummy 下**所有**角色 FBX（含模型本体 `HumanM_Model.fbx`）必须 `animationType: 2`；**模型 FBX 保留 `avatarSetup: 1`**（自动生成 Generic Avatar，fileID 9000000，预制 `m_Avatar` 引用它），**只有带 `@` 的动画 FBX 才 NoAvatar**。模型是 Humanoid 而剪辑是 Generic 时动画完全不播（T-pose）。批量工具：`UxGame/工具/战斗/配置 Dummy 动画导入(Rig=Generic)`。Kevin 包 `takeName: Untitled` ⇒ 剪辑 fileID 恒为 3094330708855449807（名称哈希），换绑只需改 guid。
