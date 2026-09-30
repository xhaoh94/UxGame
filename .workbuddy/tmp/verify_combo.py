# -*- coding: utf-8 -*-
"""校验三连击资产：行尾、guids、引用闭合、字段合法性。"""
import io
import json
import os
import re
import sys

ASSETS = r'C:\Project\UxGame\Unity\Assets'
COMBAT = os.path.join(ASSETS, 'Data', 'Res', 'Combat', 'HeroZS')
TIMELINE = os.path.join(ASSETS, 'Data', 'Res', 'Timeline', 'HeroZS')

G = json.load(io.open(r'C:\Project\UxGame\.workbuddy\tmp\new_guids.json', 'r', encoding='utf-8'))

expect_guids = {
    os.path.join(COMBAT, 'HeroZSAttack02.asset.meta'): G['combat_atk02'],
    os.path.join(COMBAT, 'HeroZSAttack03.asset.meta'): G['combat_atk03'],
    os.path.join(TIMELINE, 'HeroZSAttack02.asset.meta'): G['tl_atk02'],
    os.path.join(TIMELINE, 'HeroZSAttack03.asset.meta'): G['tl_atk03'],
}
for path, guid in expect_guids.items():
    text = io.open(path, 'r', encoding='utf-8').read()
    assert ('guid: %s' % guid) in text, 'meta guid 不符: %s' % path

# 1. 新 guid 的"出现文件集合"必须精确等于预期（比计数强：多一处引用也会被逮到）
PROFILE = os.path.join(COMBAT, 'HeroZSCombatProfile.asset')
expected_holders = {
    G['combat_atk02']: {os.path.join(COMBAT, 'HeroZSAttack02.asset.meta'), PROFILE},
    G['combat_atk03']: {os.path.join(COMBAT, 'HeroZSAttack03.asset.meta'), PROFILE},
    G['tl_atk02']: {os.path.join(TIMELINE, 'HeroZSAttack02.asset.meta'), PROFILE},
    G['tl_atk03']: {os.path.join(TIMELINE, 'HeroZSAttack03.asset.meta'), PROFILE},
    G['vfx_host_script']: {os.path.join(ASSETS, 'Hotfix', 'Common', 'Combat',
                                        'CombatVfxHost.cs.meta')},
}
new_guids = list(expected_holders)
actual_holders = {g: set() for g in new_guids}
for root, dirs, files in os.walk(ASSETS):
    dirs[:] = [d for d in dirs if d not in ('Library', 'Temp', 'obj')]
    for name in files:
        if not name.endswith(('.meta', '.asset', '.prefab', '.unity')):
            continue
        p = os.path.join(root, name)
        try:
            text = io.open(p, 'r', encoding='utf-8', errors='ignore').read()
        except OSError:
            continue
        for g in new_guids:
            if g in text:
                actual_holders[g].add(os.path.normcase(p))
for g, want in expected_holders.items():
    want = {os.path.normcase(x) for x in want}
    got = actual_holders[g]
    assert got == want, ('guid %s 持有文件不符\n  多出: %s\n  缺少: %s'
                         % (g, sorted(got - want), sorted(want - got)))
print('guid 持有集合 ok')

# 2. 每个文件只出现 UTF-8 且无 tab；行尾与备份一致
expect_eol = {
    os.path.join(COMBAT, 'HeroZSAttack01.asset'): b'\n',
    os.path.join(COMBAT, 'HeroZSAttack02.asset'): b'\n',
    os.path.join(COMBAT, 'HeroZSAttack03.asset'): b'\n',
    os.path.join(TIMELINE, 'HeroZSAttack01.asset'): b'\r\n',
    os.path.join(TIMELINE, 'HeroZSAttack02.asset'): b'\n',
    os.path.join(TIMELINE, 'HeroZSAttack03.asset'): b'\n',
}
for path, eol in expect_eol.items():
    raw = io.open(path, 'rb').read()
    lf = raw.count(b'\n')
    crlf = raw.count(b'\r\n')
    got = b'\r\n' if crlf else b'\n'
    assert crlf in (0, lf), '%s 行尾混用 CRLF=%d LF=%d' % (path, crlf, lf)
    assert got == eol, '%s 行尾变成 %r（期望 %r）' % (path, got, eol)
    assert b'\t' not in raw, '%s 含 tab' % path
    assert raw.decode('utf-8'), path
print('行尾/编码 ok')

# 3. 三段动作的窗口与引用闭合
profile = io.open(os.path.join(COMBAT, 'HeroZSCombatProfile.asset'), 'r', encoding='utf-8').read()
action_guids = re.findall(r'^  - \{fileID: 11400000, guid: (\w+), type: 2\}$', profile, re.M)
# statePresentations 的 timeline 行缩进相同，必须先切到 actionPresentations 段再匹配
pres_block = profile.split('actionPresentations:', 1)[1]
presentation_guids = re.findall(
    r'^    timeline: \{fileID: 11400000, guid: (\w+), type: 2\}$', pres_block, re.M)
assert len(action_guids) == 3, 'profile actions 数 = %d' % len(action_guids)
assert len(presentation_guids) == 3, 'profile actionPresentations 数 = %d' % len(presentation_guids)

gui2path = {}
for folder in (COMBAT, TIMELINE):
    for name in os.listdir(folder):
        if not name.endswith('.asset.meta'):
            continue
        text = io.open(os.path.join(folder, name), 'r', encoding='utf-8').read()
        m = re.search(r'^guid: (\w+)$', text, re.M)
        gui2path[m.group(1)] = os.path.join(folder, name[:-5])
for g in action_guids + presentation_guids:
    assert g in gui2path, 'Profile 引用了不存在的资产 guid %s' % g
print('profile 引用闭合 ok')

actions = {}
for g in action_guids:
    text = io.open(gui2path[g], 'r', encoding='utf-8').read()
    aid = int(re.search(r'^  actionId: (\d+)$', text, re.M).group(1))
    dur = int(re.search(r'^  durationFrames: (\d+)$', text, re.M).group(1))
    targets = [int(x) for x in re.findall(r'^    TargetActionId: (\d+)$', text, re.M)]
    hits = [(int(a), int(b)) for a, b in re.findall(r'^    StartFrame: (\d+)\n    EndFrame: (\d+)\n    shape', text, re.M)]
    cancels = [(int(a), int(b)) for a, b in re.findall(r'^    StartFrame: (\d+)\n    EndFrame: (\d+)\n    TargetActionId', text, re.M)]
    actions[aid] = {'dur': dur, 'targets': targets, 'hits': hits, 'cancels': cancels}
    assert 1001 <= aid <= 1003
    for t in targets:
        assert t in actions or t in (1001, 1002, 1003), 'target %d 不存在' % t
for aid, info in sorted(actions.items()):
    for (s, e) in info['hits'] + info['cancels']:
        assert 0 <= s < e <= info['dur'], '动作 %d 区间 [%d,%d) 超出时长 %d' % (aid, s, e, info['dur'])
    print('  action %d dur=%d 命中=%s 取消=%s 目标=%s'
          % (aid, info['dur'], info['hits'], info['cancels'], info['targets']))
print('窗口合法性 ok')

# 4. 三条 Timeline 的粒子 Clip 窗口都要落在动画时长内
for rid in (1, 2, 3):
    path = os.path.join(TIMELINE, 'HeroZSAttack%02d.asset' % rid)
    text = io.open(path, 'r', encoding='utf-8').read()
    assert 'class: ParticleAssetTrack' in text and 'class: ParticleClipAsset' in text
    assert text.count('class: AnimationTrackAsset') == 1
    for s, e in re.findall(r'^        StartFrame: (\d+)\n        EndFrame: (\d+)\n', text, re.M):
        assert int(s) < int(e) <= 70, '%s 粒子窗口越界' % path
print('timeline 表现 ok')
print('ALL OK')
