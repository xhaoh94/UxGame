import subprocess, os, sys, re

os.chdir(r'C:\Project\UxGame\Unity')

def run(args):
    p = subprocess.run(args, capture_output=True, text=True, encoding='utf-8', errors='replace')
    return p.returncode, p.stdout, p.stderr

rc, out, err = run(['git', 'diff', '--check'])
print('=== git diff --check rc=%d ===' % rc)
print(out)
print(err)

rc, out, err = run(['git', 'status', '--porcelain'])
print('=== git status --porcelain ===')
print(out)

rc, out, err = run(['git', 'diff', '--stat'])
print('=== git diff --stat ===')
print(out)
