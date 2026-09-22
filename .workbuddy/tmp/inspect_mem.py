# -*- coding: utf-8 -*-
import io

P = r'C:\Project\UxGame\.workbuddy\memory\MEMORY.md'
with io.open(P, 'r', encoding='utf-8', newline='') as fh:
    raw = fh.read()
print('crlf=%d lf=%d' % (raw.count('\r\n'), raw.count('\n')))
lines = raw.replace('\r\n', '\n').split('\n')
for i in range(77, 82):
    print('--- line %d ---' % (i + 1))
    print(repr(lines[i]))
