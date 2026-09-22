import io, os

p = r'C:\Project\UxGame\.workbuddy\review\StateLayer-Action-removal-review.md'
raw = open(p, 'rb').read()
text = raw.decode('utf-8')
crlf = text.count('\r\n')
lf = text.count('\n')
nl = '\r\n' if crlf == lf else '\n'
print('detect: CRLF=%d LF=%d -> using %r' % (crlf, lf, nl))

tail = '*审核方式：只读。审核方未修改工作区任何文件。*'
assert text.count(tail) == 1, 'tail hit=%d' % text.count(tail)
assert text.endswith(nl), 'no trailing newline'

receipt = nl.join([
    '',
    '---',
    '',
    '## 6. 修复回执（2026-09-21 15:10）',
    '',
    'P1 / P2 / P3 / P4 已全部处理；另修 **4 处失效行号** + **1 处措辞歧义**。',
    '',
    '| 项 | 处理 | 落点 |',
    '|---|---|---|',
    '| **P1** | 恢复 `TryCancel` 的 ⚠ 坑注释（压缩为 2 行）；删掉复述 `if` 的注释与复述 `for` 的注释 | `CombatActionRunner.cs:578-584` |',
    '| **P1** | `AdvanceTo` 里那行空白直接删掉 —— 原句与方法自身 doc（`:520-526`"累加到 DurationFrames 就自动 Completed"）完全重复，留着是噪声 | `CombatActionRunner.cs:549` |',
    '| **P2** | 清掉 3 处行尾空白 / 空白行 | `CombatActionRunner.cs:248 / 549 / 592` 附近 |',
    '| **P2** | 清掉 4 处行尾空白，并补回被误删的空行分隔 | `CombatController.cs:88-98` |',
    '| **P3** | tick doc 的"原因见第 3 步"改为直述原因（步骤编号随重写一起消失了，编号引用已失效） | `CombatController.cs:74-75` |',
    '| **P4** | 恢复 `StatePresentationValidationUsesTimelineOrProfileContext`：`Action/Executing` → `Control/Normal`（同一"不允许映射"分支），消息过滤改 `"不是允许配置表现映射"` | `CombatEditorWorkflowTests.cs:551-600` |',
    '| 追加 | 文档行号全量核验，修 4 处失效：`CombatController.cs:149→138`、`CombatActionRunner.cs:76→131`、`:212→16`、`:285→517` | `COMBAT_DESIGN.md:20 / 82 / 231 / 407` |',
    '| 追加 | "停掉 Action 层" → "停掉 Action 播放层"（`StateLayer.Action` 已删，原文与 `TimelinePlaybackLayer.Action` 同名易混） | `COMBAT_DESIGN.md:22` |',
    '',
    '验证：',
    '',
    '| 检查 | 结果 |',
    '|---|---|',
    '| `git diff --check` | 空输出（无行尾空白、无空白行） |',
    '| 4 个改动文件行尾 | 全 CRLF，`CRLF == LF` |',
    '| `StateLayer.Action` / `ActionState` / `SyncAction` / `UnitStateMachine` 全仓 | 0 命中 |',
    '| `profile.FrameRate` / `TimelineAsset.SetFrameRate` / `CombatValidationIssue.Context` | 签名均已存在，测试调用合法 |',
    '| 编译 | **未验证**（本环境无法编译 C#，源码改动全部是注释/空白/测试恢复） |',
    '',
    '仍留着、本次刻意未动的一项：`CombatActionRunner.Tick` 里还剩 `// 0.` / `// 1.` 两个序号注释，',
    '而 `2.` / `3.` 已被改成无序号散文 —— 序号体系只剩一半。属排版一致性，不影响语义。',
    '',
    '> 本节由后续修复方写入，前 5 节仍是只读审核结论。',
    '',
])

open(p, 'wb').write((text + receipt).encode('utf-8'))
b = open(p, 'rb').read()
print('after: CRLF=%d LF=%d %s' % (b.count(b'\r\n'), b.count(b'\n'),
                                   'OK' if b.count(b'\r\n') == b.count(b'\n') else '*** MIXED ***'))
