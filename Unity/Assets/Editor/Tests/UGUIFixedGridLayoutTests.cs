using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIFixedGridLayoutTests
    {
        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void PositionsAndContentIncludeSpacingPaddingAndPartialLastLine(UGUIVirtualListAxis axis)
        {
            var layout = CreateLayout(axis);
            layout.SetItemCount(8, true);
            Assert.AreEqual(232f, layout.ContentLength, 0.01f);
            Assert.AreEqual(316f, layout.CrossSize, 0.01f);
            Assert.IsFalse(layout.SupportsDynamicSize);
            var info = layout.GetItemLayout(4);
            Assert.AreEqual(axis == UGUIVirtualListAxis.Vertical ? new Vector2(108, -82) : new Vector2(82, -108), info.Position);
            Assert.AreEqual(axis == UGUIVirtualListAxis.Vertical ? new Vector2(100, 60) : new Vector2(60, 100), info.Size);
            info = layout.GetItemLayout(7);
            Assert.AreEqual(axis == UGUIVirtualListAxis.Vertical ? new Vector2(108, -152) : new Vector2(152, -108), info.Position);
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void VisibleRangeIncludesWholeLinesAndClampsPartialLastLine(UGUIVirtualListAxis axis)
        {
            var layout = CreateLayout(axis);
            layout.SetItemCount(8, true);
            AssertRange(layout.GetVisibleRange(12, 60, 0), 0, 2);
            AssertRange(layout.GetVisibleRange(82, 60, 0), 3, 5);
            AssertRange(layout.GetVisibleRange(152, 60, 0), 6, 7);
            AssertRange(layout.GetVisibleRange(72, 10, 10), 0, 5);
        }

        [Test]
        public void PaddingGapZeroViewportAndOutsideContentCanReturnEmpty()
        {
            var layout = CreateLayout(UGUIVirtualListAxis.Vertical);
            layout.SetItemCount(8, true);
            Assert.IsTrue(layout.GetVisibleRange(0, 12, 0).IsEmpty);
            Assert.IsTrue(layout.GetVisibleRange(72, 10, 0).IsEmpty);
            Assert.IsTrue(layout.GetVisibleRange(212, 20, 0).IsEmpty);
            Assert.IsTrue(layout.GetVisibleRange(1000, 300, 0).IsEmpty);
            Assert.IsTrue(layout.GetVisibleRange(0, 0, 100).IsEmpty);
            AssertRange(layout.GetVisibleRange(-20, 40, 0), 0, 2);
        }

        [Test]
        public void EmptyAndSmallCountsHaveValidBounds()
        {
            var layout = CreateLayout(UGUIVirtualListAxis.Vertical);
            layout.SetItemCount(0, true);
            Assert.AreEqual(32, layout.ContentLength, 0.01f);
            Assert.AreEqual(0, layout.CrossSize);
            Assert.IsTrue(layout.GetVisibleRange(0, 100, 0).IsEmpty);
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetItemLayout(0));
            layout.SetItemCount(2, true);
            Assert.AreEqual(92, layout.ContentLength, 0.01f);
            Assert.AreEqual(208, layout.CrossSize, 0.01f);
            AssertRange(layout.GetVisibleRange(0, 1000, 0), 0, 1);
        }

        [Test]
        public void SingleCellPerLineMatchesFixedLinearGeometry()
        {
            foreach (var axis in new[] { UGUIVirtualListAxis.Vertical, UGUIVirtualListAxis.Horizontal })
            {
                var grid = new UGUIFixedGridLayout(axis, new Vector2(100, 80), 1, new Vector2(8, 8), 12, 20);
                var linear = new UGUIFixedLinearLayout(axis, new Vector2(100, 80), 8, 12, 20);
                grid.SetItemCount(20, true);
                linear.SetItemCount(20, true);
                Assert.AreEqual(linear.ContentLength, grid.ContentLength, 0.01f);
                for (int i = 0; i < 20; i++)
                {
                    Assert.AreEqual(linear.GetItemLayout(i).Position, grid.GetItemLayout(i).Position);
                    Assert.AreEqual(linear.GetItemLayout(i).Size, grid.GetItemLayout(i).Size);
                }
            }
        }

        [Test]
        public void GridUsesNoPerItemStorageAndSupportsLargeCount()
        {
            var grid = new UGUIFixedGridLayout(UGUIVirtualListAxis.Vertical, Vector2.one, 4);
            grid.SetItemCount(int.MaxValue, true);
            Assert.Greater(grid.ContentLength, 0f);
            var range = grid.GetVisibleRange(10000, 100, 0);
            AssertRange(range, 40000, 40399);
        }

        [Test]
        public void InvalidConfigurationIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UGUIFixedGridLayout(UGUIVirtualListAxis.Vertical, Vector2.one, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UGUIFixedGridLayout(UGUIVirtualListAxis.Vertical, Vector2.zero, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UGUIFixedGridLayout(UGUIVirtualListAxis.Vertical, new Vector2(float.NaN, 1), 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UGUIFixedGridLayout(UGUIVirtualListAxis.Vertical, Vector2.one, 3, new Vector2(-1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UGUIFixedGridLayout(UGUIVirtualListAxis.Vertical, Vector2.one, 3, paddingStart: float.PositiveInfinity));
            var grid = CreateLayout(UGUIVirtualListAxis.Vertical);
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetItemCount(-1, true));
        }

        [Test]
        public void FiniteInputsCannotOverflowIntoInfiniteLayout()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UGUIFixedGridLayout(
                UGUIVirtualListAxis.Vertical, new Vector2(1, float.MaxValue), 1, new Vector2(0, float.MaxValue)));
            var mainOverflow = new UGUIFixedGridLayout(UGUIVirtualListAxis.Vertical, new Vector2(1, float.MaxValue), 1);
            mainOverflow.SetItemCount(1, true);
            Assert.Throws<ArgumentOutOfRangeException>(() => mainOverflow.SetItemCount(2, true));
            Assert.IsFalse(float.IsInfinity(mainOverflow.ContentLength));
            var crossOverflow = new UGUIFixedGridLayout(UGUIVirtualListAxis.Vertical, new Vector2(float.MaxValue, 1), 2);
            crossOverflow.SetItemCount(1, true);
            Assert.Throws<ArgumentOutOfRangeException>(() => crossOverflow.SetItemCount(2, true));
            Assert.IsFalse(float.IsInfinity(crossOverflow.CrossSize));
        }

        [Test]
        public void ExistingSerializedLayoutValuesRemainCompatible()
        {
            Assert.AreEqual(0, (int)UGUIVirtualListLayoutMode.FixedLinear);
            Assert.AreEqual(1, (int)UGUIVirtualListLayoutMode.VariableLinear);
            Assert.AreEqual(2, (int)UGUIVirtualListLayoutMode.FixedGrid);
            Assert.AreEqual(0, (int)UGUIVirtualListItemSizeMode.Manual);
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void GridVirtualizesReusesDifferentTypesAndSkipsMeasurement(UGUIVirtualListAxis axis)
        {
            var root = new GameObject("Grid Test", typeof(RectTransform), typeof(ScrollRect));
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(root.transform, false);
            ((RectTransform)viewport.transform).sizeDelta = new Vector2(320, 320);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = (RectTransform)content.transform;
            var factory = new GridFactory();
            var list = new UGUIVirtualList(scroll,
                new UGUIFixedGridLayout(axis, new Vector2(80, 80), 3, new Vector2(8, 8)), factory)
            {
                BufferSize = 40
            };
            try
            {
                var data = new List<int>();
                for (int i = 0; i < 10000; i++) data.Add(i);
                list.SetDatas(data);
                Assert.Less(factory.Created, 30);
                int before = factory.Created;
                list.ScrollToView(9001, UGUIVirtualListAlignment.Start);
                Assert.IsTrue(list.TryGetItem(9001, out var item));
                Assert.IsFalse(list.TryGetItem(0, out _));
                Assert.AreEqual(new Vector2(80, 80), item.RectTransform.rect.size);
                Assert.LessOrEqual(factory.Created, before + 12);
                Assert.AreEqual(0, factory.Measurements);
                int clicked = -1;
                list.ItemClicked += (index, value) => clicked = index;
                ((GridItem)item).Click();
                Assert.AreEqual(9001, clicked);
                list.ScrollToView(9999, UGUIVirtualListAlignment.End);
                Assert.IsTrue(list.TryGetItem(9999, out _));
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static UGUIFixedGridLayout CreateLayout(UGUIVirtualListAxis axis) => new UGUIFixedGridLayout(
            axis,
            axis == UGUIVirtualListAxis.Vertical ? new Vector2(100, 60) : new Vector2(60, 100),
            3,
            axis == UGUIVirtualListAxis.Vertical ? new Vector2(8, 10) : new Vector2(10, 8),
            12, 20);

        private static void AssertRange(UGUIVirtualListRange range, int start, int end)
        {
            Assert.AreEqual(start, range.Start);
            Assert.AreEqual(end, range.End);
        }

        private sealed class GridFactory : IUGUIVirtualListItemFactory
        {
            public int Created;
            public int Measurements;
            public int GetItemType(int index, object data) => index % 3;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                Created++;
                var go = new GameObject("Grid Item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new GridItem(go, this);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
        }

        private sealed class GridItem : IUGUIVirtualListItem
        {
            private readonly GridFactory _factory;
            private Action _click;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public GridItem(GameObject gameObject, GridFactory factory) { GameObject = gameObject; _factory = factory; }
            public void Bind(int index, object data) { RectTransform.sizeDelta = new Vector2(1000, 1000); }
            public Vector2 Measure() { _factory.Measurements++; return RectTransform.rect.size; }
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { _click = callback; }
            public void Click() => _click?.Invoke();
        }
    }
}
