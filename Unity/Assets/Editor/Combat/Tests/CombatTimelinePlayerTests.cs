using NUnit.Framework;
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
