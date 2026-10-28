using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIFlowLayoutTests
    {
        [TestCase(320f, 100f, 10f, 0, 3)]
        [TestCase(319.9f, 100f, 10f, 0, 2)]
        [TestCase(320f, 100f, 10f, 2, 2)]
        [TestCase(1000f, 100f, 10f, 4, 4)]
        [TestCase(80f, 100f, 10f, 0, 1)]
        [TestCase(0f, 100f, 10f, 0, 1)]
        public void CapacityUsesAvailableSpaceAndOptionalMaximum(float available, float size, float gap, int cap, int expected)
        {
            Assert.AreEqual(expected, UGUIFlowLayout.CalculateCountPerLine(available, size, gap, true, cap));
            Assert.AreEqual(1, UGUIFlowLayout.CalculateCountPerLine(available, size, gap, false, cap));
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void SingleLineIsTheSameFixedAlgorithmWithOneCellPerLine(UGUIVirtualListAxis axis)
        {
            var flow = new UGUIFlowLayout(axis, new Vector2(100, 80), spacing: new Vector2(8, 8), paddingStart: 12, paddingEnd: 20);
            flow.SetViewportSize(new Vector2(2000, 2000));
            flow.SetItemCount(100, true);
            var old = new UGUIFixedLinearLayout(axis, new Vector2(100, 80), 8, 12, 20);
            old.SetItemCount(100, true);
            Assert.AreEqual(1, flow.CountPerLine);
            Assert.AreEqual(old.ContentLength, flow.ContentLength, 0.01f);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(old.GetItemLayout(i).Position, flow.GetItemLayout(i).Position);
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void FixedFlowRecalculatesCapacityWhenViewportChanges(UGUIVirtualListAxis axis)
        {
            var flow = CreateVariableOrFixed(axis, false);
            flow.SetItemCount(10, true);
            Assert.AreEqual(3, flow.CountPerLine);
            Assert.AreEqual(302, flow.ContentLength, 0.01f);
            Assert.IsTrue(flow.SetViewportSize(axis == UGUIVirtualListAxis.Vertical ? new Vector2(220, 300) : new Vector2(300, 220)));
            Assert.AreEqual(2, flow.CountPerLine);
            Assert.AreEqual(372, flow.ContentLength, 0.01f);
            var pos = flow.GetItemLayout(7).Position;
            Assert.AreEqual(axis == UGUIVirtualListAxis.Vertical ? new Vector2(108, -222) : new Vector2(222, -108), pos);
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void VariableFlowUsesLargestItemOfEachLineAndDoesNotOverlap(UGUIVirtualListAxis axis)
        {
            var flow = MeasuredFlow(axis);
            Assert.AreEqual(327, flow.ContentLength, 0.01f);
            Assert.AreEqual(316, flow.CrossSize, 0.01f);
            Assert.AreEqual(axis == UGUIVirtualListAxis.Vertical ? new Vector2(108, -142) : new Vector2(142, -108), flow.GetItemLayout(4).Position);
            Assert.AreEqual(axis == UGUIVirtualListAxis.Vertical ? new Vector2(0, -242) : new Vector2(242, 0), flow.GetItemLayout(6).Position);
            SetMainSize(flow, axis, 1, 40);
            Assert.AreEqual(axis == UGUIVirtualListAxis.Vertical ? -92 : 92,
                axis == UGUIVirtualListAxis.Vertical ? flow.GetItemLayout(3).Position.y : flow.GetItemLayout(3).Position.x, 0.01f);
            SetMainSize(flow, axis, 0, 150);
            Assert.AreEqual(axis == UGUIVirtualListAxis.Vertical ? -172 : 172,
                axis == UGUIVirtualListAxis.Vertical ? flow.GetItemLayout(3).Position.y : flow.GetItemLayout(3).Position.x, 0.01f);
        }

        [Test]
        public void VariableFlowQueriesWholeLinesAndCanReturnEmptyGap()
        {
            var flow = MeasuredFlow(UGUIVirtualListAxis.Vertical);
            AssertRange(flow.GetVisibleRange(12, 120, 0), 0, 2);
            Assert.IsTrue(flow.GetVisibleRange(132, 10, 0).IsEmpty);
            AssertRange(flow.GetVisibleRange(142, 90, 0), 3, 5);
            AssertRange(flow.GetVisibleRange(242, 65, 0), 6, 7);
            Assert.IsTrue(flow.GetVisibleRange(307, 20, 0).IsEmpty);
        }

        [Test]
        public void ResizeAndCountChangesKeepKnownMeasurementsUntilExplicitReset()
        {
            var flow = MeasuredFlow(UGUIVirtualListAxis.Vertical);
            flow.SetViewportSize(new Vector2(220, 300));
            Assert.AreEqual(2, flow.CountPerLine);
            Assert.AreEqual(407, flow.ContentLength, 0.01f);
            Assert.AreEqual(120, flow.GetItemLayout(1).Size.y, 0.01f);
            flow.SetItemCount(9, false);
            Assert.AreEqual(120, flow.GetItemLayout(1).Size.y, 0.01f);
            Assert.AreEqual(60, flow.GetItemLayout(8).Size.y, 0.01f);
            flow.SetItemCount(2, false);
            Assert.AreEqual(120, flow.GetItemLayout(1).Size.y, 0.01f);
            flow.SetItemCount(2, true);
            Assert.AreEqual(60, flow.GetItemLayout(1).Size.y, 0.01f);
        }

        [Test]
        public void FixedFlowSupportsHugeCountWithoutPerItemCaches()
        {
            var flow = new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, Vector2.one, allowMultiple: true);
            flow.SetViewportSize(new Vector2(100, 300));
            flow.SetItemCount(int.MaxValue, true);
            Assert.AreEqual(100, flow.CountPerLine);
            AssertRange(flow.GetVisibleRange(100, 1, 0), 10000, 10099);
        }

        [TestCase(UGUIVirtualListAxis.Vertical, false)]
        [TestCase(UGUIVirtualListAxis.Horizontal, false)]
        [TestCase(UGUIVirtualListAxis.Vertical, true)]
        [TestCase(UGUIVirtualListAxis.Horizontal, true)]
        public void CoreViewportResizeKeepsAnchorAndBindingToken(UGUIVirtualListAxis axis, bool variable)
        {
            using var f = new FlowFixture(axis, variable);
            f.List.ScrollToView(60, UGUIVirtualListAlignment.Start);
            f.SetPosition(f.Position + 15);
            Assert.IsTrue(f.List.TryGetItemToken(60, out var token));
            float before = f.Visual(60);
            f.Scroll.viewport.sizeDelta = axis == UGUIVirtualListAxis.Vertical ? new Vector2(220, 300) : new Vector2(300, 220);
            Assert.IsTrue(f.List.RefreshViewportIfChanged());
            Assert.AreEqual(2, f.Layout.CountPerLine);
            Assert.AreEqual(before, f.Visual(60), 0.01f);
            Assert.IsTrue(token.IsCurrent, "已有 Item 只调整布局，不重新 Bind");
            Assert.IsFalse(f.List.RefreshViewportIfChanged());
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void VariableMultiLineSizeNotificationKeepsFollowingRowStable(UGUIVirtualListAxis axis)
        {
            using var f = new FlowFixture(axis, true);
            f.List.ScrollToView(60, UGUIVirtualListAlignment.Start);
            float before = f.Visual(60);
            Assert.IsTrue(f.List.TryGetItem(58, out var item));
            item.RectTransform.SetSizeWithCurrentAnchors(axis == UGUIVirtualListAxis.Vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, 180);
            Assert.IsTrue(f.List.NotifyItemSizeChanged(58));
            Assert.AreEqual(before, f.Visual(60), 0.01f);
            float position = f.Position;
            Assert.IsTrue(f.List.TryGetItem(57, out item));
            item.RectTransform.SetSizeWithCurrentAnchors(axis == UGUIVirtualListAxis.Vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, 120);
            f.List.NotifyItemSizeChanged(57);
            Assert.AreEqual(position, f.Position, 0.01f, "不是最高项的变化不应推动后面的行");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InactiveInspectorOptionsDoNotAffectRuntimeLayout(bool multiple)
        {
            var root = new GameObject("Inactive option test");
            try
            {
                var component = root.AddComponent<UGUIVirtualListComponent>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                void Set(string name, object value) => typeof(UGUIVirtualListComponent).GetField(name, flags).SetValue(component, value);
                Set("_allowMultiple", multiple);
                Set("_limitGridCount", !multiple);
                Set("_gridCount", -5); // 单列或不限制时，该值必须被忽略。
                Set("_spacing", multiple ? float.NaN : 8f);
                Set("_gridSpacing", multiple ? new Vector2(8, 8) : new Vector2(float.NaN, float.NaN));
                var layout = (UGUIFlowLayout)typeof(UGUIVirtualListComponent).GetMethod("CreateLayout", flags)
                    .Invoke(component, new object[] { new Vector2(100, 100) });
                layout.SetViewportSize(new Vector2(320, 300));
                Assert.AreEqual(multiple ? 3 : 1, layout.CountPerLine);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void BufferRejectsNonFiniteValuesWithoutChangingCurrentSetting()
        {
            using var f = new FlowFixture(UGUIVirtualListAxis.Vertical, false);
            Assert.Throws<ArgumentOutOfRangeException>(() => f.List.BufferSize = float.PositiveInfinity);
            Assert.Throws<ArgumentOutOfRangeException>(() => f.List.BufferSize = float.NaN);
            Assert.AreEqual(300, f.List.BufferSize);
        }

        [Test]
        public void LegacyGridSettingsMigrateToFixedSizeAndLimitedMultipleColumns()
        {
            var root = new GameObject("Legacy flow test");
            UnityEditor.Editor inspector = null;
            try
            {
                var component = root.AddComponent<UGUIVirtualListComponent>();
                typeof(UGUIVirtualListComponent).GetField("_layoutMode", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(component, UGUIVirtualListLayoutMode.FixedGrid);
                typeof(UGUIVirtualListComponent).GetField("_gridCount", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, 3);
                inspector = UnityEditor.Editor.CreateEditor(component, typeof(UGUIVirtualListComponentEditor));
                var settings = new SerializedObject(component);
                Assert.AreEqual(0, settings.FindProperty("_layoutMode").intValue);
                Assert.IsTrue(settings.FindProperty("_allowMultiple").boolValue);
                Assert.IsTrue(settings.FindProperty("_limitGridCount").boolValue);
                Assert.AreEqual(3, settings.FindProperty("_gridCount").intValue);
            }
            finally
            {
                if (inspector != null) UnityEngine.Object.DestroyImmediate(inspector);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static UGUIFlowLayout CreateVariableOrFixed(UGUIVirtualListAxis axis, bool variable)
        {
            var flow = new UGUIFlowLayout(axis, axis == UGUIVirtualListAxis.Vertical ? new Vector2(100, 60) : new Vector2(60, 100),
                variableSize: variable, allowMultiple: true,
                spacing: axis == UGUIVirtualListAxis.Vertical ? new Vector2(8, 10) : new Vector2(10, 8), paddingStart: 12, paddingEnd: 20);
            flow.SetViewportSize(axis == UGUIVirtualListAxis.Vertical ? new Vector2(320, 300) : new Vector2(300, 320));
            return flow;
        }
        private static UGUIFlowLayout MeasuredFlow(UGUIVirtualListAxis axis)
        {
            var flow = CreateVariableOrFixed(axis, true);
            flow.SetItemCount(8, true);
            var sizes = new[] { 30, 120, 70, 50, 90, 20, 40, 65 };
            for (int i = 0; i < sizes.Length; i++) SetMainSize(flow, axis, i, sizes[i]);
            return flow;
        }
        private static void SetMainSize(UGUIFlowLayout flow, UGUIVirtualListAxis axis, int index, float main) =>
            flow.SetItemSize(index, axis == UGUIVirtualListAxis.Vertical ? new Vector2(100, main) : new Vector2(main, 100));
        private static void AssertRange(UGUIVirtualListRange range, int start, int end)
        {
            Assert.AreEqual(start, range.Start);
            Assert.AreEqual(end, range.End);
        }

        private sealed class FlowFixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly ScrollRect Scroll;
            public readonly UGUIFlowLayout Layout;
            public readonly UGUIVirtualList List;
            private readonly UGUIVirtualListAxis _axis;
            public FlowFixture(UGUIVirtualListAxis axis, bool variable)
            {
                _axis = axis;
                Root = new GameObject("Flow test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                Scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                Scroll.viewport = (RectTransform)viewport.transform;
                Scroll.viewport.sizeDelta = axis == UGUIVirtualListAxis.Vertical ? new Vector2(320, 300) : new Vector2(300, 320);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                Scroll.content = (RectTransform)content.transform;
                Scroll.vertical = axis == UGUIVirtualListAxis.Vertical;
                Scroll.horizontal = !Scroll.vertical;
                Scroll.movementType = ScrollRect.MovementType.Clamped;
                Layout = new UGUIFlowLayout(axis, new Vector2(100, 100), variable, true, spacing: new Vector2(10, 10));
                List = new UGUIVirtualList(Scroll, Layout, this) { BufferSize = 300 };
                var data = new List<int>();
                for (int i = 0; i < 1000; i++) data.Add(i);
                List.SetDatas(data);
            }
            public float Position => _axis == UGUIVirtualListAxis.Vertical ? Scroll.content.anchoredPosition.y : -Scroll.content.anchoredPosition.x;
            public void SetPosition(float position)
            {
                var value = Scroll.content.anchoredPosition;
                if (_axis == UGUIVirtualListAxis.Vertical) value.y = position; else value.x = -position;
                Scroll.content.anchoredPosition = value;
                Scroll.onValueChanged.Invoke(Vector2.zero);
            }
            public float Visual(int index)
            {
                Assert.IsTrue(List.TryGetItem(index, out var item));
                var position = item.RectTransform.anchoredPosition + Scroll.content.anchoredPosition;
                return _axis == UGUIVirtualListAxis.Vertical ? -position.y : position.x;
            }
            public int GetItemType(int index, object data) => index % 2;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Flow item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose() { List.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject gameObject) { GameObject = gameObject; }
            public void Bind(int index, object data) => RectTransform.sizeDelta = new Vector2(100, 100);
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }
}
