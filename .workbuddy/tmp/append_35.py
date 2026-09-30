import io

PATH = r'C:\Project\UxGame\.workbuddy\memory\2026-09-28.md'

with io.open(PATH, 'r', encoding='utf-8', newline='') as f:
    raw = f.read()

crlf = raw.count('\r\n')
lf = raw.count('\n')
eol = '\r\n' if (crlf > 0 and crlf == lf) else '\n'
print('BEFORE crlf=%d lf=%d bytes=%d eol=%r' % (crlf, lf, len(raw), eol))

block = eol.join([
    '',
    '### 命令层是三种模式共用的入口（补记）',
    '',
    '- `CombatCommandBuffer` 类头注释原文：「按逻辑帧保存输入命令，供本地模拟、网络同步和**录像**共用」。',
    '- `CombatComponent.EnqueueCommand(in CombatCommand)` 是 public，接收**外部带帧号**的命令（可"未来帧"），`:113` 注释写明调用点在「按键 / 收包回调里」；`RequestAction` 只是本地那条便利路径（帧号 = `CurrentFrame + 1`）。',
    '- 所以 `RequestId` / `IsPredicted` / `Confirm` / `Reject` / `DiscardBefore`（注释：回滚后旧命令必须扔掉）这套并非单机残留，而是为网络与录像预留的同一套入口 —— 现在只跑 `LocalRealtime`。',
    '- 结论：`ComputeStateHash` 的天然消费者是"客户端自己也重算逻辑"的那两条路（帧同步对照 / 预测回滚自检）；**后端下发状态流那种录像用不上它**（客户端没有重算动作，也就没有重算发散）。防作弊不在它的能力范围内。',
    '',
])

if not raw.endswith(eol):
    block = eol + block

out = raw + block
with io.open(PATH, 'w', encoding='utf-8', newline='') as f:
    f.write(out)

with io.open(PATH, 'r', encoding='utf-8', newline='') as f:
    chk = f.read()
print('AFTER  crlf=%d lf=%d bytes=%d' % (chk.count('\r\n'), chk.count('\n'), len(chk)))
print('PREFIX_PRESERVED=%s EOL_UNCHANGED=%s' % (chk.startswith(raw), chk.count('\r\n') == crlf))
