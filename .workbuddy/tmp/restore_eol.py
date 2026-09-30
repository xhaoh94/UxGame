# -*- coding: utf-8 -*-
"""把被归一化的行尾恢复原状。

依据（不是猜的）：
- .workbuddy/tmp/backup_combo/Timeline_HeroZSAttack01.asset 是改动前原件，实测 CRLF
  → Data/Res/Timeline/HeroZS/HeroZSAttack01.asset 必须回到 CRLF。
- ParticleClipAsset.cs 同目录邻居 ParticleTrackAsset.cs 与它自己的 .meta 都是 CRLF
  （git ls-files --eol 实测）→ 按目录惯例回到 CRLF。

不动 TLParticleClip.cs：同目录的 TLParticleTrack.cs 本来就是 LF。
不动 HeroZSAttack02/03.asset：新建文件，同目录 HeroZSIdle/Run.asset 就是 LF。
"""
import io

TARGETS = [
    r'C:\Project\UxGame\Unity\Assets\Data\Res\Timeline\HeroZS\HeroZSAttack01.asset',
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Timeline\Asset\Particle\ParticleClipAsset.cs',
]

for path in TARGETS:
    raw = io.open(path, 'rb').read()
    text = raw.decode('utf-8')
    if '\r\n' in text:
        print('SKIP ', path.rsplit('\\', 1)[-1], '（已是 CRLF）')
        continue
    assert '\r' not in text, '存在孤立 \\r，不能盲目替换：%s' % path
    text = text.replace('\n', '\r\n')
    io.open(path, 'w', encoding='utf-8', newline='').write(text)

    after = io.open(path, 'rb').read()
    lf, crlf = after.count(b'\n'), after.count(b'\r\n')
    assert crlf == lf and crlf > 0, '写入后行尾异常：crlf=%d lf=%d' % (crlf, lf)
    print('FIXED', path.rsplit('\\', 1)[-1], '-> CRLF (%d 行)' % crlf)
