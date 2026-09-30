using NUnit.Framework;

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
