# -*- coding: utf-8 -*-
"""把本轮新学到的两条经验写回 skill：往返证明、git diff --output，以及两个新坑。"""
import io, os, sys

P = r'C:\Users\xhaoh\.workbuddy\skills\unity-csharp-safe-refactor\SKILL.md'
with io.open(P, 'r', encoding='utf-8', newline='') as f:
    raw = f.read()
crlf, lf = raw.count('\r\n'), raw.count('\n')
eol = '\r\n' if crlf == lf else ('\n' if crlf == 0 else None)
if eol is None:
    print('ABORT: 混行尾 crlf=%d lf=%d' % (crlf, lf)); sys.exit(1)
print('skill 行尾: %s' % ('CRLF' if eol == '\r\n' else 'LF'))
text = raw.replace('\r\n', '\n')

RULES = []

# 1) 第 5 步加"往返证明"
RULES.append((
    'retitle',
    '### 5. 验证（五件事，缺一不可）',
    '### 5. 验证（六件事，缺一不可）',
))
RULES.append((
    'roundtrip',
    '''5. **守卫分析**（删了 null 兜底时必须做）：新写法的每个访问点，是否都在 `if (X?.Y != true) return;` 之类早退之后？若在 `?.` 链上，写 `A?.B?.C()` 与旧的 `b?.C()`（其中 `b => A?.B`）是**语义等价**的，可以放心；若在守卫之后，写 `A.B.C` 更诚实（真为 null 时应大声抛错，而不是被 `?.` 静默吞掉）
''',
    '''5. **守卫分析**（删了 null 兜底时必须做）：新写法的每个访问点，是否都在 `if (X?.Y != true) return;` 之类早退之后？若在 `?.` 链上，写 `A?.B?.C()` 与旧的 `b?.C()`（其中 `b => A?.B`）是**语义等价**的，可以放心；若在守卫之后，写 `A.B.C` 更诚实（真为 null 时应大声抛错，而不是被 `?.` 静默吞掉）
6. **往返证明**（忘了先备份时也照样能做，强烈推荐）：把规则**反向**应用一次得到"改动前"的文本，落成回滚副本；再**正向**应用一次，断言与当前落盘内容**逐字节相同**。相等 ⇒ 这次改动**就是规则本身**，文件里没有第二处被动过。比人工读 diff 强：diff 只能证明"你看到的都合理"，往返能证明"没有你没看到的"。

   ```python
   old = current
   for name, o, n in RULES:
       assert old.count(n) == 1
       old = old.replace(n, o)              # 反推旧版本
   roundtrip = old
   for name, o, n in RULES:
       assert roundtrip.count(o) == 1
       roundtrip = roundtrip.replace(o, n)
   assert roundtrip == current              # 逐字节相同
   ```

   前提是每条规则**可逆且不重叠**（旧文本在反推结果里仍然唯一）。整文件重写（如测试文件）反推不了，就把手写的旧版本贴进脚本当 `forced_old`。
''',
))

# 2) 第 6 步补 --output 的做法
RULES.append((
    'diff-output',
    '''⚠ **别用 PowerShell 管道抓 `git diff` 再读**：`git diff | Out-File -Encoding utf8` 会用控制台代码页（中文 Windows 上是 GBK）解码 git 输出的 UTF-8 字节，结果中文全是 `鈫?` 这种乱码，而且**还会吃掉行尾的换行**，把两行 source 粘成一行 —— 看起来像"注释被改坏了"，其实是采集环节的失真。要看中文就 `Read` 源文件本身，或走第 5 步的非 ASCII 投影比对。
''',
    '''⚠ **别用 PowerShell 管道抓 `git diff` 再读**：`git diff | Out-File -Encoding utf8` 会用控制台代码页（中文 Windows 上是 GBK）解码 git 输出的 UTF-8 字节，结果中文全是 `鈫?` 这种乱码，而且**还会吃掉行尾的换行**，把两行 source 粘成一行 —— 看起来像"注释被改坏了"，其实是采集环节的失真。

**要落盘再 `Read`，用 `git diff --output=<文件>`** —— 由 git 自己按 UTF-8 写文件，完全绕开 PowerShell 的解码与换行处理（中文正常，换行也正常）。同理 `git diff --check --output=<文件>`：它只输出"行尾空白"这类纯 ASCII 报告，落盘后**看文件长度是否为 0** 就能判定，比读输出稳。
''',
))

# 3) 坑：删死代码会孤立字段
RULES.append((
    'orphan-field',
    '''- 改完提醒用户**在 Unity 里过一遍编译**；''',
    '''- **删掉一段死代码后，回头扫一遍它是不是某个字段/方法的唯一读者。** 实测：删掉 `RefreshTimeline` 里"算了但没人要"的返回值后，`_framePlanInitialized` 立刻变成只写字段（声明 + 3 处赋值、零读者）—— 编译器只给 CS0414 警告，不报错，于是它就永久留在那儿。同类：只被删掉那段代码调用的私有方法、只在那段里读的属性。判断方法：拿被删代码里出现的每个标识符，在改动后的文件里重新数一遍出现次数。
- **替换哨兵值时，先确认旧写法的"未设置"态是否恰好等于某个真实值。** 实测：旧的哨兵是 `string.Empty`，而"空 selection"的 `OwnerKey` 是 `null`（`default` 结构体的默认值），二者**不相等**，于是每次"从空到空"的比较都为真 → 每帧白走一次 `StopLayer`。换成结构化 `default` 后这个副作用消失。这类差异属于"行为等价性"里唯一需要显式记录的一处，必须写进回执，别当成零影响。
- 改完提醒用户**在 Unity 里过一遍编译**；''',
))

for name, old, new in RULES:
    n = text.count(old)
    if n != 1:
        print('ABORT [%s]: 命中 %d 次' % (name, n)); sys.exit(1)
    text = text.replace(old, new)

with io.open(P, 'w', encoding='utf-8', newline='') as f:
    f.write(text.replace('\n', eol))

chk = io.open(P, 'r', encoding='utf-8', newline='').read()
print('OK  skill 已更新：%d -> %d 字节, crlf=%d lf=%d' % (
    len(raw.encode('utf-8')), len(chk.encode('utf-8')), chk.count('\r\n'), chk.count('\n')))
