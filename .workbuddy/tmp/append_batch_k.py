import io

p = r'C:\Project\UxGame\Unity\Assets\Editor\Combat\COMBAT_DESIGN.md'
raw = io.open(p, 'rb').read()
assert raw.count(b'\r\n') == raw.count(b'\n'), 'doc is not pure CRLF'
assert '批次 K'.encode('utf-8') not in raw, 'already appended'

BLOCK = """#### 批次 K：生成窗口与两级映射（配置侧已完成）

批次 I/J 里的挥刀拖尾是纯表现，跟着动作时间轴播完就结束。刀波不一样：它的判定原点不在出招者身上，
而是出招之后才存在的一个空间位置 —— 所以它必须是一个独立实体，而"在哪一帧生成"必须由逻辑侧描述。
本批次只做配置侧（逻辑轨 + 两级映射），生成系统与实体留到下一批次。

**生成窗口是一条逻辑轨，和取消／命中窗口并列。** 架构为此是预留过的：`MigrateLogicItemStableIds` 的
注释原文写着"后续逻辑轨也必须追加到这个固定扫描顺序中"。三者的区间形状一致（`[StartFrame, EndFrame)`），
但语义不同：命中／取消是"区间内每帧成立"，生成是"进入区间的第一帧触发一次"。因此去重交给消费方按
`(ActionInstanceId, WindowId)` 记录 —— 与 `CombatActionRunner.TryAcceptHit` 完全同构，窗口本身不判边沿。

**加一条逻辑轨要同步的位置（编译器只覆盖其中一部分）。**

`CombatActionAsset`：

| 位置 | 漏改后果 |
|---|---|
| `ActionSpawnWindow` 类（继承 `CombatLogicWindow`） | 编译报错 |
| `spawnWindows` 字段、`SpawnWindows` 属性、`OnValidate` 的 `??=` | 编译报错 |
| `ValidateData()` 的 foreach | **不报错**，字段不 clamp |
| `MigrateLogicItemStableIds()` 的扫描 | **不报错**，stableId 不稳定 |

`CharacterCombatProfile.ValidateRuntime()` 追加窗口区间与"生成物非空"校验。
`CombatLogicTimelineSource` 构造函数把 `spawnTrack` **追加到 tracks 末尾** —— 顺序即索引，
测试与外部都按 `Tracks[0]`／`[1]` 取轨，插在中间会静默改变它们的含义。

**两级映射，与既有表现映射同构。** 逻辑侧 `CombatSpawnProfile` 描述"怎么飞、飞多久、用哪个动作"
（`flightAction` 承载命中窗口与伤害），刻意不含任何渲染资源；视觉侧 `CombatSpawnPresentation` 挂在
`CharacterCombatProfile`，以**资产引用**做键 —— 和 `CombatActionPresentation` 同一套写法。
这样逻辑程序集不会被拉进表现资源树，战报重放仍能在无渲染环境下跑。

**窗口轨的重复在本批次被消除。** 取消轨与命中轨此前各写了一份帧区间拖拽、夹取、undo 登记、增删与布局校验。
现在收敛到 `CombatWindowEditorTrackBase<TWindow, TClip>` 与 `CombatWindowEditorClipBase<TWindow>` 两个基类，
子类只声明"窗口在资产上的哪个列表、叫什么、什么颜色"。`CombatLogicWindow` 作为窗口共同基类让这一步成立：
基类可以直接按 `StableId`／`StartFrame`／`EndFrame` 做校验，不需要任何类型判断。

**顺带把数据源解耦到接口。** 原来 `CombatLogicTimelineSource.CreateInspector` 是一个多分支 switch，
每加一种窗口都要改它。现在元素自己实现 `ICombatLogicTimelineInspectorSource`（暴露 `Owner` 与
`CreateInspector()`），数据源的匹配缩成两行 —— 这与 `CoreViewsDoNotReachThroughTimelineWindowAsset`
那条契约测试同一个方向：数据源不该认识具体轨类型。

**本批次不做**：生成系统的运行时消费（阶段 Timeline 求值 → 注册实体 → `EntityRegistered` 通知表现层）、
投射物实体本身、`CollectTargets` 的"可被选中"护栏，以及编辑器可视化沙盒。
"""

block = BLOCK.replace('\n', '\r\n')
text = raw.decode('utf-8')
out = text.rstrip('\r\n') + '\r\n\r\n' + block
io.open(p, 'w', encoding='utf-8', newline='').write(out)

r2 = io.open(p, 'rb').read()
print('bytes %d  CRLF %d  LF %d' % (len(r2), r2.count(b'\r\n'), r2.count(b'\n')))
print('OK' if r2.count(b'\r\n') == r2.count(b'\n') else 'EOL PROBLEM')
