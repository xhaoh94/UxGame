import os, io

BASE = r'C:\Project\UxGame\Unity\Assets'

RUNNER = os.path.join(BASE, r'HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs')
CTRL   = os.path.join(BASE, r'HotfixBase\Manager\Combat\Runtime\Unit\CombatController.cs')
MD     = os.path.join(BASE, r'Editor\Combat\COMBAT_DESIGN.md')
TEST   = os.path.join(BASE, r'Editor\Combat\Tests\CombatEditorWorkflowTests.cs')

report = []

def load(path):
    raw = open(path, 'rb').read()
    text = raw.decode('utf-8')
    # 统一按 CRLF 切
    assert text.count('\r\n') == text.count('\n'), 'EOL not uniform: %s' % path
    lines = text.split('\r\n')
    return lines

def save(path, lines):
    text = '\r\n'.join(lines)
    data = text.encode('utf-8')
    open(path, 'wb').write(data)
    raw = open(path, 'rb').read()
    crlf = raw.count(b'\r\n')
    lf = raw.count(b'\n')
    report.append('%s: lines=%d CRLF=%d LF=%d %s' % (
        os.path.basename(path), raw.count(b'\n'), crlf, lf,
        'OK' if crlf == lf else '*** MIXED ***'))

def apply(path, edits, label):
    """edits: {1-based lineno: [new line strings]}  [] 表示删除该行"""
    lines = load(path)
    for n in edits:
        assert 1 <= n <= len(lines), '%s: line %d out of range (%d)' % (label, n, len(lines))
    out = []
    for i, ln in enumerate(lines, 1):
        if i in edits:
            out.extend(edits[i])
        else:
            out.append(ln)
    save(path, out)
    print('applied %d edit(s) -> %s' % (len(edits), os.path.basename(path)))
    for n in sorted(edits):
        print('    %4d | %s' % (n, ('<DELETED>' if not edits[n] else ' -> '.join(x.strip()[:70] for x in edits[n]))))

# ------------------------------------------------------------------
# 1) CombatActionRunner.cs  (P1 + P2)
# ------------------------------------------------------------------
runner_edits = {
    # P2: `{` 行尾 16 空格
    248: ['            {'],
    # P2: 空白行（原 DurationFrames 注释；与方法 doc 重复，直接删行）
    550: [],
    # P2: 空白行
    589: [],
    # P1: 删掉复述 for 的注释
    581: [],
    # P1: 删掉复述 if 的注释
    592: [],
    # P1: 恢复 TryCancel 的 ⚠ 坑注释
    579: [
        '        /// <summary>',
        '        /// 有动作时尝试被新命令取消：窗口由当前动作提供，且必须指向命令里的那个动作；',
        '        /// 条件成立就结束当前动作（Cancelled）并立刻起手新动作，ActionFrame 从 0 开始。',
        '        ///',
        '        /// ⚠ "狂点打不出伤害"就是这么来的：普攻 1001 的取消窗口是 [5,26) 且指向自己，',
        '        /// 帧 5-26 之间每次按键都会把动作重置回帧 0，永远走不到帧 41 的命中窗口。',
        '        /// </summary>',
        '        private bool TryCancel(in CombatFrameCommands commands)',
    ],
}
apply(RUNNER, runner_edits, 'CombatActionRunner')

# ------------------------------------------------------------------
# 2) CombatController.cs  (P3: 悬空步骤号 + P2 空白)
# ------------------------------------------------------------------
ctrl_edits = {
    74: ['        /// 执行顺序固定为"先推进动作、后判定移动"：Locomotion 判定要读 ActionRunner 本帧的锁移动状态，动作没先推进就会拿到上一帧的值。'],
    88: [],
    90: ['            {'],
    94: ['            {'],
    98: ['            {'],
}
apply(CTRL, ctrl_edits, 'CombatController')

# ------------------------------------------------------------------
# 3) COMBAT_DESIGN.md  (行号引用全部核验修正)
# ------------------------------------------------------------------
md = open(MD, encoding='utf-8').read()
assert md.count('\r\n') == md.count('\n')

md_pairs = [
    ('`CombatController.cs:149`    `CaptureSnapshot`', None),  # 占位，见下
]
subs = [
    ('`CombatController.cs:149`', '`CombatController.cs:138`', 1),
    ('`CombatActionRunner.cs:76`', '`CombatActionRunner.cs:131`', 1),
    ('`CombatActionRunner.cs:212`', '`CombatActionRunner.cs:16`', 1),
    ('`CombatActionRunner.Clear()` 会置空 `ActionChanged`（`:285`）',
     '`CombatActionRunner.Clear()` 会置空 `ActionChanged`（`:517`）', 1),
    ('Life/Control 命中时独占并停掉 Action 层',
     'Life/Control 命中时独占并停掉 Action 播放层', 1),
]
for old, new, expect in subs:
    cnt = md.count(old)
    assert cnt == expect, 'MD: expect %d hit, got %d for %r' % (expect, cnt, old)
    md = md.replace(old, new)
open(MD, 'wb').write(md.encode('utf-8'))
raw = open(MD, 'rb').read()
report.append('%s: CRLF=%d LF=%d %s' % (os.path.basename(MD), raw.count(b'\r\n'), raw.count(b'\n'),
                                        'OK' if raw.count(b'\r\n') == raw.count(b'\n') else '*** MIXED ***'))
print('applied %d sub(s) -> COMBAT_DESIGN.md' % len(subs))

# ------------------------------------------------------------------
# 4) CombatEditorWorkflowTests.cs  (P4: 恢复被删用例，改用 Control/Normal)
# ------------------------------------------------------------------
t = open(TEST, encoding='utf-8').read()
assert t.count('\r\n') == t.count('\n')

anchor = (
    '        [Test]\r\n'
    '        public void SameStateCanKeepMultiplePresentationVariants()'
)
assert t.count(anchor) == 1, 'test anchor hit=%d' % t.count(anchor)

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
    '                // Control/Normal 与已删除的 Action 层落在同一个"不允许映射"分支，\r\n'
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
open(TEST, 'wb').write(t.encode('utf-8'))
raw = open(TEST, 'rb').read()
report.append('%s: lines=%d CRLF=%d LF=%d %s' % (os.path.basename(TEST), raw.count(b'\n'),
                                                 raw.count(b'\r\n'), raw.count(b'\n'),
                                                 'OK' if raw.count(b'\r\n') == raw.count(b'\n') else '*** MIXED ***'))
print('restored test -> CombatEditorWorkflowTests.cs')

print()
print('=== EOL 校验 ===')
for r in report:
    print(' ', r)
