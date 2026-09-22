import os

BASE = r'C:\Project\UxGame\Unity\Assets'
MD   = os.path.join(BASE, r'Editor\Combat\COMBAT_DESIGN.md')
TEST = os.path.join(BASE, r'Editor\Combat\Tests\CombatEditorWorkflowTests.cs')

def rd(path):
    return open(path, 'rb').read().decode('utf-8')

# ---------- dry run: 确认所有锚点命中数 ----------
md = rd(MD)
subs = [
    ('`CombatController.cs:149`', '`CombatController.cs:138`', 1),
    ('`CombatActionRunner.cs:76`', '`CombatActionRunner.cs:131`', 1),
    ('`CombatActionRunner.cs:212`', '`CombatActionRunner.cs:16`', 1),
    ('`CombatActionRunner.Clear()` 会置空 `ActionChanged`（`:285`）',
     '`CombatActionRunner.Clear()` 会置空 `ActionChanged`（`:517`）', 1),
    ('Life/Control 命中时独占并停掉 Action 层',
     'Life/Control 命中时独占并停掉 Action 播放层', 1),
]
print('=== MD dry run ===')
ok = True
for old, new, expect in subs:
    c = md.count(old)
    flag = 'OK' if c == expect else '*** MISMATCH ***'
    if c != expect:
        ok = False
    print('  hit=%d expect=%d %s | %s' % (c, expect, flag, old[:60]))

t = rd(TEST)
anchor = ('        [Test]\r\n'
          '        public void SameStateCanKeepMultiplePresentationVariants()')
c = t.count(anchor)
print('=== TEST dry run ===')
print('  anchor hit=%d expect=1 %s' % (c, 'OK' if c == 1 else '*** MISMATCH ***'))
if c != 1:
    ok = False

print('EOL md: CRLF=%d LF=%d' % (md.count('\r\n'), md.count('\n')))
print('EOL test: CRLF=%d LF=%d' % (t.count('\r\n'), t.count('\n')))
print('DRY_RUN_OK' if ok else 'DRY_RUN_FAILED')
