import os, re

root = r'C:\Project\UxGame\Unity\Assets'

files = [
    r'HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs',
    r'HotfixBase\Manager\Combat\Runtime\Unit\CombatController.cs',
    r'HotfixBase\Manager\Combat\Runtime\Unit\CombatStateMachine.cs',
    r'Editor\Combat\COMBAT_DESIGN.md',
    r'Editor\Combat\CombatEditorUtility.cs',
    r'Editor\Combat\Tests\CombatEditorWorkflowTests.cs',
    r'Editor\Combat\Tests\CombatRuntimeTests.cs',
    r'Editor\Combat\Tests\BattleWorldTests.cs',
]

for f in files:
    p = os.path.join(root, f)
    if not os.path.exists(p):
        print('MISSING', f); continue
    raw = open(p, 'rb').read()
    crlf = raw.count(b'\r\n')
    lf = raw.count(b'\n')
    print('%-72s EOL=%-4s CRLF=%d LF=%d' % (f, 'CRLF' if crlf == lf else 'MIXED', crlf, lf))

print()
print('=== total lines ===')
for f in files:
    p = os.path.join(root, f)
    if os.path.exists(p):
        print('%-72s %d' % (f, open(p, 'rb').read().count(b'\n')))
