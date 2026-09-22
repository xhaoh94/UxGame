import os, subprocess

R = r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs'
C = r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Unit\CombatController.cs'
M = r'C:\Project\UxGame\Unity\Assets\Editor\Combat\COMBAT_DESIGN.md'
T = r'C:\Project\UxGame\Unity\Assets\Editor\Combat\Tests\CombatEditorWorkflowTests.cs'

def rd(p):
    return open(p, 'rb').read().decode('utf-8')

print('=== EOL ===')
for p in (R, C, M, T):
    raw = open(p, 'rb').read()
    crlf, lf = raw.count(b'\r\n'), raw.count(b'\n')
    print('  %-34s lines=%-5d CRLF=%-5d LF=%-5d %s' % (
        os.path.basename(p), lf, crlf, lf, 'OK' if crlf == lf else '*** MIXED ***'))

print()
print('=== 引号字符检查（U+201C/U+201D 出现行）===')
for p in (R, C):
    lines = rd(p).split('\r\n')
    hits = [(i, ln.strip()[:60]) for i, ln in enumerate(lines, 1)
            if '\u201c' in ln or '\u201d' in ln]
    print('  %s: %d line(s) with full-width quotes' % (os.path.basename(p), len(hits)))
    for i, s in hits[:5]:
        print('     %4d | %s' % (i, s))

print()
print('=== CombatController.Tick ===')
lines = rd(C).split('\r\n')
for i in range(59, 103):
    print('  %4d | %s' % (i, lines[i - 1]))

print()
print('=== 恢复的测试用例 ===')
tl = rd(T).split('\r\n')
start = next(i for i, ln in enumerate(tl, 1) if 'StatePresentationValidationUsesTimelineOrProfileContext' in ln)
for i in range(start - 4, start + 48):
    print('  %4d | %s' % (i, tl[i - 1]))

print()
os.chdir(r'C:\Project\UxGame\Unity')
p = subprocess.run(['git', 'diff', '--check'], capture_output=True, text=True, encoding='utf-8', errors='replace')
print('=== git diff --check rc=%d ===' % p.returncode)
print(p.stdout if p.stdout.strip() else '  (clean)')
