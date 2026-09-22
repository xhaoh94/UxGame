import os
base = r'C:\Project\UxGame\Unity\Assets'
runner = os.path.join(base, r'HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs')
ctrl = os.path.join(base, r'HotfixBase\Manager\Combat\Runtime\Unit\CombatController.cs')

def show(path, nums, name):
    lines = open(path, encoding='utf-8').read().split('\n')
    print('--- %s (total %d) ---' % (name, len(lines)))
    for n in nums:
        if 1 <= n <= len(lines):
            print('  %4d | %s' % (n, lines[n - 1].strip()[:110]))
        else:
            print('  %4d | <out of range>' % n)
    print()

show(runner, [76, 212, 285, 275, 301], 'CombatActionRunner.cs')
show(ctrl, [138, 149], 'CombatController.cs')
