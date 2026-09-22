import os, re

base = r'C:\Project\UxGame\Unity\Assets'
doc = os.path.join(base, r'Editor\Combat\COMBAT_DESIGN.md')
md = open(doc, encoding='utf-8').read()

# 找 `Xxx.cs:123` 后面紧跟的 `Token`
pat = re.compile(r'`([A-Za-z0-9_/\.]+\.cs):(\d+)`\s*`([^`]+)`')
hits = list(pat.finditer(md))

# 建立文件名 -> 路径索引
index = {}
for dp, dn, fn in os.walk(base):
    for f in fn:
        if f.endswith('.cs'):
            index.setdefault(f, []).append(os.path.join(dp, f))

print('=== 行号引用核验 ===')
for m in hits:
    fname, line, token = m.group(1), int(m.group(2)), m.group(3)
    short = os.path.basename(fname)
    paths = index.get(short, [])
    if not paths:
        print('%-40s NO-FILE' % short); continue
    p = paths[0]
    lines = open(p, encoding='utf-8', errors='replace').read().split('\n')
    tok = token.split(' ')[0].strip('`')
    found = None
    if 1 <= line <= len(lines):
        cur = lines[line - 1]
        if tok in cur:
            found = 'OK'
        else:
            # 找真实行号
            real = [i + 1 for i, ln in enumerate(lines) if tok in ln]
            found = 'MISMATCH real=%s   doc_line=%r' % (real, cur.strip()[:80])
    else:
        found = 'OUT-OF-RANGE (file has %d lines)' % len(lines)
    # 上下文里的 md 行号
    mdline = md[:m.start()].count('\n') + 1
    print('md:%-4d %-26s :%-5d %-22s -> %s' % (mdline, short, line, tok, found))
