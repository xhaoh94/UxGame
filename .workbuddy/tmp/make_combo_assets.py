# -*- coding: utf-8 -*-
"""生成三连击所需的 Combat / Timeline 资产，并改写现有的 1001 与 Profile。

全程 ASCII 写入（中文用 \\uXXXX 转义），逐文件保留原行尾；新建文件用 LF。
"""
import io
import json
import os
import re
import sys
import uuid

ROOT = r'C:\Project\UxGame\Unity\Assets\Data\Res'
COMBAT = os.path.join(ROOT, 'Combat', 'HeroZS')
TIMELINE = os.path.join(ROOT, 'Timeline', 'HeroZS')

G = json.load(io.open(r'C:\Project\UxGame\.workbuddy\tmp\new_guids.json', 'r', encoding='utf-8'))
IDS = json.load(io.open(r'C:\Project\UxGame\.workbuddy\tmp\new_item_ids.json', 'r', encoding='utf-8'))

SCRIPT_COMBAT = '3b75265136ed4c240ba71ab0bd53273f'   # CombatActionAsset
SCRIPT_TIMELINE = 'f1420382afa24de4e8b1a9fce371ab3e'  # TimelineAsset
ATTACK_CLIP = '75f41ed80eab9704189a26e21e6c38cf'      # Hero_ZS@Attack.FBX
UPPER_MASK = 'c93943888ec64050899cb0dd185da004'       # HeroZSUpperBody.mask
FBX_CLIP_FILEID = 1827226128182048838

ATTACK_FRAMES = 70

# 段位表：(actionId, stableId, displayName 转义, 命中窗口, 取消窗口目标, 取消窗口, 伤害)
# 取消窗口从命中窗口结束处打开 —— 命中之前按键不生效，这就是"按早了没用"。
SEGMENTS = [
    (1001, 'hero.action.attack01', '\\u666E\\u901A\\u653B\\u51FB\\u00B7\\u4E00\\u6BB5',
     (16, 20), 1002, (20, 36), 10),
    (1002, 'hero.action.attack02', '\\u666E\\u901A\\u653B\\u51FB\\u00B7\\u4E8C\\u6BB5',
     (18, 22), 1003, (22, 40), 12),
    (1003, 'hero.action.attack03', '\\u666E\\u901A\\u653B\\u51FB\\u00B7\\u4E09\\u6BB5',
     (26, 32), 1001, (32, 60), 24),
]
SEG = {s[0]: s for s in SEGMENTS}

WINDOW_IDS = {1001: G['window_1001'], 1002: G['window_1002'], 1003: G['window_1003']}
HIT_IDS = {1001: G['hit_1001'], 1002: G['hit_1002'], 1003: G['hit_1003']}

# 表现：每段一条 Timeline，粒子颜色/窗口不同，用来肉眼确认播的是第几段。
FX = {
    1001: ((0.55, 0.85, 1.00), (16, 40)),
    1002: ((1.00, 0.78, 0.25), (18, 42)),
    1003: ((1.00, 0.32, 0.18), (26, 62)),
}

GUIDS = {
    1001: {'combat': 'bcf1d7b44ac84f749e130a537270b60a', 'timeline': '8c3a15de724e45b4a93ed9a8ee7dfc42'},
    1002: {'combat': G['combat_atk02'], 'timeline': G['tl_atk02']},
    1003: {'combat': G['combat_atk03'], 'timeline': G['tl_atk03']},
}


def load(path):
    raw = io.open(path, 'rb').read()
    crlf = raw.count(b'\r\n')
    lf = raw.count(b'\n')
    if crlf and crlf != lf:
        sys.exit('ABORT: %s 行尾不统一 CRLF=%d LF=%d' % (path, crlf, lf))
    eol = '\r\n' if crlf else '\n'
    return raw.decode('utf-8').replace('\r\n', '\n'), eol


def write(path, text, eol):
    if not text.endswith('\n'):
        text += '\n'
    assert all(ord(ch) < 128 for ch in text), 'must stay ASCII: %s' % path
    out = text.replace('\n', eol)
    with io.open(path, 'w', encoding='utf-8', newline='') as fh:
        fh.write(out)


def head(script_guid, name, editor_class):
    return (
        '%%YAML 1.1\n'
        '%%TAG !u! tag:unity3d.com,2011:\n'
        '--- !u!114 &11400000\n'
        'MonoBehaviour:\n'
        '  m_ObjectHideFlags: 0\n'
        '  m_CorrespondingSourceObject: {fileID: 0}\n'
        '  m_PrefabInstance: {fileID: 0}\n'
        '  m_PrefabAsset: {fileID: 0}\n'
        '  m_GameObject: {fileID: 0}\n'
        '  m_Enabled: 1\n'
        '  m_EditorHideFlags: 0\n'
        '  m_Script: {fileID: 11500000, guid: %s, type: 3}\n'
        '  m_Name: %s\n'
        '  m_EditorClassIdentifier: %s\n'
    ) % (script_guid, name, editor_class)


def combat_action(action_id):
    _, stable_id, display, hit, next_id, cancel, damage = SEG[action_id]
    return (
        head(SCRIPT_COMBAT, 'HeroZSAttack%02d' % (action_id - 1000), '') +
        '  stableId: %s\n'
        '  actionId: %d\n'
        '  displayName: "%s"\n'
        '  durationFrames: %d\n'
        '  movementPolicy: 0\n'
        '  cancelWindows:\n'
        '  - stableId: %s\n'
        '    StartFrame: %d\n'
        '    EndFrame: %d\n'
        '    TargetActionId: %d\n'
        '    RequiresHitConfirm: 0\n'
        '  hitWindows:\n'
        '  - stableId: %s\n'
        '    StartFrame: %d\n'
        '    EndFrame: %d\n'
        '    shape: 0\n'
        '    radiusMillimeters: 1500\n'
        '  damage: %d\n'
        '  appliedBuff:\n'
        '    buffId: 0\n'
        '    durationFrames: 120\n'
        '    damagePerTick: 0\n'
        % (stable_id, action_id, display, ATTACK_FRAMES,
           WINDOW_IDS[action_id], cancel[0], cancel[1], next_id,
           HIT_IDS[action_id], hit[0], hit[1], damage)
    )


def fmt(value):
    """Unity 写浮点时不会带多余的 .0。"""
    text = ('%.4f' % value).rstrip('0').rstrip('.')
    return text if text else '0'


def timeline_asset(action_id, anim_rid, anim_clip_rid, fx_track_rid, fx_clip_rid):
    colour, (fx_start, fx_end) = FX[action_id]
    rid = action_id - 1000
    ids = IDS[str(action_id)]
    return (
        head(SCRIPT_TIMELINE, 'HeroZSAttack%02d' % rid, 'Unity.HotfixBase::Ux.TimelineAsset') +
        '  frameRate: 60\n'
        '  tracks:\n'
        '  - rid: %d\n'
        '  - rid: %d\n'
        '  references:\n'
        '    version: 2\n'
        '    RefIds:\n'
        '    - rid: %d\n'
        '      type: {class: AnimationTrackAsset, ns: Ux, asm: Unity.HotfixBase}\n'
        '      data:\n'
        '        id: %s\n'
        '        trackName: Attack\n'
        '        clips:\n'
        '        - rid: %d\n'
        '        isAdditive: 0\n'
        '        avatarMask: {fileID: 31900000, guid: %s, type: 2}\n'
        '    - rid: %d\n'
        '      type: {class: AnimationClipAsset, ns: Ux, asm: Unity.HotfixBase}\n'
        '      data:\n'
        '        id: %s\n'
        '        clipName: Attack\n'
        '        StartFrame: 0\n'
        '        EndFrame: %d\n'
        '        InFrame: 0\n'
        '        OutFrame: 0\n'
        '        clip: {fileID: %d, guid: %s, type: 3}\n'
        '        pre: 0\n'
        '        post: 0\n'
        '        PreFrame: 0\n'
        '        PostFrame: 2147483647\n'
        '    - rid: %d\n'
        '      type: {class: ParticleAssetTrack, ns: Ux, asm: Unity.HotfixBase}\n'
        '      data:\n'
        '        id: %s\n'
        '        trackName: HitFx%02d\n'
        '        clips:\n'
        '        - rid: %d\n'
        '    - rid: %d\n'
        '      type: {class: ParticleClipAsset, ns: Ux, asm: Unity.HotfixBase}\n'
        '      data:\n'
        '        id: %s\n'
        '        clipName: HitFx%02d\n'
        '        StartFrame: %d\n'
        '        EndFrame: %d\n'
        '        InFrame: 0\n'
        '        OutFrame: 0\n'
        '        startColor: {r: %s, g: %s, b: %s, a: 1}\n'
        % (anim_rid, fx_track_rid,
           anim_rid, ids[0],
           anim_clip_rid, UPPER_MASK,
           anim_clip_rid, ids[1],
           ATTACK_FRAMES, FBX_CLIP_FILEID, ATTACK_CLIP,
           fx_track_rid, ids[2], rid,
           fx_clip_rid,
           fx_clip_rid, ids[3], rid,
           fx_start, fx_end, fmt(colour[0]), fmt(colour[1]), fmt(colour[2]))
    )


def meta(guid):
    return (
        'fileFormatVersion: 2\n'
        'guid: %s\n'
        'NativeFormatImporter:\n'
        '  externalObjects: {}\n'
        '  mainObjectFileID: 11400000\n'
        '  userData: \n'
        '  assetBundleName: \n'
        '  assetBundleVariant: \n'
    ) % guid


def validate_timeline_refs(path):
    text = io.open(path, 'r', encoding='utf-8').read()
    used = re.findall(r'^\s*- rid: (\d+)$', text, re.M)
    defined = re.findall(r'^\s{4}- rid: (\d+)$', text, re.M)
    assert len(used) == 8, '%s 引用数不是 8：%s' % (path, used)
    assert len(defined) == 4, '%s 定义数不是 4：%s' % (path, defined)
    for rid in defined:
        assert used.count(rid) == 2, '%s rid %s 出现 %d 次（应为 1 引用 + 1 定义）' % (path, rid, used.count(rid))
    print('    refs ok: %s' % sorted(defined))


def main():
    # ---- 新增/改写 Combat 逻辑资产 ----
    for action_id in (1001, 1002, 1003):
        path = os.path.join(COMBAT, 'HeroZSAttack%02d.asset' % (action_id - 1000))
        if action_id == 1001:
            _, eol = load(path)
        else:
            eol = '\n'
        write(path, combat_action(action_id), eol)
        print('OK  combat  %s' % path)

    # ---- 新增/改写 Timeline 表现资产 ----
    rids = {1001: (1176164152299749448, 1176164152299749450, 1176164152299749460, 1176164152299749462),
            1002: (1176164152299749470, 1176164152299749472, 1176164152299749474, 1176164152299749476),
            1003: (1176164152299749480, 1176164152299749482, 1176164152299749484, 1176164152299749486)}
    for action_id in (1001, 1002, 1003):
        path = os.path.join(TIMELINE, 'HeroZSAttack%02d.asset' % (action_id - 1000))
        if action_id == 1001:
            _, eol = load(path)
        else:
            eol = '\n'
        write(path, timeline_asset(action_id, *rids[action_id]), eol)
        validate_timeline_refs(path)
        print('OK  timeline %s' % path)

    # ---- meta（新增资产） ----
    for action_id in (1002, 1003):
        for folder, key in ((COMBAT, 'combat'), (TIMELINE, 'timeline')):
            path = os.path.join(folder, 'HeroZSAttack%02d.asset.meta' % (action_id - 1000))
            write(path, meta(GUIDS[action_id][key]), '\n')
            print('OK  meta    %s' % path)

    # ---- Profile：注册三段动作 + 表现映射 ----
    profile = os.path.join(COMBAT, 'HeroZSCombatProfile.asset')
    text, eol = load(profile)
    old = (
        '  actions:\n'
        '  - {fileID: 11400000, guid: %s, type: 2}\n'
        '  actionPresentations:\n'
        '  - action: {fileID: 11400000, guid: %s, type: 2}\n'
        '    timeline: {fileID: 11400000, guid: %s, type: 2}\n'
    ) % (GUIDS[1001]['combat'], GUIDS[1001]['combat'], GUIDS[1001]['timeline'])
    assert text.endswith(old) or GUIDS[1003]['combat'] in text, 'Profile 尾部结构不符合预期，先看现场'
    if GUIDS[1003]['combat'] in text:
        print('SKIP profile（已注册三段）')
        return
    new = (
        '  actions:\n'
        '  - {fileID: 11400000, guid: %s, type: 2}\n'
        '  - {fileID: 11400000, guid: %s, type: 2}\n'
        '  - {fileID: 11400000, guid: %s, type: 2}\n'
        '  actionPresentations:\n'
        '  - action: {fileID: 11400000, guid: %s, type: 2}\n'
        '    timeline: {fileID: 11400000, guid: %s, type: 2}\n'
        '  - action: {fileID: 11400000, guid: %s, type: 2}\n'
        '    timeline: {fileID: 11400000, guid: %s, type: 2}\n'
        '  - action: {fileID: 11400000, guid: %s, type: 2}\n'
        '    timeline: {fileID: 11400000, guid: %s, type: 2}\n'
    ) % (GUIDS[1001]['combat'], GUIDS[1002]['combat'], GUIDS[1003]['combat'],
         GUIDS[1001]['combat'], GUIDS[1001]['timeline'],
         GUIDS[1002]['combat'], GUIDS[1002]['timeline'],
         GUIDS[1003]['combat'], GUIDS[1003]['timeline'])
    write(profile, text[:len(text) - len(old)] + new, eol)
    print('OK  profile %s' % profile)


main()
