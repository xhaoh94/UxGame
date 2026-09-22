import os, re, subprocess

os.chdir(r'C:\Project\UxGame\Unity')

# 1) 全模块扫描过期引用
pats = {
    'States.': re.compile(r'\bStates\.'),
    'Actions.': re.compile(r'\bActions\.'),
    'StateLayer.Action': re.compile(r'StateLayer\.Action'),
    'ActionState': re.compile(r'\bActionState\b'),
    'SyncAction': re.compile(r'\bSyncAction\b'),
    'UnitStateMachine': re.compile(r'UnitStateMachine'),
    '第4步/第 4 步': re.compile(r'第\s*4\s*步'),
    '第3步/第 3 步': re.compile(r'第\s*3\s*步'),
    '──': re.compile(r'──'),
}

roots = [r'Assets\Editor\Combat', r'Assets\HotfixBase\Manager\Combat', r'Assets\Hotfix\Common\Combat']
for root in roots:
    for dp, dn, fn in os.walk(root):
        for f in fn:
            if not (f.endswith('.cs') or f.endswith('.md')):
                continue
            p = os.path.join(dp, f)
            try:
                lines = open(p, encoding='utf-8').read().split('\n')
            except Exception as e:
                print('ERR', p, e); continue
            for i, ln in enumerate(lines, 1):
                for name, rx in pats.items():
                    for m in rx.finditer(ln):
                        if name == '──' and '──' not in ln:
                            continue
                        print('%s:%d [%s] %s' % (p.replace('\\', '/'), i, name, ln.strip()[:130]))

print()
print('=== trailing whitespace in modified files ===')
rc = subprocess.run(['git', 'diff', '--check'], capture_output=True, text=True, encoding='utf-8', errors='replace')
print(rc.stdout)
