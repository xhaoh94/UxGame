# -*- coding: utf-8 -*-
"""表现层 owner key: string 插值 -> 结构化比较（引用 + 实例号）。
先在内存里全算完并断言，全部通过才落盘。"""
import io, os, sys

ROOT = r'C:\Project\UxGame\Unity\Assets'
PLAYER = os.path.join(ROOT, r'HotfixBase\Manager\Combat\Runtime\Presentation\CombatTimelinePlayer.cs')
COMP   = os.path.join(ROOT, r'Hotfix\Common\Combat\CombatComponent.cs')
WIN    = os.path.join(ROOT, r'Editor\Timeline\TimelineWindow.cs')
TEST   = os.path.join(ROOT, r'Editor\Combat\Tests\CombatTimelinePlayerTests.cs')


def load(path):
    with io.open(path, 'r', encoding='utf-8', newline='') as fh:
        raw = fh.read()
    crlf, lf = raw.count('\r\n'), raw.count('\n')
    if crlf == lf:
        eol = '\r\n'
    elif crlf == 0:
        eol = '\n'
    else:
        print('ABORT: %s 混行尾 crlf=%d lf=%d' % (path, crlf, lf)); sys.exit(1)
    return raw.replace('\r\n', '\n'), eol


def save(path, text, eol):
    with io.open(path, 'w', encoding='utf-8', newline='') as fh:
        fh.write(text.replace('\n', eol))


def proj(s):
    return ''.join(c for c in s if ord(c) > 127)


def apply(text, rules, tag):
    for name, old, new in rules:
        n = text.count(old)
        if n != 1:
            print('ABORT [%s/%s]: 命中 %d 次（期望 1）' % (tag, name, n)); sys.exit(1)
        if not set(proj(old)) <= set(proj(new)):
            print('ABORT [%s/%s]: 规则丢了中文 %r' % (tag, name, proj(old))); sys.exit(1)
        text = text.replace(old, new)
    return text


# ---------------- CombatTimelinePlayer.cs ----------------
P_RULES = [
('selection-struct', '''    public readonly struct CombatTimelineSelection
    {
        public readonly string OwnerKey;
        public readonly TimelineAsset Asset;
        public readonly int Frame;

        public CombatTimelineSelection(string ownerKey, TimelineAsset asset, int frame)
        {
            OwnerKey = ownerKey ?? string.Empty;
            Asset = asset;
            Frame = Math.Max(0, frame);
        }
    }
''', '''    public readonly struct CombatTimelineSelection
    {
        public readonly TimelineAsset Asset;
        public readonly int Frame;

        /// <summary>同一资产的第几次播放。只有 Action 槽用：连招时同一资产再次起手必须强制重播动画。</summary>
        public readonly long InstanceId;

        public CombatTimelineSelection(TimelineAsset asset, int frame, long instanceId = 0)
        {
            Asset = asset;
            Frame = Math.Max(0, frame);
            InstanceId = instanceId;
        }

        /// <summary>
        /// 是否同一个来源 —— 唯一用途是判断"要不要重新切轨道"。
        /// 所以 Frame 必须不参与：帧每帧都在变，它变了不该重播。
        /// </summary>
        public bool SameOwner(in CombatTimelineSelection other) =>
            ReferenceEquals(Asset, other.Asset) && InstanceId == other.InstanceId;
    }
'''),
('resolver-doc', '''    public static class CombatTimelineResolver
    {
        public static CombatTimelinePlan Resolve(''', '''    public static class CombatTimelineResolver
    {
        /// <summary>
        /// 把状态机与动作运行时的当前事实翻译成"这一帧两个播放层各播哪条时间线、播到第几帧"。
        /// 纯读、无副作用；切轨道与推播放头由 CombatTimelinePlayer 做。
        /// 基础层按 Life > Control > Locomotion 选，Life/Control 命中即独占（ExclusiveBase）。
        /// </summary>
        public static CombatTimelinePlan Resolve('''),
('life', '''return new CombatTimelinePlan(new CombatTimelineSelection($"state:{(int)StateLayer.Life}:{lifeId}:{life.StableId}:{life.VariantId}", life.Timeline, states.GetStateFrame(StateLayer.Life)), default, true);''',
             '''return new CombatTimelinePlan(new CombatTimelineSelection(life.Timeline, states.GetStateFrame(StateLayer.Life)), default, true);'''),
('control', '''return new CombatTimelinePlan(new CombatTimelineSelection($"state:{(int)StateLayer.Control}:{controlId}:{control.StableId}:{control.VariantId}", control.Timeline, states.GetStateFrame(StateLayer.Control)), default, true);''',
             '''return new CombatTimelinePlan(new CombatTimelineSelection(control.Timeline, states.GetStateFrame(StateLayer.Control)), default, true);'''),
('action', '''actionSelection = new CombatTimelineSelection($"action:{actions.Current.InstanceId}", asset, actions.Current.ActionFrame);''',
            '''actionSelection = new CombatTimelineSelection(asset, actions.Current.ActionFrame, actions.Current.InstanceId);'''),
('locomotion', '''var baseSelection = new CombatTimelineSelection($"state:{(int)StateLayer.Locomotion}:{locomotionId}:{locomotion?.StableId ?? "none"}:{locomotion?.VariantId ?? ""}", locomotion?.Timeline, states.GetStateFrame(StateLayer.Locomotion));''',
                '''var baseSelection = new CombatTimelineSelection(locomotion?.Timeline, states.GetStateFrame(StateLayer.Locomotion));'''),
('fields', '''        private readonly TimelineComponent _component;
        private string _baseOwner = string.Empty;
        private string _actionOwner = string.Empty;
''', '''        private readonly TimelineComponent _component;
        private CombatTimelineSelection _baseOwner;
        private CombatTimelineSelection _actionOwner;
'''),
('compare', '''            var baseChanged = force || !string.Equals(_baseOwner, plan.Base.OwnerKey, StringComparison.Ordinal) ||
                plan.Base.Asset != null && _component.GetTimeline(TimelinePlaybackLayer.Base) == null;
            var actionChanged = force || !string.Equals(_actionOwner, plan.Action.OwnerKey, StringComparison.Ordinal) ||
                plan.Action.Asset != null && _component.GetTimeline(TimelinePlaybackLayer.Action) == null;
''', '''            var baseChanged = force || !_baseOwner.SameOwner(plan.Base) ||
                plan.Base.Asset != null && _component.GetTimeline(TimelinePlaybackLayer.Base) == null;
            var actionChanged = force || !_actionOwner.SameOwner(plan.Action) ||
                plan.Action.Asset != null && _component.GetTimeline(TimelinePlaybackLayer.Action) == null;
'''),
('store', '''            _baseOwner = plan.Base.OwnerKey;
            _actionOwner = plan.ExclusiveBase ? string.Empty : plan.Action.OwnerKey;
''', '''            _baseOwner = plan.Base;
            _actionOwner = plan.ExclusiveBase ? default : plan.Action;
'''),
('release', '''        public void Release()
        {
            _baseOwner = string.Empty;
            _actionOwner = string.Empty;
        }
''', '''        public void Release()
        {
            _baseOwner = default;
            _actionOwner = default;
        }
'''),
]

# ---------------- CombatComponent.cs ----------------
C_RULES = [
('field', '''        private CombatTimelinePlan _framePlan;
        private bool _framePlanInitialized;
        private bool _registered;
''', '''        private CombatTimelinePlan _framePlan;
        private bool _registered;
'''),
('ticklogic', '''            _timelinePlayer?.Synchronize(_framePlan, Unit?.Viewer?.GetComponentInChildren<Animator>());
            _framePlanInitialized = true;
''', '''            _timelinePlayer?.Synchronize(_framePlan, Unit?.Viewer?.GetComponentInChildren<Animator>());
'''),
('ondestroy', '''            _framePlan = default;
            _framePlanInitialized = false;
            _timelinePlayer?.Release();
''', '''            _framePlan = default;
            _timelinePlayer?.Release();
'''),
('refreshtimeline', '''        private bool RefreshTimeline(bool force)
        {
            if (Controller?.IsInitialized != true || Unit.Timeline == null)
            {
                return false;
            }

            EnsureTimelinePlayer();
            var nextPlan = CombatTimelineResolver.Resolve(Profile, Controller.StateMachine, Controller.ActionRunner, PresentationVariant);
            var changed = force || !_framePlanInitialized ||
                !string.Equals(nextPlan.Base.OwnerKey, _framePlan.Base.OwnerKey, StringComparison.Ordinal) ||
                !string.Equals(nextPlan.Action.OwnerKey, _framePlan.Action.OwnerKey, StringComparison.Ordinal) ||
                nextPlan.ExclusiveBase != _framePlan.ExclusiveBase;
            _framePlan = nextPlan;
            _timelinePlayer.Synchronize(_framePlan, Unit.Viewer?.GetComponentInChildren<Animator>(), force);
            _timelinePlayer.Evaluate(_framePlan, false);
            _framePlanInitialized = true;
            return changed;
        }
''', '''        private void RefreshTimeline(bool force)
        {
            if (Controller?.IsInitialized != true || Unit.Timeline == null)
            {
                return;
            }

            EnsureTimelinePlayer();
            _framePlan = CombatTimelineResolver.Resolve(Profile, Controller.StateMachine, Controller.ActionRunner, PresentationVariant);
            _timelinePlayer.Synchronize(_framePlan, Unit.Viewer?.GetComponentInChildren<Animator>(), force);
            _timelinePlayer.Evaluate(_framePlan, false);
        }
'''),
]

# ---------------- TimelineWindow.cs ----------------
W_RULES = [
('preview-plan', '''            var frame = clipView?.CurFrame ?? 0;
            var baseAsset = default(TimelineAsset);
            var baseKey = "preview:action-only";
            if (_previewBaseMode != PreviewBaseMode.ActionOnly && _combatProfile != null)
            {
                var state = _previewBaseMode == PreviewBaseMode.Move
                    ? LocomotionState.Move
                    : LocomotionState.Idle;
                baseAsset = _combatProfile.GetStateTimeline(StateLayer.Locomotion, (int)state);
                baseKey = $"preview:locomotion:{(int)state}:{baseAsset?.name ?? "none"}";
            }
            var baseSelection = new CombatTimelineSelection(baseKey, baseAsset, frame);
            var actionSelection = new CombatTimelineSelection(
                $"preview:action:{Asset.GetInstanceID()}",
                Asset,
                frame);
            return new CombatTimelinePlan(baseSelection, actionSelection);
''', '''            var frame = clipView?.CurFrame ?? 0;
            var baseAsset = default(TimelineAsset);
            if (_previewBaseMode != PreviewBaseMode.ActionOnly && _combatProfile != null)
            {
                var state = _previewBaseMode == PreviewBaseMode.Move
                    ? LocomotionState.Move
                    : LocomotionState.Idle;
                baseAsset = _combatProfile.GetStateTimeline(StateLayer.Locomotion, (int)state);
            }
            // 预览的来源就是资产本身；切换预览模式走 force 同步，不依赖来源变化。
            var baseSelection = new CombatTimelineSelection(baseAsset, frame);
            var actionSelection = new CombatTimelineSelection(Asset, frame);
            return new CombatTimelinePlan(baseSelection, actionSelection);
'''),
]

TEST_NEW = '''using NUnit.Framework;
using UnityEngine;

namespace Ux.Editor.Tests
{
    public sealed class CombatTimelinePlayerTests
    {
        [Test]
        public void SelectionClampsFrameAndDefaultsInstance()
        {
            var selection = new CombatTimelineSelection(null, -4);

            Assert.IsNull(selection.Asset);
            Assert.AreEqual(0, selection.Frame);
            Assert.AreEqual(0, selection.InstanceId);
        }

        [Test]
        public void PlanKeepsBaseAndActionSelectionsIndependent()
        {
            var baseSelection = new CombatTimelineSelection(null, 12);
            var actionSelection = new CombatTimelineSelection(null, 4, 7);
            var plan = new CombatTimelinePlan(baseSelection, actionSelection);

            Assert.AreEqual(12, plan.Base.Frame);
            Assert.AreEqual(4, plan.Action.Frame);
            Assert.AreEqual(7, plan.Action.InstanceId);
            Assert.IsFalse(plan.ExclusiveBase);
        }

        /// <summary>
        /// 来源比较只回答"要不要重新切轨道"：Frame 必须不参与（否则每帧都在变，等于每帧重播），
        /// 实例号必须参与（否则连招第二下不会重播动画）。
        /// </summary>
        [Test]
        public void SameOwnerIgnoresFrameAndTracksAssetAndInstance()
        {
            var first = ScriptableObject.CreateInstance<TimelineAsset>();
            var second = ScriptableObject.CreateInstance<TimelineAsset>();

            Assert.IsTrue(new CombatTimelineSelection(first, 3).SameOwner(new CombatTimelineSelection(first, 90)));
            Assert.IsFalse(new CombatTimelineSelection(first, 3).SameOwner(new CombatTimelineSelection(second, 3)));
            Assert.IsFalse(new CombatTimelineSelection(first, 3, 1).SameOwner(new CombatTimelineSelection(first, 3, 2)));
            Assert.IsTrue(default(CombatTimelineSelection).SameOwner(new CombatTimelineSelection(null, 5)));

            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }
    }
}
'''

pending = []
for path, rules, tag in ((PLAYER, P_RULES, 'player'), (COMP, C_RULES, 'component'), (WIN, W_RULES, 'window')):
    text, eol = load(path)
    pending.append((path, apply(text, rules, tag), eol, text))

# 测试文件整体重写：中文注释只会新增，所以只断言"没丢中文"
ttext, teol = load(TEST)
if not set(proj(ttext)) <= set(proj(TEST_NEW)):
    print('ABORT [test]: 丢了中文 %r' % proj(ttext)); sys.exit(1)
pending.append((TEST, TEST_NEW, teol, ttext))

# 残留断言：所有落盘内容里不得再出现这些词
DEAD = ['OwnerKey', '_framePlanInitialized']
for path, new_text, eol, old_text in pending:
    for tok in DEAD:
        n = new_text.count(tok)
        if n:
            print('ABORT: %s 仍含 %s (%d)' % (path, tok, n)); sys.exit(1)

for path, new_text, eol, old_text in pending:
    print('OK  %-24s 行数 %d -> %d  eol=%s' % (
        os.path.basename(path), old_text.count('\n') + 1, new_text.count('\n') + 1,
        'CRLF' if eol == '\r\n' else 'LF'))

for path, new_text, eol, old_text in pending:
    save(path, new_text, eol)
print('全部落盘完成')
