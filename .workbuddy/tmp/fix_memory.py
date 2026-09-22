# -*- coding: utf-8 -*-
import io
import sys

P = r'C:\Project\UxGame\.workbuddy\memory\MEMORY.md'

with io.open(P, 'r', encoding='utf-8', newline='') as fh:
    raw = fh.read()
crlf, lf = raw.count('\r\n'), raw.count('\n')
if crlf == lf:
    eol = '\r\n'
elif crlf == 0:
    eol = '\n'
else:
    print('ABORT 混行尾'); sys.exit(1)
print('eol=%r crlf=%d lf=%d' % (eol, crlf, lf))
t = raw.replace('\r\n', '\n')


def sub(t, name, old, new):
    n = t.count(old)
    if n != 1:
        print('ABORT [%s]: 命中 %d 次' % (name, n)); sys.exit(1)
    return t.replace(old, new)


# 1) 修回被劈成两行的那句（反引号里本来要显示 CRLF 的转义写法）
old_broken = '`newline=\'\'` 读出的 `' + '\n' + '` 直写会改变行尾'
new_fixed = '`newline=\'\'` 读出的 `' + chr(92) + 'r' + chr(92) + 'n' + '` 直写会改变行尾'
t = sub(t, '修回归行尾说明', old_broken, new_fixed)

# 2) 代码风格约定：补自动属性规则
anchor2 = '- 参考量级：清理后 Combat 模块整体约 10%。单文件超过 20% 基本就是写多了。'
add2 = ('\n- **不要写"私有字段 + 转发属性"**（2026-09-21 用户要求把 Combat 模块全部改掉）：'
        '`public X Foo => _foo;` 直接写成 `public X Foo { get; private set; }`，省掉那个字段。'
        '能不能转看三条：字段是 `readonly` 的要用 `{ get; }`（写 `private set` 会丢掉 readonly 保证）；'
        '字段带 `[SerializeField]` 的不能转（会丢 Unity 序列化）；对外类型是只读接口、类内却要拿具体可变类型的'
        '（`ICombatEntity[]` / `readonly List<>` 暴露成 `IReadOnlyList<>`）也转不了 —— 只读接口没有可写索引器 / `Add` / `Clear`。'
        '另外 `=> _x.y`、`=> _x.Count`、索引器是计算/投影而不是转发，本来就没有 1:1 后置字段。')
t = sub(t, '补自动属性规则', anchor2, anchor2 + add2)

# 3) 工具坑：补 Python 转义坑
anchor3 = '**含正则或反斜杠的 Python 一律先 Write 成 `.py` 再执行**。'
add3 = ('\n- **Python 字符串里的转义序列会变成真字符**：往 md / 代码里追加"表示 CRLF 两字节的转义写法"时，'
        '直接写进普通字符串会被解释成真正的换行，把一行劈成两行 —— `MEMORY.md` 第 79 行就中过一次。'
        '要原样写入反斜杠，用 `chr(92)` 拼接，别依赖转义层数；写完用 `repr()` 单行回读确认。')
t = sub(t, '补 Python 转义坑', anchor3, anchor3 + add3)

with io.open(P, 'w', encoding='utf-8', newline='') as fh:
    fh.write(t.replace('\n', eol))

with io.open(P, 'r', encoding='utf-8', newline='') as fh:
    chk = fh.read()
print('写后 crlf=%d lf=%d' % (chk.count('\r\n'), chk.count('\n')))
for i, ln in enumerate(chk.replace('\r\n', '\n').split('\n')):
    if 'newline=' in ln or '转发属性' in ln:
        print('line %d: %s' % (i + 1, ln[:160]))
