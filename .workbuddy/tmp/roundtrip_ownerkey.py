# -*- coding: utf-8 -*-
"""1) 反向应用规则重建改动前版本，落成回滚副本；
   2) 再正向应用一次，断言与当前落盘内容逐字节相同（往返证明：这次改动就是规则本身，没动别的东西）。"""
import io, os, sys

HERE = r'C:\Project\UxGame\.workbuddy\tmp'
src = io.open(os.path.join(HERE, 'apply_ownerkey.py'), 'r', encoding='utf-8').read()
ns = {}
exec(src[src.index('P_RULES = ['):src.index('pending = []')], ns)
P_RULES, C_RULES, W_RULES, TEST_NEW = ns['P_RULES'], ns['C_RULES'], ns['W_RULES'], ns['TEST_NEW']


def load(path):
    with io.open(path, 'r', encoding='utf-8', newline='') as fh:
        raw = fh.read()
    crlf, lf = raw.count('\r\n'), raw.count('\n')
    if crlf != lf and crlf != 0:
        print('ABORT: %s 混行尾' % path); sys.exit(1)
    return raw.replace('\r\n', '\n')


ROOT = r'C:\Project\UxGame\Unity\Assets'
PRE = os.path.join(HERE, 'backup_ownerkey_pre')
os.makedirs(PRE, exist_ok=True)

TESTS_OLD = '''using NUnit.Framework;

namespace Ux.Editor.Tests
{
    public sealed class CombatTimelinePlayerTests
    {
        [Test]
        public void SelectionClampsFrameAndNormalizesOwnerKey()
        {
            var selection = new CombatTimelineSelection(null, null, -4);

            Assert.AreEqual(string.Empty, selection.OwnerKey);
            Assert.AreEqual(0, selection.Frame);
            Assert.IsNull(selection.Asset);
        }

        [Test]
        public void PlanKeepsBaseAndActionSelectionsIndependent()
        {
            var baseSelection = new CombatTimelineSelection("base", null, 12);
            var actionSelection = new CombatTimelineSelection("action", null, 4);
            var plan = new CombatTimelinePlan(baseSelection, actionSelection);

            Assert.AreEqual("base", plan.Base.OwnerKey);
            Assert.AreEqual(12, plan.Base.Frame);
            Assert.AreEqual("action", plan.Action.OwnerKey);
            Assert.AreEqual(4, plan.Action.Frame);
            Assert.IsFalse(plan.ExclusiveBase);
        }
    }
}
'''

TASKS = [
    ('CombatTimelinePlayer.cs', r'HotfixBase\Manager\Combat\Runtime\Presentation\CombatTimelinePlayer.cs', P_RULES, None),
    ('CombatComponent.cs', r'Hotfix\Common\Combat\CombatComponent.cs', C_RULES, None),
    ('TimelineWindow.cs', r'Editor\Timeline\TimelineWindow.cs', W_RULES, None),
    ('CombatTimelinePlayerTests.cs', r'Editor\Combat\Tests\CombatTimelinePlayerTests.cs', [], TESTS_OLD),
]

for name, rel, rules, forced_old in TASKS:
    path = os.path.join(ROOT, rel)
    current = load(path)

    if forced_old is None:
        old = current
        for rname, o, n in rules:
            if old.count(n) != 1:
                print('ABORT [%s/%s]: 反向目标命中 %d 次' % (name, rname, old.count(n))); sys.exit(1)
            old = old.replace(n, o)
        roundtrip = old
        for rname, o, n in rules:
            if roundtrip.count(o) != 1:
                print('ABORT [%s/%s]: 往返正向命中 %d 次' % (name, rname, roundtrip.count(o))); sys.exit(1)
            roundtrip = roundtrip.replace(o, n)
    else:
        old = forced_old
        roundtrip = TEST_NEW

    if roundtrip != current:
        print('ABORT [%s]: 往返不一致 —— 文件里还有本次规则之外的改动' % name); sys.exit(1)

    with io.open(os.path.join(PRE, name), 'w', encoding='utf-8', newline='') as f:
        f.write(old)
    print('OK  %-32s 往返一致，回滚副本已写入' % name)

print('全部通过')
