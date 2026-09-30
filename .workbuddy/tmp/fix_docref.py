# -*- coding: utf-8 -*-
"""修 COMBAT_DESIGN.md 里 Resolve 的行号引用（36 -> 实际行），并按 skill 第 8 步核验 token 真在该行。"""
import io, os, sys

ROOT = r'C:\Project\UxGame\Unity\Assets'
MD = os.path.join(ROOT, r'Editor\Combat\COMBAT_DESIGN.md')
PLAYER = os.path.join(ROOT, r'HotfixBase\Manager\Combat\Runtime\Presentation\CombatTimelinePlayer.cs')

# 先找出 Resolve 的真实行号
with io.open(PLAYER, 'r', encoding='utf-8', newline='') as f:
    plines = f.read().replace('\r\n', '\n').split('\n')
real = [i + 1 for i, ln in enumerate(plines) if 'public static CombatTimelinePlan Resolve(' in ln]
if len(real) != 1:
    print('ABORT: Resolve 命中 %d 行' % len(real)); sys.exit(1)
real_line = real[0]
print('Resolve 实际在 CombatTimelinePlayer.cs:%d' % real_line)
print('  该行内容: %s' % plines[real_line - 1].strip())

with io.open(MD, 'r', encoding='utf-8', newline='') as f:
    raw = f.read()
crlf, lf = raw.count('\r\n'), raw.count('\n')
eol = '\r\n' if crlf == lf else ('\n' if crlf == 0 else None)
if eol is None:
    print('ABORT: md 混行尾'); sys.exit(1)
text = raw.replace('\r\n', '\n')

OLD = '`Manager/Combat/Runtime/Presentation/CombatTimelinePlayer.cs:36`'
NEW = '`Manager/Combat/Runtime/Presentation/CombatTimelinePlayer.cs:%d`' % real_line
if text.count(OLD) != 1:
    print('ABORT: md 引用命中 %d 次' % text.count(OLD)); sys.exit(1)
text = text.replace(OLD, NEW)

with io.open(MD, 'w', encoding='utf-8', newline='') as f:
    f.write(text.replace('\n', eol))

# 落盘后回读核验
with io.open(MD, 'r', encoding='utf-8', newline='') as f:
    chk = f.read()
line22 = chk.replace('\r\n', '\n').split('\n')[21]
print('md:22 -> %s' % line22)
print('token 命中该行: %s' % ('CombatTimelineResolver.Resolve' in line22))
print('行尾: crlf=%d lf=%d' % (chk.count('\r\n'), chk.count('\n')))
