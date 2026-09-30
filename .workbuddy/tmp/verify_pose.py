# -*- coding: utf-8 -*-
"""粒子 Clip 位姿改动的静态自检。

分三层：
1. 资产层 —— 三条 Timeline 的位姿字段真的写进去了，且只写一份、缩进对、值合法；
2. 代码层 —— 字段/方法/调用点都在；
3. 一致性 —— TLParticleClip 里引用的 _clipAsset.<字段> 必须在 ParticleClipAsset 上有定义（抓改名漏改）。
"""
import io
import os
import re
import sys

ASSETS = r'C:\Project\UxGame\Unity\Assets'
COMBO = os.path.join(ASSETS, r'Data\Res\Timeline\HeroZS')

EXPECTED_POSE = {
    'HeroZSAttack01.asset': ('0 0 0', '0 0 0', '1'),
    'HeroZSAttack02.asset': ('0.25 0.15 0', '0 45 0', '1.4'),
    'HeroZSAttack03.asset': ('-0.3 0.3 0.4', '0 0 30', '2'),
}

# 01 是既有资产，原件是 CRLF（backup_combo 实测）；02/03 同批新建，同目录 HeroZSIdle/Run.asset 即 LF。
EXPECTED_EOL = {
    'HeroZSAttack01.asset': True,
    'HeroZSAttack02.asset': False,
    'HeroZSAttack03.asset': False,
}

VEC = re.compile(r'^([ \t]*)%s: \{x: (-?[\d.]+), y: (-?[\d.]+), z: (-?[\d.]+)\}$')


def read(path):
    return io.open(path, encoding='utf-8').read()


def check_assets():
    print('=== 1. 资产层 ===')
    for name, (want_pos, want_rot, want_scale) in EXPECTED_POSE.items():
        path = os.path.join(COMBO, name)
        raw = io.open(path, 'rb').read()
        crlf, lf = raw.count(b'\r\n'), raw.count(b'\n')
        assert crlf in (0, lf), '%s 行尾混用 CRLF=%d LF=%d' % (name, crlf, lf)
        got_eol = b'\r\n' if crlf else b'\n'
        want_eol = b'\r\n' if EXPECTED_EOL[name] else b'\n'
        assert got_eol == want_eol, '%s 行尾变成 %r（期望 %r）' % (name, got_eol, want_eol)
        # 归一化只为让下面按 $ 匹配的正则工作，不写回文件。
        text = raw.decode('utf-8').replace('\r\n', '\n')

        # 三个字段各出现一次，且都缩进 8 空格（位于 ParticleClipAsset 的 data 块内）
        for key in ('startColor', 'positionOffset', 'rotationEuler', 'scaleFactor'):
            n = len(re.findall(r'^[ \t]*%s:' % key, text, re.M))
            assert n == 1, '%s 的 %s 出现 %d 次（应为 1）' % (name, key, n)

        # 顺序：startColor -> positionOffset -> rotationEuler -> scaleFactor
        order = [re.search(r'^[ \t]*%s:' % k, text, re.M).start()
                 for k in ('startColor', 'positionOffset', 'rotationEuler', 'scaleFactor')]
        assert order == sorted(order), '%s 字段顺序不对' % name

        # 值必须与预期一致，且缩进统一
        got = {}
        for key, want in (('positionOffset', want_pos), ('rotationEuler', want_rot)):
            m = re.search(VEC.pattern % key, text, re.M)
            assert m, '%s 的 %s 不是标准 Vector3 写法' % (name, key)
            assert m.group(1) == ' ' * 8, '%s 的 %s 缩进 %r' % (name, key, m.group(1))
            got[key] = '{} {} {}'.format(m.group(2), m.group(3), m.group(4))
            assert got[key] == want, '%s 的 %s = %r，应为 %r' % (name, key, got[key], want)

        m = re.search(r'^([ \t]*)scaleFactor: ([\d.]+)$', text, re.M)
        assert m, '%s 的 scaleFactor 写法异常' % name
        assert m.group(1) == ' ' * 8, '%s 的 scaleFactor 缩进异常' % name
        assert m.group(2) == want_scale, '%s 的 scaleFactor = %s，应为 %s' % (name, m.group(2), want_scale)
        assert float(m.group(2)) > 0, '%s 的 scaleFactor 必须为正（0 会被当作未设置）' % name

        print('  OK   %s  pos=%s  rot=%s  scale=%s' % (name, got['positionOffset'], got['rotationEuler'], want_scale))


def check_code():
    print('=== 2. 代码层 ===')
    asset_cs = read(os.path.join(ASSETS, r'HotfixBase\Manager\Timeline\Asset\Particle\ParticleClipAsset.cs'))
    clip_cs = read(os.path.join(ASSETS, r'HotfixBase\Manager\Timeline\Runtime\Particle\TLParticleClip.cs'))
    insp_cs = read(os.path.join(ASSETS, r'Editor\Timeline\TimelineAssetInspectors.cs'))
    win_cs = read(os.path.join(ASSETS, r'Editor\Timeline\TimelineWindow.cs'))

    for decl in ('public Vector3 positionOffset;',
                 'public Vector3 rotationEuler;',
                 'public float scaleFactor = 1f;'):
        assert decl in asset_cs, 'ParticleClipAsset 缺少声明：%s' % decl
    print('  OK   ParticleClipAsset 三个位姿字段')

    # 应用/还原都在，且 OnDisable 走还原
    for token in ('_restorePose = PoseSnapshot.Capture(target);',
                  '_appliedPose = _restorePose.OffsetBy(',
                  '_appliedPose.Apply(target);',
                  'if (!_appliedPose.Matches(target))',
                  '_restorePose.Apply(target);'):
        assert token in clip_cs, 'TLParticleClip 缺少：%s' % token
    ondisable = re.search(r'protected override void OnDisable\(\)\s*\{(.*?)\n        \}', clip_cs, re.S)
    assert ondisable and 'RestorePose();' in ondisable.group(1), 'OnDisable 未调用 RestorePose'
    onstart = re.search(r'protected override void OnStart\(TimelineClipAsset asset\)\s*\{(.*?)\n        \}', clip_cs, re.S)
    assert onstart and '_hasPose = false;' in onstart.group(1), 'OnStart 未复位 _hasPose（池化复用会带脏位姿）'
    print('  OK   TLParticleClip 应用/还原/复位')

    # 池化哨兵：scaleFactor <= 0 必须回落到 1
    assert re.search(r'ScaleFactor\s*=>\s*_clipAsset\.scaleFactor > 0f \? _clipAsset\.scaleFactor : 1f;', clip_cs), \
        'scaleFactor 缺少 <=0 回落为 1 的哨兵'
    print('  OK   scaleFactor 哨兵')

    # Inspector：三个控件 + 三条 RecordUndo + RefreshFields 覆盖
    for token in ('new Vector3Field("位置偏移")', 'new Vector3Field("旋转偏移")', 'new FloatField("缩放倍率")',
                  '"timeline_clip_particle_position"', '"timeline_clip_particle_rotation"',
                  '"timeline_clip_particle_scale"'):
        assert token in insp_cs, 'ParticleClipInspector 缺少：%s' % token
    refresh = re.search(r'void RefreshFields\(\)\s*\{(.*?)\n        \}', insp_cs, re.S)
    assert refresh, '找不到 ParticleClipInspector.RefreshFields'
    for key in ('positionOffset', 'rotationEuler', 'scaleFactor'):
        assert 'SetValueWithoutNotify(asset.%s)' % key in refresh.group(1), \
            'RefreshFields 未回填 %s（改完值会显示旧值）' % key
    print('  OK   ParticleClipInspector 字段与回填')

    # 编辑器预览：占位宿主必须真的接上
    assert 'ParticleSystem ResolvePreviewVfx()' in win_cs, 'TimelineWindow 缺少 ResolvePreviewVfx'
    calls = len(re.findall(r'ResolvePreviewVfx\(\)', win_cs))
    assert calls == 3, 'ResolvePreviewVfx 出现 %d 次（定义 1 + 调用 2）' % calls
    assert re.search(r'vfx:\s*ResolvePreviewVfx\(\)', win_cs), '预览 Synchronize 未传 vfx，特效在编辑期看不到'
    assert 'target = ResolvePreviewVfx();' in win_cs, 'AutoBindMissingTracks 未走占位宿主'
    print('  OK   TimelineWindow 预览接上占位特效宿主')


def check_consistency():
    print('=== 3. 字段引用一致性 ===')
    asset_cs = read(os.path.join(ASSETS, r'HotfixBase\Manager\Timeline\Asset\Particle\ParticleClipAsset.cs'))
    base_cs = read(os.path.join(ASSETS, r'HotfixBase\Manager\Timeline\Asset\TimelineClipAsset.cs'))
    clip_cs = read(os.path.join(ASSETS, r'HotfixBase\Manager\Timeline\Runtime\Particle\TLParticleClip.cs'))

    declared = set(re.findall(r'public (?:Vector3|float|Color)\s+(\w+)', asset_cs))
    declared |= set(re.findall(r'public (?:int|string)\s+(\w+)', base_cs))
    used = set(re.findall(r'_clipAsset\.(\w+)', clip_cs))
    missing = used - declared
    assert not missing, 'TLParticleClip 引用了 ParticleClipAsset 上不存在的字段: %s' % sorted(missing)
    print('  OK   引用闭合：%s' % ', '.join(sorted(used)))
    print('       （声明的其它字段 %s）' % ', '.join(sorted(declared - used)))


def main():
    check_assets()
    check_code()
    check_consistency()
    print('ALL OK')


if __name__ == '__main__':
    sys.exit(main())
