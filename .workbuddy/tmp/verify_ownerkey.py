# -*- coding: utf-8 -*-
"""静态校验：行尾（读原始字节，不做归一化）、残留词、括号净配对、关键调用点。"""
import io, os

ROOT = r'C:\Project\UxGame\Unity\Assets'
FILES = {
 'player': os.path.join(ROOT, r'HotfixBase\Manager\Combat\Runtime\Presentation\CombatTimelinePlayer.cs'),
 'component': os.path.join(ROOT, r'Hotfix\Common\Combat\CombatComponent.cs'),
 'window': os.path.join(ROOT, r'Editor\Timeline\TimelineWindow.cs'),
 'test': os.path.join(ROOT, r'Editor\Combat\Tests\CombatTimelinePlayerTests.cs'),
}

print('== 行尾（原始字节） ==')
for k, p in FILES.items():
    with io.open(p, 'r', encoding='utf-8', newline='') as f:
        raw = f.read()
    crlf, lf = raw.count('\r\n'), raw.count('\n')
    kind = 'CRLF' if crlf == lf else ('LF' if crlf == 0 else 'MIXED')
    print('%-10s crlf=%d lf=%d -> %s' % (k, crlf, lf, kind))

print()
print('== 残留词扫描（Assets 全量） ==')
DEAD = ['OwnerKey', '_framePlanInitialized', 'preview:action-only', 'preview:action:', 'state:{(int)']
hits = {t: [] for t in DEAD}
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if not f.endswith('.cs'):
            continue
        p = os.path.join(dp, f)
        try:
            with io.open(p, 'r', encoding='utf-8', newline='') as fh:
                body = fh.read()
        except UnicodeDecodeError:
            continue
        for i, line in enumerate(body.split('\n')):
            for t in DEAD:
                if t in line:
                    hits[t].append('%s:%d' % (os.path.relpath(p, ROOT), i + 1))
for t in DEAD:
    print('%-22s %d 处 %s' % (t, len(hits[t]), hits[t][:6]))

print()
print('== 括号净配对差（{ - } / ( - ) ） ==')
for k, p in FILES.items():
    with io.open(p, 'r', encoding='utf-8', newline='') as f:
        b = f.read()
    print('%-10s 花括号差=%d 圆括号差=%d' % (k, b.count('{') - b.count('}'), b.count('(') - b.count(')')))

print()
print('== 关键调用点 ==')
for k, p in FILES.items():
    with io.open(p, 'r', encoding='utf-8', newline='') as f:
        lines = f.read().split('\n')
    for i, line in enumerate(lines):
        if 'SameOwner' in line or 'new CombatTimelineSelection' in line or 'RefreshTimeline(' in line:
            print('%-10s %4d  %s' % (k, i + 1, line.strip()))
