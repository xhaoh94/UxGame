import io, os, re, sys

ASSETS = r'C:\Project\UxGame\Unity\Assets'

CHANGED = [
    r'HotfixBase\Manager\Combat\Asset\CombatLogicWindow.cs',
    r'HotfixBase\Manager\Combat\Asset\CombatActionAsset.cs',
    r'HotfixBase\Manager\Combat\Asset\CombatSpawnProfile.cs',
    r'HotfixBase\Manager\Combat\Asset\CombatSpawnPresentation.cs',
    r'HotfixBase\Manager\Combat\Asset\CharacterCombatProfile.cs',
    r'Editor\Combat\Timeline\CombatWindowEditorBase.cs',
    r'Editor\Combat\Timeline\CombatLogicTimelineSource.cs',
    r'Editor\Combat\Timeline\CombatCancelWindowEditorAdapters.cs',
    r'Editor\Combat\Timeline\CombatHitWindowEditorAdapters.cs',
    r'Editor\Combat\Timeline\CombatSpawnWindowEditorAdapters.cs',
    r'Editor\Combat\Timeline\CombatSpawnWindowInspectors.cs',
    r'Editor\Combat\Tests\CombatLogicTimelineSourceTests.cs',
    r'Editor\Combat\Tests\TimelineEditorSourceTests.cs',
]


def read(rel):
    p = os.path.join(ASSETS, rel)
    raw = io.open(p, 'rb').read()
    return raw.decode('utf-8')


def strip_comments(s):
    s = re.sub(r'/\*.*?\*/', '', s, flags=re.S)
    s = re.sub(r'//[^\n]*', '', s)
    # 字符串字面量里会出现 [0,10) 这类半开区间文案，不剥掉会把括号计数带偏。
    s = re.sub(r'"(?:\\.|[^"\\])*"', '""', s)
    return s


texts = {}
for rel in CHANGED:
    try:
        texts[rel] = read(rel)
    except Exception as exc:
        print('FAIL read %s: %s' % (rel, exc))
        sys.exit(1)

print('== 1. 括号配平（剥注释） ==')
bad = 0
for rel in CHANGED:
    t = strip_comments(texts[rel])
    ob, cb = t.count('{'), t.count('}')
    op, cp = t.count('('), t.count(')')
    oa, ca = t.count('['), t.count(']')
    ok = (ob == cb and op == cp and oa == ca)
    if not ok:
        bad += 1
    print('  %-6s {} %d/%d  () %d/%d  [] %d/%d  %s'
          % ('OK' if ok else 'FAIL', ob, cb, op, cp, oa, ca, os.path.basename(rel)))
assert bad == 0, '括号不平衡文件数: %d' % bad

print('')
print('== 2. 新增类型定义齐全 ==')
DEFS = {
    'CombatLogicWindow': r'abstract class CombatLogicWindow',
    'ActionSpawnWindow': r'sealed class ActionSpawnWindow\s*:\s*CombatLogicWindow',
    'ActionCancelWindow 继承基类': r'sealed class ActionCancelWindow\s*:\s*CombatLogicWindow',
    'ActionHitWindow 继承基类': r'sealed class ActionHitWindow\s*:\s*CombatLogicWindow',
    'CombatSpawnProfile': r'sealed class CombatSpawnProfile\s*:\s*ScriptableObject',
    'CombatSpawnPresentation': r'sealed class CombatSpawnPresentation',
    'ICombatLogicTimelineInspectorSource': r'interface ICombatLogicTimelineInspectorSource',
    'CombatWindowEditorTrackBase': r'abstract class CombatWindowEditorTrackBase<',
    'CombatWindowEditorClipBase': r'abstract class CombatWindowEditorClipBase<',
    'CombatCancelWindowEditorTrack': r'sealed class CombatCancelWindowEditorTrack\s*:',
    'CombatHitWindowEditorTrack': r'sealed class CombatHitWindowEditorTrack\s*:',
    'CombatSpawnWindowEditorTrack': r'sealed class CombatSpawnWindowEditorTrack\s*:',
    'CombatSpawnWindowTrackInspector': r'class CombatSpawnWindowTrackInspector',
    'CombatSpawnWindowClipInspector': r'class CombatSpawnWindowClipInspector',
}
all_text = '\n'.join(texts.values())
for name, pat in DEFS.items():
    assert re.search(pat, all_text), '缺少定义: %s' % name
    print('  OK   %s' % name)

print('')
print('== 3. CombatActionAsset 加轨的 5 处同步 ==')
asset = texts[r'HotfixBase\Manager\Combat\Asset\CombatActionAsset.cs']
assert 'List<ActionSpawnWindow> spawnWindows = new();' in asset, '缺字段'
assert 'IReadOnlyList<ActionSpawnWindow> SpawnWindows => spawnWindows;' in asset, '缺属性'
assert asset.count('foreach (var window in spawnWindows)') == 2, \
    'ValidateData / Migrate 应有 2 处 spawnWindows 遍历，实际 %d' % asset.count('foreach (var window in spawnWindows)')
assert 'spawnWindows = new List<ActionSpawnWindow>();' in asset, 'Migrate 缺 null 兜底'
assert 'spawnWindows ??= new List<ActionSpawnWindow>();' in asset, 'OnValidate 缺兜底'
print('  字段 / 属性 / ValidateData / Migrate / OnValidate 全部就位')

print('')
print('== 4. CharacterCombatProfile 加映射的 4 处同步 ==')
prof = texts[r'HotfixBase\Manager\Combat\Asset\CharacterCombatProfile.cs']
assert 'List<CombatSpawnPresentation> spawnPresentations = new();' in prof, '缺字段'
assert 'IReadOnlyList<CombatSpawnPresentation> SpawnPresentations => spawnPresentations;' in prof, '缺属性'
assert 'public CombatSpawnPresentation GetSpawnPresentation(' in prof, '缺查询方法'
assert 'spawnPresentations ??= new List<CombatSpawnPresentation>();' in prof, 'ValidateData 缺兜底'
assert 'foreach (var window in action.SpawnWindows)' in prof, 'ValidateRuntime 缺窗口校验'
assert '$"生成窗口未配置生成物' in prof, 'ValidateRuntime 缺生成物非空校验'
assert 'foreach (var presentation in spawnPresentations)' in prof, 'ValidateRuntime 缺映射校验'
print('  字段 / 属性 / 查询 / ValidateData / ValidateRuntime 全部就位')

print('')
print('== 5. 轨基类抽象成员在各子类都有实现 ==')
TRACK_ABSTRACT = ['Name', 'TypeName', 'Color', 'WindowPropertyName', 'TrackIdSuffix',
                  'Windows', 'MakeClip', 'CreateInspector']
track_files = {
    'cancel': texts[r'Editor\Combat\Timeline\CombatCancelWindowEditorAdapters.cs'],
    'hit': texts[r'Editor\Combat\Timeline\CombatHitWindowEditorAdapters.cs'],
    'spawn': texts[r'Editor\Combat\Timeline\CombatSpawnWindowEditorAdapters.cs'],
}
for key, t in track_files.items():
    for member in TRACK_ABSTRACT:
        assert re.search(r'override[^\n]*\b%s\b' % member, t), \
            '%s 轨缺少 %s 实现' % (key, member)
    for member in ['Window', 'Name', 'TypeName', 'CreateInspector', 'Track']:
        assert re.search(r'\b%s\b' % member, t), '%s 片段缺少 %s' % (key, member)
    print('  OK   %s 轨 8 个抽象成员 + 片段成员' % key)

print('')
print('== 6. Source 不再认识具体窗口类型 ==')
src = texts[r'Editor\Combat\Timeline\CombatLogicTimelineSource.cs']
for name in ['CombatCancelWindowEditorTrack', 'CombatHitWindowEditorTrack', 'CombatSpawnWindowEditorTrack']:
    assert name in src, 'Source 必须构造 ' + name
body = strip_comments(src)
assert 'case CombatCancelWindowEditorTrack' not in body, 'Source 仍在 switch 具体轨类型'
assert 'case CombatHitWindowEditorTrack' not in body, 'Source 仍在 switch 具体轨类型'
assert 'case CombatSpawnWindowEditorTrack' not in body, 'Source 仍在 switch 具体轨类型'
assert 'ICombatLogicTimelineInspectorSource' in body, 'CreateInspector 未走接口分派'
assert 'tracks = new ITimelineEditorTrack[] { cancelTrack, hitTrack, spawnTrack };' in body, \
    'tracks 数组未追加生成轨'
print('  三条轨按顺序构造，Inspector 走接口分派，无具体类型 switch')

print('')
print('== 7. 测试同步 ==')
t1 = texts[r'Editor\Combat\Tests\CombatLogicTimelineSourceTests.cs']
assert 'Assert.AreEqual(4, document.TrackCount,' in t1, 'TrackCount 未更新为 4'
assert 'source.Tracks[2]' in t1, '未按索引 2 取生成轨'
for name in ['LogicSourceEditsSpawnWindowsWithProfileReference',
             'SpawnWindowClampsRangeAndSharesItemIdDomain',
             'ProfileValidationRejectsSpawnWindowWithoutSpawnProfile']:
    assert name in t1, '缺测试 ' + name
t2 = texts[r'Editor\Combat\Tests\TimelineEditorSourceTests.cs']
assert 'CombatSpawnWindowEditorTrack' in t2 and 'CombatSpawnWindowEditorClip' in t2, \
    '解耦契约测试未补生成轨类型名'
print('  TrackCount=4、索引 2、3 个新测试、契约测试类型名 全部就位')

print('')
print('ALL OK')
