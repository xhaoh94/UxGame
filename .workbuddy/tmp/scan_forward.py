"""扫描 Combat 模块里 'prop => _field;' 形式的转发属性，并报告字段的修饰符与其余引用点。"""
import io
import os
import re

ROOTS = [
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat',
    r'C:\Project\UxGame\Unity\Assets\Hotfix\Common\Combat',
]

PROP = re.compile(
    r'(?P<mods>(?:public|internal|protected|private)\s+)*'
    r'(?P<type>[A-Za-z0-9_<>,\.\[\]\?]+)\s+'
    r'(?P<name>[A-Za-z0-9_]+)\s*=>\s*'
    r'(?P<field>_[A-Za-z0-9_]+)\s*;',
)

FIELD = re.compile(
    r'^\s*(?P<mods>(?:(?:public|internal|protected|private|readonly|static|const|volatile)\s+)+)'
    r'(?P<type>[A-Za-z0-9_<>,\.\[\]\?]+)\s+'
    r'(?P<name>_[A-Za-z0-9_]+)\s*(?P<init>=.*?)?;\s*$',
)

files = []
for root in ROOTS:
    for dp, dn, fn in os.walk(root):
        for f in fn:
            if f.endswith('.cs'):
                files.append(os.path.join(dp, f))

print('files scanned:', len(files))
print()

for path in sorted(files):
    with io.open(path, 'r', encoding='utf-8', newline='') as fh:
        text = fh.read()
    lines = text.replace('\r\n', '\n').split('\n')

    for m in PROP.finditer(text):
        field = m.group('field')
        name = m.group('name')
        line_no = text[:m.start()].count('\n') + 1
        decl = None
        for i, ln in enumerate(lines):
            fm = FIELD.match(ln)
            if fm and fm.group('name') == field:
                decl = (i + 1, ln.strip())
                break
        # 字段在属性行之后的所有引用
        refs = []
        nl = 0
        for i, ln in enumerate(lines):
            for m2 in re.finditer(r'(?<![A-Za-z0-9_])' + re.escape(field) + r'(?![A-Za-z0-9_])', ln):
                refs.append((i + 1, ln.strip()[:90]))
        print('%s:%d' % (path.split('\\')[-1], line_no))
        print('   prop  : %s %s => %s' % (m.group('type'), name, field))
        print('   field : %s' % (decl and ('%d | %s' % decl),))
        print('   refs(%d):' % len(refs))
        for r in refs:
            print('      %d | %s' % r)
        print()
