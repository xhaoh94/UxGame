import os

p = r'C:\Project\UxGame\.workbuddy\memory\2026-09-21.md'
text = open(p, 'rb').read().decode('utf-8')
crlf, lf = text.count('\r\n'), text.count('\n')
nl = '\r\n' if crlf == lf and crlf > 0 else '\n'
print('detect CRLF=%d LF=%d -> %r' % (crlf, lf, nl))

body = nl.join([
    '',
    '## 收尾：P1 / P2 / P4 一并修掉（用户："一并处理吧"）',
    '',
    '上轮审核留的 3 项全部处理，另**追加**修了 4 处文档行号失效 + 1 处措辞歧义。改动共 5 个文件。',
    '',
    '| 项 | 处理 | 落点 |',
    '|---|---|---|',
    '| P1 | 恢复 `TryCancel` 的 ⚠ 坑注释（压缩成 2 行）；删掉复述 `if` 的注释、复述 `for` 的注释 | `CombatActionRunner.cs:578-584` |',
    '| P1 | `AdvanceTo` 那行空白**直接删**（原句与方法自身 doc 重复，不是"唯一线索"） | `CombatActionRunner.cs:549` |',
    '| P2 | 清 3 处行尾空白/空白行，并把 `}` 与 `foreach` 之间被误删的**空行补回** | `CombatActionRunner.cs:248 / 549 / 592` |',
    '| P2 | 清 4 处行尾空白，补回 `ActionRunner.Tick` 与 `if (!_grounded)` 之间的空行 | `CombatController.cs:84-100` |',
    '| P3 | doc 里"原因见第 3 步"改为**直述原因**（步骤编号随重写一起消失，编号引用已彻底失效） | `CombatController.cs:74-75` |',
    '| P4 | 恢复 `StatePresentationValidationUsesTimelineOrProfileContext`：`Action/Executing` → `Control/Normal`，消息过滤 `"Action 层"` → `"不是允许配置表现映射"` | `CombatEditorWorkflowTests.cs:551-600` |',
    '| 追加 | 文档行号全量核验（脚本抽 `` `X.cs:NNN` `` + token 逐一验），修 4 处失效 | `COMBAT_DESIGN.md:20 / 82 / 231 / 407` |',
    '| 追加 | 「停掉 Action 层」→「停掉 Action **播放**层」（`StateLayer.Action` 已删，原文与 `TimelinePlaybackLayer.Action` 同名易混） | `COMBAT_DESIGN.md:22` |',
    '',
    '**新增工具方法（值得复用）**：文档行号引用核验脚本 —— 用正则抽 `` `([\\w/]+\\.cs):(\\d+)`\\s*`([^`]+)` ``，按 basename 建索引找真实文件，检查该 token 是否真的出现在该行；不匹配就 grep 出真实行号。这次一次抓出 4 处漂移（`149→138`、`76→131`、`212→16`、`285→517`），全是改名前后的历史欠账。',
    '',
    '**验证结果**：',
    '',
    '| 检查 | 结果 |',
    '|---|---|',
    '| `git diff --check` | 空输出（前一轮有 7 条 trailing whitespace） |',
    '| 4 个改动文件行尾 | 全 CRLF，`CRLF == LF`（逐文件原始字节统计） |',
    '| `StateLayer.Action` / `ActionState` / `SyncAction` / `UnitStateMachine` 全仓 | 0 命中 |',
    '| `profile.FrameRate` / `TimelineAsset.SetFrameRate` / `CombatValidationIssue.Context` | 签名均已存在，恢复的测试调用合法 |',
    '| 编译 | **未验证**（本环境不能编 C#） |',
    '',
    '**本轮踩的坑（新）**：`open(path, encoding=\"utf-8\").read()` 会把 `\\r\\n` 归一成 `\\n`，于是"逐行改写再写回"这类脚本里 `crlf == lf` 断言**必然失败**。要保留行尾必须 `open(path, \'rb\')` 后 `decode`。第一次跑补丁时正是这样 —— 前两个 `.cs` 文件已落盘、第三个 `.md` 断言炸掉，结果脚本半途中断（.cs 成功、.md/测试未跑）。**补丁脚本要按文件分段、可单独重跑**，否则重跑整脚本会按旧行号二次改坏已改过的文件。',
    '',
    '**刻意未动的一项**：`CombatActionRunner.Tick` 里还剩 `// 0.`（`:237`）与 `// 1.`（`:242`）两个序号注释，而原 `2.` / `3.` 已被改成无序号散文 —— 序号体系只剩一半。属排版一致性，用户偏好最小侵入，先留着并已在审核文档 §6 记明。',
    '',
])

open(p, 'wb').write((text + body).encode('utf-8'))
b = open(p, 'rb').read()
print('after: CRLF=%d LF=%d' % (b.count(b'\r\n'), b.count(b'\n')))
