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
            Assert.AreEqual(expected, UIFlowLayout.CalculateCountPerLine(available, size, gap, true, cap));
            Assert.AreEqual(1, UIFlowLayout.CalculateCountPerLine(available, size, gap, false, cap));
        }

        [TestCase(UIVirtualListAxis.Vertical)]
        [TestCase(UIVirtualListAxis.Horizontal)]
        public void SingleLineIsTheSameFixedAlgorithmWithOneCellPerLine(UIVirtualListAxis axis)
        {
            var flow = new UIFlowLayout(axis, new Vector2(100, 80), spacing: new Vector2(8, 8), paddingStart: 12, paddingEnd: 20);
            flow.SetViewportSize(new Vector2(2000, 2000));
            flow.SetItemCount(100, true);
            var old = new UIFixedLinearLayout(axis, new Vector2(100, 80), 8, 12, 20);
            old.SetItemCount(100, true);
            Assert.AreEqual(1, flow.CountPerLine);
            Assert.AreEqual(old.ContentLength, flow.ContentLength, 0.01f);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(old.GetItemLayout(i).Position, flow.GetItemLayout(i).Position);
        }

        [TestCase(UIVirtualListAxis.Vertical)]
        [TestCase(UIVirtualListAxis.Horizontal)]
        public void FixedFlowRecalculatesCapacityWhenViewportChanges(UIVirtualListAxis axis)
        {
            var flow = CreateVariableOrFixed(axis, false);
            flow.SetItemCount(10, true);
            Assert.AreEqual(3, flow.CountPerLine);
            Assert.AreEqual(302, flow.ContentLength, 0.01f);
            Assert.IsTrue(flow.SetViewportSize(axis == UIVirtualListAxis.Vertical ? new Vector2(220, 300) : new Vector2(300, 220)));
            Assert.AreEqual(2, flow.CountPerLine);
            Assert.AreEqual(372, flow.ContentLength, 0.01f);
            var pos = flow.GetItemLayout(7).Position;
            Assert.AreEqual(axis == UIVirtualListAxis.Vertical ? new Vector2(108, -222) : new Vector2(222, -108), pos);
        }

        [TestCase(UIVirtualListAxis.Vertical)]
        [TestCase(UIVirtualListAxis.Horizontal)]
        public void VariableFlowUsesLargestItemOfEachLineAndDoesNotOverlap(UIVirtualListAxis axis)
        {
            var flow = MeasuredFlow(axis);
            Assert.AreEqual(327, flow.ContentLength, 0.01f);
            Assert.AreEqual(316, flow.CrossSize, 0.01f);
            Assert.AreEqual(axis == UIVirtualListAxis.Vertical ? new Vector2(108, -142) : new Vector2(142, -108), flow.GetItemLayout(4).Position);
            Assert.AreEqual(axis == UIVirtualListAxis.Vertical ? new Vector2(0, -242) : new Vector2(242, 0), flow.GetItemLayout(6).Position);
            SetMainSize(flow, axis, 1, 40);
            Assert.AreEqual(axis == UIVirtualListAxis.Vertical ? -92 : 92,
                axis == UIVirtualListAxis.Vertical ? flow.GetItemLayout(3).Position.y : flow.GetItemLayout(3).Position.x, 0.01f);
            SetMainSize(flow, axis, 0, 150);
            Assert.AreEqual(axis == UIVirtualListAxis.Vertical ? -172 : 172,
                axis == UIVirtualListAxis.Vertical ? flow.GetItemLayout(3).Position.y : flow.GetItemLayout(3).Position.x, 0.01f);
        }

        [Test]
        public void VariableFlowQueriesWholeLinesAndCanReturnEmptyGap()
        {
            var flow = MeasuredFlow(UIVirtualListAxis.Vertical);
            AssertRange(flow.GetVisibleRange(12, 120, 0), 0, 2);
            Assert.IsTrue(flow.GetVisibleRange(132, 10, 0).IsEmpty);
            AssertRange(flow.GetVisibleRange(142, 90, 0), 3, 5);
            AssertRange(flow.GetVisibleRange(242, 65, 0), 6, 7);
            Assert.IsTrue(flow.GetVisibleRange(307, 20, 0).IsEmpty);
        }

        [Test]
        public void ResizeAndCountChangesKeepKnownMeasurementsUntilExplicitReset()
        {
            var flow = MeasuredFlow(UIVirtualListAxis.Vertical);
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
            var flow = new UIFlowLayout(UIVirtualListAxis.Vertical, Vector2.one, allowMultiple: true);
            flow.SetViewportSize(new Vector2(100, 300));
            flow.SetItemCount(int.MaxValue, true);
            Assert.AreEqual(100, flow.CountPerLine);
            AssertRange(flow.GetVisibleRange(100, 1, 0), 10000, 10099);
        }

        [TestCase(UIVirtualListAxis.Vertical, false)]
        [TestCase(UIVirtualListAxis.Horizontal, false)]
        [TestCase(UIVirtualListAxis.Vertical, true)]
        [TestCase(UIVirtualListAxis.Horizontal, true)]
        public void CoreViewportResizeKeepsAnchorAndBindingToken(UIVirtualListAxis axis, bool variable)
        {
            using var f = new FlowFixture(axis, variable);
            f.List.ScrollToView(60, UIVirtualListAlignment.Start);
            f.SetPosition(f.Position + 15);
            Assert.IsTrue(f.List.TryGetItemToken(60, out var token));
            float before = f.Visual(60);
            f.Scroll.viewport.sizeDelta = axis == UIVirtualListAxis.Vertical ? new Vector2(220, 300) : new Vector2(300, 220);
            Assert.IsTrue(f.List.RefreshViewportIfChanged());
            Assert.AreEqual(2, f.Layout.CountPerLine);
            Assert.AreEqual(before, f.Visual(60), 0.01f);
            Assert.IsTrue(token.IsCurrent, "已有 Item 只调整布局，不重新 Bind");
            Assert.IsFalse(f.List.RefreshViewportIfChanged());
        }

        [TestCase(UIVirtualListAxis.Vertical)]
        [TestCase(UIVirtualListAxis.Horizontal)]
        public void VariableMultiLineSizeNotificationKeepsFollowingRowStable(UIVirtualListAxis axis)
        {
            using var f = new FlowFixture(axis, true);
            f.List.ScrollToView(60, UIVirtualListAlignment.Start);
            float before = f.Visual(60);
            Assert.IsTrue(f.List.TryGetItem(58, out var item));
            item.RectTransform.SetSizeWithCurrentAnchors(axis == UIVirtualListAxis.Vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, 180);
            Assert.IsTrue(f.List.NotifyItemSizeChanged(58));
            Assert.AreEqual(before, f.Visual(60), 0.01f);
            float position = f.Position;
            Assert.IsTrue(f.List.TryGetItem(57, out item));
            item.RectTransform.SetSizeWithCurrentAnchors(axis == UIVirtualListAxis.Vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, 120);
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
                var component = root.AddComponent<UIVirtualListComponent>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                void Set(string name, object value) => typeof(UIVirtualListComponent).GetField(name, flags).SetValue(component, value);
                Set("_allowMultiple", multiple);
                Set("_limitGridCount", !multiple);
                Set("_gridCount", -5); // 单列或不限制时，该值必须被忽略。
                Set("_spacing", multiple ? float.NaN : 8f);
                Set("_gridSpacing", multiple ? new Vector2(8, 8) : new Vector2(float.NaN, float.NaN));
                var layout = (UIFlowLayout)typeof(UIVirtualListComponent).GetMethod("CreateLayout", flags)
                    .Invoke(component, new object[] { new Vector2(100, 100) });
                layout.SetViewportSize(new Vector2(320, 300));
                Assert.AreEqual(multiple ? 3 : 1, layout.CountPerLine);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void BufferRejectsNonFiniteValuesWithoutChangingCurrentSetting()
        {
            using var f = new FlowFixture(UIVirtualListAxis.Vertical, false);
            Assert.Throws<ArgumentOutOfRangeException>(() => f.List.BufferSize = float.PositiveInfinity);
            Assert.Throws<ArgumentOutOfRangeException>(() => f.List.BufferSize = float.NaN);
            Assert.AreEqual(300, f.List.BufferSize);
        }

        private static UIFlowLayout CreateVariableOrFixed(UIVirtualListAxis axis, bool variable)
        {
            var flow = new UIFlowLayout(axis, axis == UIVirtualListAxis.Vertical ? new Vector2(100, 60) : new Vector2(60, 100),
                variableSize: variable, allowMultiple: true,
                spacing: axis == UIVirtualListAxis.Vertical ? new Vector2(8, 10) : new Vector2(10, 8), paddingStart: 12, paddingEnd: 20);
            flow.SetViewportSize(axis == UIVirtualListAxis.Vertical ? new Vector2(320, 300) : new Vector2(300, 320));
            return flow;
        }
        private static UIFlowLayout MeasuredFlow(UIVirtualListAxis axis)
        {
            var flow = CreateVariableOrFixed(axis, true);
            flow.SetItemCount(8, true);
            var sizes = new[] { 30, 120, 70, 50, 90, 20, 40, 65 };
            for (int i = 0; i < sizes.Length; i++) SetMainSize(flow, axis, i, sizes[i]);
            return flow;
        }
        private static void SetMainSize(UIFlowLayout flow, UIVirtualListAxis axis, int index, float main) =>
            flow.SetItemSize(index, axis == UIVirtualListAxis.Vertical ? new Vector2(100, main) : new Vector2(main, 100));
        private static void AssertRange(UIVirtualListRange range, int start, int end)
        {
            Assert.AreEqual(start, range.Start);
            Assert.AreEqual(end, range.End);
        }

        private sealed class FlowFixture : IDisposable, IUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly ScrollRect Scroll;
            public readonly UIFlowLayout Layout;
            public readonly UIVirtualList List;
            private readonly UIVirtualListAxis _axis;
            public FlowFixture(UIVirtualListAxis axis, bool variable)
            {
                _axis = axis;
                Root = new GameObject("Flow test", typeof(RectTransform), typeof(UIVirtualListScrollRect));
                Scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                Scroll.viewport = (RectTransform)viewport.transform;
                Scroll.viewport.sizeDelta = axis == UIVirtualListAxis.Vertical ? new Vector2(320, 300) : new Vector2(300, 320);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                Scroll.content = (RectTransform)content.transform;
                Scroll.vertical = axis == UIVirtualListAxis.Vertical;
                Scroll.horizontal = !Scroll.vertical;
                Scroll.movementType = ScrollRect.MovementType.Clamped;
                Layout = new UIFlowLayout(axis, new Vector2(100, 100), variable, true, spacing: new Vector2(10, 10));
                List = new UIVirtualList(Scroll, Layout, this) { BufferSize = 300 };
                var data = new List<int>();
                for (int i = 0; i < 1000; i++) data.Add(i);
                List.SetDatas(data);
            }
            public float Position => _axis == UIVirtualListAxis.Vertical ? Scroll.content.anchoredPosition.y : -Scroll.content.anchoredPosition.x;
            public void SetPosition(float position)
            {
                var value = Scroll.content.anchoredPosition;
                if (_axis == UIVirtualListAxis.Vertical) value.y = position; else value.x = -position;
                Scroll.content.anchoredPosition = value;
                Scroll.onValueChanged.Invoke(Vector2.zero);
            }
            public float Visual(int index)
            {
                Assert.IsTrue(List.TryGetItem(index, out var item));
                var position = item.RectTransform.anchoredPosition + Scroll.content.anchoredPosition;
                return _axis == UIVirtualListAxis.Vertical ? -position.y : position.x;
            }
            public int GetItemType(int index, object data) => index % 2;
            public IUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Flow item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go);
            }
            public void Destroy(IUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose() { List.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
        private sealed class View : IUIVirtualListItem
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
