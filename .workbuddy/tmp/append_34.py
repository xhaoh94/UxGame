import io, sys

PATH = r'C:\Project\UxGame\.workbuddy\memory\2026-09-28.md'

with io.open(PATH, 'r', encoding='utf-8', newline='') as f:
    raw = f.read()

crlf = raw.count('\r\n')
lf = raw.count('\n')
print('BEFORE crlf=%d lf=%d bytes=%d' % (crlf, lf, len(raw)))
eol = '\r\n' if crlf == lf and crlf > 0 else '\n'
print('eol=%r' % eol)

block = eol.join([
    '',
    '## ComputeStateHash 的接线现状（补记）',
    '',
    '- 生产调用者 **0 处**；唯一消费者是 `Editor/Combat/Tests/BattleWorldTests.cs`（7 个 `Assert`，验的是性质：同输入同值 / 换 `IsGrounded` 会变 / 命中确认后会变 / 快照往返后回到同值）。',
    '- 三个帧源模式只有 `LocalRealtime` 有调用者（`Hotfix/Modules/Scene/Scene.cs:36`）；`StartExternal` / `StartReplay` / `AdvanceExternalTo` **全仓零调用** —— 帧同步与录像的时钟入口是预留的。',
    '- 预测回滚骨架已有（`Confirm`/`Reject`、`CaptureSnapshot`/`RestoreSnapshot`），哈希是它的天然校验器，但没有任何路径把两者接起来。',
    '- 接线要补三件：① 采样点 —— `BattleWorld.Tick` 的追赶循环（`while (Frame < frame) { Frame++; TickFrame(Frame); }`）内没有任何钩子；② 传输/落盘 —— `ulong` 无序列化入口，录制格式无哈希槽位；③ 不一致处理 —— 无 mismatch 分支、无"回滚到上一确认帧"路径。',
    '- 边界：位置与朝向**刻意不进哈希**，所以单靠哈希校验不出位置发散。`COMBAT_DESIGN.md:43` 记了帧同步还需补定点数（当前 16 处浮点）。',
    '- 时机：`FnvOffsetBasis` / `FnvPrime` / 覆盖面现在改动零成本；一旦接上回放或联网比对就全部冻结（改值或缩覆盖面都会把历史记录判成发散）。',
    '',
])

if not raw.endswith(eol):
    block = eol + block

out = raw + block

with io.open(PATH, 'w', encoding='utf-8', newline='') as f:
    f.write(out)

# 校验：行尾未翻转、旧内容逐字节保留
with io.open(PATH, 'r', encoding='utf-8', newline='') as f:
    chk = f.read()
print('AFTER  crlf=%d lf=%d bytes=%d' % (chk.count('\r\n'), chk.count('\n'), len(chk)))
print('PREFIX_PRESERVED=%s' % chk.startswith(raw))
print('OK' if chk.startswith(raw) and chk.count('\r\n') == chk.count('\n') else 'FAIL')
