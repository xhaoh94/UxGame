import os

BASE = r'C:\Project\UxGame\Unity\Assets'
MD   = os.path.join(BASE, r'Editor\Combat\COMBAT_DESIGN.md')
TEST = os.path.join(BASE, r'Editor\Combat\Tests\CombatEditorWorkflowTests.cs')

def rd(path):
    return open(path, 'rb').read().decode('utf-8')

def wr(path, text):
    open(path, 'wb').write(text.encode('utf-8'))
    raw = open(path, 'rb').read()
    crlf, lf = raw.count(b'\r\n'), raw.count(b'\n')
    print('  wrote %s: lines=%d CRLF=%d LF=%d %s' % (
        os.path.basename(path), lf, crlf, lf, 'OK' if crlf == lf else '*** MIXED ***'))
    assert crlf == lf

# ---------- 1) COMBAT_DESIGN.md ----------
md = rd(MD)
subs = [
    ('`CombatController.cs:149`', '`CombatController.cs:138`'),
    ('`CombatActionRunner.cs:76`', '`CombatActionRunner.cs:131`'),
    ('`CombatActionRunner.cs:212`', '`CombatActionRunner.cs:16`'),
    ('`CombatActionRunner.Clear()` 会置空 `ActionChanged`（`:285`）',
     '`CombatActionRunner.Clear()` 会置空 `ActionChanged`（`:517`）'),
    ('Life/Control 命中时独占并停掉 Action 层',
     'Life/Control 命中时独占并停掉 Action 播放层'),
]
for old, new in subs:
    assert md.count(old) == 1, 'MD anchor hit=%d: %r' % (md.count(old), old)
    md = md.replace(old, new)
wr(MD, md)
print('  md: %d substitution(s)' % len(subs))

# ---------- 2) CombatEditorWorkflowTests.cs ----------
t = rd(TEST)
anchor = ('        [Test]\r\n'
          '        public void SameStateCanKeepMultiplePresentationVariants()')
assert t.count(anchor) == 1

restored = (
    '        [TestCase(false, false)]\r\n'
    '        [TestCase(true, false)]\r\n'
    '        [TestCase(true, true)]\r\n'
    '        public void StatePresentationValidationUsesTimelineOrProfileContext(bool hasTimeline, bool destroyTimeline)\r\n'
    '        {\r\n'
    '            var profile = CombatTestProfiles.CreateProfile();\r\n'
    '            var timeline = hasTimeline ? ScriptableObject.CreateInstance<TimelineAsset>() : null;\r\n'
    '            try\r\n'
    '            {\r\n'
    '                if (timeline != null)\r\n'
    '                {\r\n'
    '                    timeline.SetFrameRate(profile.FrameRate);\r\n'
    '                }\r\n'
    '                // Control/Normal 与已删除的 Action 层落在同一个「不允许映射」分支，\r\n'
    '                // 用它替代即可继续覆盖 issue.Context 挂到哪个对象上。\r\n'
    '                CombatTestProfiles.AddStatePresentation(\r\n'
    '                    profile,\r\n'
    '                    StateLayer.Control,\r\n'
    '                    (int)ControlState.Normal,\r\n'
    '                    CombatStatePresentation.DefaultVariantId,\r\n'
    '                    timeline);\r\n'
    '                if (destroyTimeline)\r\n'
    '                {\r\n'
    '                    Object.DestroyImmediate(timeline);\r\n'
    '                }\r\n'
    '\r\n'
    '                var issues = CombatEditorUtility.ValidateProfile(profile);\r\n'
    '                var issue = issues.Find(value =>\r\n'
    '                    value.Severity == CombatValidationSeverity.Error &&\r\n'
    '                    value.Message.Contains("不是允许配置表现映射"));\r\n'
    '\r\n'
    '                Assert.IsNotNull(issue);\r\n'
    '                if (hasTimeline && !destroyTimeline)\r\n'
    '                {\r\n'
    '                    Assert.AreSame(timeline, issue.Context);\r\n'
    '                }\r\n'
    '                else\r\n'
    '                {\r\n'
    '                    Assert.AreSame(profile, issue.Context);\r\n'
    '                }\r\n'
    '            }\r\n'
    '            finally\r\n'
    '            {\r\n'
    '                Object.DestroyImmediate(profile);\r\n'
    '                if (timeline != null)\r\n'
    '                {\r\n'
    '                    Object.DestroyImmediate(timeline);\r\n'
    '                }\r\n'
    '            }\r\n'
    '        }\r\n'
    '\r\n'
)
t = t.replace(anchor, restored + anchor, 1)
wr(TEST, t)
print('  test: restored StatePresentationValidationUsesTimelineOrProfileContext')
