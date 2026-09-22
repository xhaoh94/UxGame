import os
bk = r'C:\Project\UxGame\.workbuddy\tmp\backup\CombatController.cs'
cur = r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Unit\CombatController.cs'

def line(path, n):
    return open(path, 'rb').read().decode('utf-8').split('\r\n')[n - 1]

for p, tag in ((bk, 'BACKUP'), (cur, 'CURRENT')):
    ln = line(p, 74)
    codes = [(ch, hex(ord(ch))) for ch in ln if ord(ch) > 127 and ch in '“”"']
    print('%-8s L74: %s' % (tag, ln))
    print('         quotes:', codes)
    print()

# 打印当前 74 行的全部非 ASCII 字符码点
ln = line(cur, 74)
print('CURRENT L74 codepoints >0x2000:')
for ch in ln:
    if ord(ch) > 0x2000:
        print('   %r U+%04X' % (ch, ord(ch)))
