import io, os, re

ROOT = r'C:\Project\UxGame\Unity\Assets\Data\Res\Timeline\HeroZS'

# 三段用不同位姿，肉眼可辨是第几段；一段保持 0 偏移，兼作"旧资产默认行为不变"的对照。
POSE = {
    'HeroZSAttack01.asset': ('{x: 0, y: 0, z: 0}',        '{x: 0, y: 0, z: 0}',  '1'),
    'HeroZSAttack02.asset': ('{x: 0.25, y: 0.15, z: 0}',  '{x: 0, y: 45, z: 0}', '1.4'),
    'HeroZSAttack03.asset': ('{x: -0.3, y: 0.3, z: 0.4}', '{x: 0, y: 0, z: 30}', '2'),
}

KEY = 'positionOffset'

for name, (pos, rot, scale) in POSE.items():
    path = os.path.join(ROOT, name)
    raw = io.open(path, 'rb').read()
    assert raw.count(b'\r\n') == 0, '%s 不是纯 LF' % name
    text = raw.decode('utf-8')

    if KEY in text:
        print('SKIP ', name, '（已写入）')
        continue

    hits = list(re.finditer(r'^([ \t]*)startColor:.*$', text, re.M))
    assert len(hits) == 1, '%s 的 startColor 出现 %d 次' % (name, len(hits))

    m = hits[0]
    indent = m.group(1)
    block = '\n'.join([
        m.group(0),
        indent + KEY + ': ' + pos,
        indent + 'rotationEuler: ' + rot,
        indent + 'scaleFactor: ' + scale,
    ])
    text = text[:m.start()] + block + text[m.end():]
    io.open(path, 'w', encoding='utf-8', newline='').write(text)
    print('OK   ', name, pos, rot, scale)
