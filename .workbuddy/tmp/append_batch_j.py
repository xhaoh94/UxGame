# -*- coding: utf-8 -*-
"""追加批次 J 章节到 COMBAT_DESIGN.md（CRLF）与今日工作日志（LF）。

两个文件行尾不同，分别按各自现状写回：脚本自带行尾断言，写错会直接失败而不是静默洗掉。
"""
import io

DOC = r'C:\Project\UxGame\Unity\Assets\Editor\Combat\COMBAT_DESIGN.md'
LOG = r'C:\Project\UxGame\.workbuddy\memory\2026-09-28.md'

DOC_SECTION = """
#### 批次 J：粒子 Clip 位姿与编辑期预览一致（已完成）

批次 I 把特效接上了，但位姿只能在 Hierarchy 里拖美术对象 —— 改的是美术资产，Timeline 里没有任何记录。
本批次把位姿变成 Clip 数据，并让编辑期预览走与运行时同一条绑定路径。

**位姿是偏移，不是覆盖。** `ParticleClipAsset` 新增 `positionOffset`（加到 `localPosition`）、
`rotationEuler`（叠乘到 `localRotation`）、`scaleFactor`（乘到 `localScale`）。选偏移语义有两个直接好处：
美术在模型上摆的位置、朝向、大小全部保留，Timeline 只做增量；零值恰好就是"不动"，所以旧资产缺这三个
字段时反序列化出来的全零值正是正确默认，不需要迁移。`scaleFactor` 是唯一的例外 —— 它的零值在相乘语义下
会把特效缩成不可见，因此 `TLParticleClip` 读取时对 `<= 0` 回落为 1。

**覆盖与还原必须成栈。** 绑定的那个 `ParticleSystem` 挂在 `TimelineComponent` 的组件级绑定表上，同一单位
的 Base / Action 各层会先后占用它。所以 Clip 退出时不能无条件还原原值：先比较当前 Transform 是否仍等于
自己写下的值，不等于就说明已被别人覆盖，此时撤销会破坏正在播放的那个 Clip。反向退出（先进入者先退出）
会留下一个未撤销的值，但粒子此时已被 `Stop(StopEmittingAndClear)` 清空，且下一次任意 Clip 激活都会重新
覆盖，所以不可见也无害。`OnStart` 必须复位 `_hasPose`：Clip 是池化的，上一个持有者的状态不能带过来。

**编辑期与运行时不再分叉。** 此前 `TimelineWindow` 只靠 `AutoBindMissingTracks` 去模型上找一个
`ParticleSystem`，模型没有就什么都看不到，而运行时有 `CombatVfxHost` 兜底 —— 两边行为不一致。
现在预览侧新增 `ResolvePreviewVfx()`：同样经 `CombatVfxHost.Ensure`、同样设成预览副本的 hideFlags
（不落进场景文件），并在 `RefreshEntity` 与 `AutoBindMissingTracks` 两处接入。占位宿主建在预览副本下，
因此它在 Hierarchy 里可见 —— 这正是调位姿时可以参照的对象。

**Inspector。** `ParticleClipInspector` 增加位置偏移 / 旋转偏移 / 缩放倍率三个字段，改动即
`RecordUndo` + `CommitChange` + `RefreshEntity`。位姿在 Clip 激活时才写入，必须让预览重播才看得到。

**资产。** 三段粒子 Clip 用互不相同的位姿做肉眼区分：一段零偏移（同时充当"旧资产默认行为不变"的对照）、
二段右移上抬并转 45°、三段左移前抬并放大到 2 倍。
"""

LOG_SECTION = """
## 粒子 Clip 位姿 + 编辑期预览一致（22:00）

用户问"特效无法在 timeline 可视化吗？例如调整位置旋转这种"。查清后确认：**编辑期可视化本身是通的**
（Instantiate 预览副本 → `AutoBindMissingTracks` 自动绑粒子 → 拖标尺 `Simulate` 重建），但位姿完全不在
Timeline 数据里，只能去 Hierarchy 拖美术对象（改的是美术资产）。用户选择"位姿写进粒子 Clip"。

改动：

- `ParticleClipAsset` 加 `positionOffset` / `rotationEuler` / `scaleFactor`（偏移语义，零值=不动，旧资产行为不变）
- `TLParticleClip` 加 `PoseSnapshot`：`OnEnable` 应用、`OnDisable` 栈式还原（只撤自己写下的值）、`OnStart` 复位 `_hasPose`（池化复用）
- `ParticleClipInspector` 加位置偏移 / 旋转偏移 / 缩放倍率三个字段，改动即 `RefreshEntity` 让预览重播
- `TimelineWindow` 新增 `ResolvePreviewVfx()`，`RefreshEntity` 与 `AutoBindMissingTracks` 两处都接上，编辑期与运行时统一走 `CombatVfxHost.Ensure`
- 三条 Timeline 资产补位姿：一段零偏移（对照）／二段右移上抬转 45°／三段左移前抬放大 2 倍

**踩到两个坑**：

1. `scaleFactor` 的字段初始化器 `= 1f` 对**旧资产不生效** —— Unity 反序列化缺字段时填 `default`(0)，
   相乘会把特效缩成不可见。改成读取时 `<= 0` 回落 1。`positionOffset`/`rotationEuler` 因为零值在加/乘下
   本来就等于"不动"才侥幸安全。
2. **`core.autocrlf=true` ⇒ `git diff --stat` 测不出行尾翻转**。上一轮把既有资产
   `Data/Res/Timeline/HeroZS/HeroZSAttack01.asset` 从 CRLF 洗成了 LF，而 `git diff --numstat` 显示
   21/0 纯插入，完全看不出来；靠 `.workbuddy/tmp/backup_combo/` 的备份才发现。已用
   `git ls-files --eol` 核对并恢复 CRLF，`ParticleClipAsset.cs` 按同目录惯例也恢复 CRLF。
   → 记忆里"行尾被翻则 diff 行数 = 文件总行数"是错的，已修正。

校验：`verify_pose.py`（资产值 / 字段顺序 / 缩进 + 代码 token + `_clipAsset.<字段>` 引用闭合）ALL OK；
`verify_combo.py` 回归 ALL OK；`verify_cs.py` 11 个文件 ALL OK。
"""


def append(path, section, eol, key):
    raw = io.open(path, 'rb').read()
    assert raw.count(b'\r\n') in (0, raw.count(b'\n')), '%s 行尾混用' % path
    actual = b'\r\n' if raw.count(b'\r\n') else b'\n'
    assert actual == eol, '%s 行尾是 %r，期望 %r' % (path, actual, eol)

    text = raw.decode('utf-8')
    assert key not in text, '%s 已包含 %r，拒绝重复追加' % (path, key)

    body = section.strip('\n') + '\n'
    if eol == b'\r\n':
        body = body.replace('\n', '\r\n')
    out = text.rstrip('\r\n') + eol.decode() + body
    io.open(path, 'w', encoding='utf-8', newline='').write(out)

    after = io.open(path, 'rb').read()
    print('OK   %s  +%d 字节  CRLF=%d LF=%d'
          % (path.rsplit('\\', 1)[-1], len(after) - len(raw), after.count(b'\r\n'), after.count(b'\n')))


append(DOC, DOC_SECTION, b'\r\n', '批次 J')
append(LOG, LOG_SECTION, b'\n', '批次 J')
