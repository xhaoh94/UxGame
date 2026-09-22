import os, re
base = r'C:\Project\UxGame\Unity\Assets'
runner = os.path.join(base, r'HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs')
lines = open(runner, encoding='utf-8').read().split('\n')

targets = ['HashSet<string>', 'HasHitConfirmed', 'ActionChanged', '_acceptedHits']
for t in targets:
    print('--- %s ---' % t)
    for i, ln in enumerate(lines, 1):
        if t in ln:
            print('  %4d | %s' % (i, ln.strip()[:120]))
    print()
