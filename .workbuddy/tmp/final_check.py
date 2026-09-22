import os, re, subprocess

UN = r'C:\Project\UxGame\Unity'
A = os.path.join(UN, 'Assets')

files = [
    r'HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs',
    r'HotfixBase\Manager\Combat\Runtime\Unit\CombatController.cs',
    r'Editor\Combat\Tests\CombatEditorWorkflowTests.cs',
]

print('=== 括号平衡（与 HEAD 对比）===')
for f in files:
    cur = open(os.path.join(A, f), 'rb').read().decode('utf-8')
    rel = 'Unity/Assets/' + f.replace('\\', '/')
    p = subprocess.run(['git', 'show', 'HEAD:' + rel], cwd=UN,
                       capture_output=True, text=True, encoding='utf-8', errors='replace')
    head = p.stdout
    def bal(t):
        return (t.count('{') - t.count('}'), t.count('(') - t.count(')'))
    cb, cp = bal(cur)
    hb, hp = bal(head)
    print('  %-32s cur=%s head=%s %s' % (os.path.basename(f), (cb, cp), (hb, hp),
                                         'OK' if (cb, cp) == (hb, hp) else '*** DIFF ***'))

print()
print('=== 残留扫描（Assets 下 .cs/.md，排除 ThirdParty）===')
pats = ['StateLayer.Action', 'ActionState', 'SyncAction', 'UnitStateMachine',
        'ResolveTimelineOwner', 'ActionStarted', 'ActionEnded',
        'States.AdvanceTo', 'Actions.Tick', '第 4 步']
for pat in pats:
    hits = []
    for dp, dn, fn in os.walk(A):
        if 'ThirdParty' in dp:
            continue
        for f in fn:
            if not (f.endswith('.cs') or f.endswith('.md')):
                continue
            pth = os.path.join(dp, f)
            try:
                t = open(pth, encoding='utf-8', errors='replace').read()
            except Exception:
                continue
            for i, ln in enumerate(t.split('\n'), 1):
                if pat in ln:
                    hits.append('%s:%d' % (os.path.relpath(pth, A), i))
    print('  %-24s %d hit(s) %s' % (pat, len(hits), hits[:4] if hits else ''))

print()
q = subprocess.run(['git', 'diff', '--check'], cwd=UN, capture_output=True, text=True,
                   encoding='utf-8', errors='replace')
print('=== git diff --check rc=%d ===' % q.returncode)
print('  ' + (q.stdout.strip() or '(clean)'))
