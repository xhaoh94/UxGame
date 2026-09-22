"""校验：改动只涉及 ASCII，所有非 ASCII 字符（中文注释）必须逐字符不变。"""
import io
import os
import re
import sys

BK = r'C:\Project\UxGame\.workbuddy\tmp\backup_autoprops'
TARGETS = [
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Unit\AttributeSet.cs',
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Core\CombatStageBuffers.cs',
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs',
    r'C:\Project\UxGame\Unity\Assets\Hotfix\Common\Combat\CombatComponent.cs',
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Unit\CombatController.cs',
]


def read(path):
    with io.open(path, 'r', encoding='utf-8', newline='') as fh:
        raw = fh.read()
    return raw


fail = 0
for cur in TARGETS:
    name = os.path.basename(cur)
    old = read(os.path.join(BK, name))
    new = read(cur)

    def eol_info(s):
        crlf = s.count('\r\n')
        lf = s.count('\n')
        kind = 'CRLF' if crlf == lf else ('LF' if crlf == 0 else 'MIXED')
        return '%s(crlf=%d lf=%d)' % (kind, crlf, lf)

    o_old = ''.join(c for c in old if ord(c) > 127)
    o_new = ''.join(c for c in new if ord(c) > 127)
    same = o_old == o_new
    if not same:
        fail = 1
    print('%-26s 行尾 %-26s 非ASCII %s (old=%d new=%d)' % (
        name, eol_info(new), 'OK' if same else '!!! 不一致', len(o_old), len(o_new)))

    # 逐行比对：列出所有"非纯 ASCII 差异"的行
    ol = old.replace('\r\n', '\n').split('\n')
    nl = new.replace('\r\n', '\n').split('\n')
    if len(ol) != len(nl):
        print('    行数 %d -> %d' % (len(ol), len(nl)))
    for ln in nl:
        if re.search(r'(?<![A-Za-z0-9_.])_[a-z][A-Za-z0-9_]*', ln) and '=>' in ln:
            print('    ?? 疑似残留转发属性: %s' % ln.strip())

print()
print('RESULT: %s' % ('FAIL' if fail else 'ALL OK'))
sys.exit(fail)
