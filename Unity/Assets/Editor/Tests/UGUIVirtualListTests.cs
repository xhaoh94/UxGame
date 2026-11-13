using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualListTests
    {
        [TestCase(UIVirtualListAxis.Vertical)]
        [TestCase(UIVirtualListAxis.Horizontal)]
        public void FixedLayoutCalculatesContentAndItemOffset(UIVirtualListAxis axis)
        {
            var layout = new UIFixedLinearLayout(axis, new Vector2(100, 100), 8, 12, 20);
            layout.SetItemCount(10, true);
            Assert.AreEqual(1104, layout.ContentLength, 0.01f);
            var info = layout.GetItemLayout(3);
            Assert.AreEqual(336, axis == UIVirtualListAxis.Vertical ? -info.Position.y : info.Position.x, 0.01f);
        }

        [Test]
        public void VariableLayoutCachesMeasurementAndResetsOnNewData()
        {
            var layout = new UIVariableLinearLayout(UIVirtualListAxis.Vertical, new Vector2(300, 100));
            layout.SetItemCount(10, true);
            Assert.IsTrue(layout.SetItemSize(0, new Vector2(300, 60)));
            Assert.AreEqual(960, layout.ContentLength, 0.01f);
            Assert.AreEqual(-60, layout.GetItemLayout(1).Position.y, 0.01f);
            layout.SetItemCount(10, true);
            Assert.AreEqual(1000, layout.ContentLength, 0.01f);
        }

        [TestCase(UIVirtualListAxis.Vertical)]
        [TestCase(UIVirtualListAxis.Horizontal)]
        public void VariableListMeasuresBindSizeBeforeOverwritingGeometry(UIVirtualListAxis axis)
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(axis);
            var list = new UIVirtualList(scroll,
                new UIVariableLinearLayout(axis, new Vector2(100, 100)), factory);
            try
            {
                list.SetDatas(new[] { 60f, 140f, 90f, 110f });
                Assert.IsTrue(list.TryGetItem(0, out var item));
                float actual = axis == UIVirtualListAxis.Vertical
                    ? item.RectTransform.rect.height : item.RectTransform.rect.width;
                Assert.AreEqual(60, actual, 0.01f);
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(UIVirtualListAxis.Vertical)]
        [TestCase(UIVirtualListAxis.Horizontal)]
        public void DistantScrollReusesItemsAndShowsRequestedIndex(UIVirtualListAxis axis)
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(axis);
            var list = new UIVirtualList(scroll,
                new UIFixedLinearLayout(axis, new Vector2(100, 100)), factory);
            try
            {
                var data = new List<float>();
                for (int i = 0; i < 1000; i++) data.Add(100);
                list.SetDatas(data);
                Assert.Less(factory.Created, 20);
                int before = factory.Created;
                list.ScrollToView(500, UIVirtualListAlignment.Start);
                Assert.IsTrue(list.TryGetItem(500, out _));
                Assert.IsFalse(list.TryGetItem(0, out _));
                Assert.LessOrEqual(factory.Created, before + 4);
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static List<int> CollectActiveIndices(UIVirtualList list)
        {
            var indices = new List<int>();
            for (int i = 0; i < list.RenderCount; i++)
                if (list.TryGetItem(i, out _)) indices.Add(i);
            return indices;
        }

        private static int SiblingOf(UIVirtualList list, int index)
        {
            Assert.IsTrue(list.TryGetItem(index, out var item), $"索引 {index} 应当已激活");
            return item.RectTransform.GetSiblingIndex();
        }

        private static float[] FixedData(int count, float size = 100f)
        {
            var data = new float[count];
            for (int i = 0; i < count; i++) data[i] = size;
            return data;
        }

        private static UIVirtualList CreateVerticalList(ScrollRect scroll, TestFactory factory) =>
            new UIVirtualList(scroll, new UIFixedLinearLayout(UIVirtualListAxis.Vertical, new Vector2(100, 100)), factory);

        [Test]
        public void ForwardRenderOrderPutsLaterIndexOnTop()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.RenderOrder = UIVirtualListRenderOrder.Forward;
                list.SetDatas(FixedData(10));
                var indices = CollectActiveIndices(list);
                Assert.Greater(indices.Count, 2);
                for (int i = 1; i < indices.Count; i++)
                    Assert.Less(SiblingOf(list, indices[i - 1]), SiblingOf(list, indices[i]),
                        "正序时后一个索引必须在更上层");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReverseRenderOrderPutsEarlierIndexOnTop()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.SetDatas(FixedData(10));
                // 运行时切换也必须立即生效，不能等下一次滚动。
                list.RenderOrder = UIVirtualListRenderOrder.Reverse;
                var indices = CollectActiveIndices(list);
                Assert.Greater(indices.Count, 2);
                for (int i = 1; i < indices.Count; i++)
                    Assert.Greater(SiblingOf(list, indices[i - 1]), SiblingOf(list, indices[i]),
                        "倒序时后一个索引必须在更下层");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CenterRenderOrderFollowsViewportCenterWhileScrolling()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.RenderOrder = UIVirtualListRenderOrder.EdgeIn;
                list.SetDatas(FixedData(20));
                // 视口 300、Item 100、滚到 0：视口中心 150 落在索引 1 → 它必须在最上层。
                int center = SiblingOf(list, 1);
                foreach (int index in CollectActiveIndices(list))
                    if (index != 1) Assert.Less(SiblingOf(list, index), center, "两侧项必须压在中心项下面");

                // 滚到索引 3 顶部：视口中心变成 450，落在索引 4 上。
                list.ScrollToView(3, UIVirtualListAlignment.Start);
                Assert.IsTrue(list.TryGetItem(4, out _));
                int moved = SiblingOf(list, 4);
                foreach (int index in CollectActiveIndices(list))
                    if (index != 4) Assert.Less(SiblingOf(list, index), moved, "滚动后中心项必须重新置顶");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CenterOutRenderOrderPutsCenterItemAtBottom()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.RenderOrder = UIVirtualListRenderOrder.CenterOut;
                list.SetDatas(FixedData(20));
                int center = SiblingOf(list, 1);
                foreach (int index in CollectActiveIndices(list))
                    if (index != 1) Assert.Greater(SiblingOf(list, index), center, "中心项必须在最下层");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UnmanagedRenderOrderLeavesSiblingsUntouched()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.SetDatas(FixedData(10));
                var indices = CollectActiveIndices(list);
                Assert.Greater(indices.Count, 2);
                for (int i = 0; i < indices.Count; i++)
                    Assert.AreEqual(i, SiblingOf(list, indices[i]), "不干预时保持创建顺序");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BringToFrontAndBackOverrideRenderOrder()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.RenderOrder = UIVirtualListRenderOrder.Forward;
                list.SetDatas(FixedData(10));

                list.BringToFront(0);
                int top = SiblingOf(list, 0);
                foreach (int index in CollectActiveIndices(list))
                    if (index != 0) Assert.Less(SiblingOf(list, index), top, "置顶项必须在最上层");

                list.BringToBack(2);
                Assert.AreEqual(0, SiblingOf(list, 2), "置底项必须是最小兄弟序号");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PinnedItemSurvivesLayoutRebuildAndInsertion()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.RenderOrder = UIVirtualListRenderOrder.Forward;
                list.SetDatas(FixedData(10));
                list.BringToFront(3);

                // 滚动会触发重排，提权必须重新应用。
                list.ScrollToView(2, UIVirtualListAlignment.Start);
                Assert.IsTrue(list.TryGetItem(3, out _));
                int top = SiblingOf(list, 3);
                foreach (int index in CollectActiveIndices(list))
                    if (index != 3) Assert.Less(SiblingOf(list, index), top, "滚动后提权项必须仍在最上层");

                // 头部插入一条：原索引 3 变成 4，提权要跟着偏移。
                list.InsertData(0, 100f);
                Assert.IsTrue(list.TryGetItem(4, out _));
                int moved = SiblingOf(list, 4);
                foreach (int index in CollectActiveIndices(list))
                    if (index != 4) Assert.Less(SiblingOf(list, index), moved, "插入后提权项必须跟着索引偏移");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClearPinnedRestoresNaturalOrder()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.RenderOrder = UIVirtualListRenderOrder.Forward;
                list.SetDatas(FixedData(10));
                list.BringToFront(0);
                list.ClearPinned();
                Assert.Less(SiblingOf(list, 0), SiblingOf(list, 1), "清除提权后回到正序");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UpdateDatasKeepsScrollPosition()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.SetDatas(FixedData(100));
                list.ScrollToView(50, UIVirtualListAlignment.Start);
                float before = list.ScrollPositionValue;
                Assert.Greater(before, 0f);

                list.UpdateDatas(FixedData(100));
                Assert.AreEqual(before, list.ScrollPositionValue, 1f);
                Assert.IsTrue(list.TryGetItem(50, out _), "保持位置后原首项仍在可视范围");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UpdateDatasClampsAnchorWhenDataShrinks()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.SetDatas(FixedData(100));
                list.ScrollToView(50, UIVirtualListAlignment.Start);
                list.UpdateDatas(FixedData(10));
                Assert.IsTrue(list.TryGetItem(9, out _), "锚点越界时夹到最后一项");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetDatasStillReturnsToTop()
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(UIVirtualListAxis.Vertical);
            var list = CreateVerticalList(scroll, factory);
            try
            {
                list.SetDatas(FixedData(100));
                list.ScrollToView(50, UIVirtualListAlignment.Start);
                list.SetDatas(FixedData(100));
                Assert.AreEqual(0f, list.ScrollPositionValue, 1f, "SetDatas 仍然回顶部");
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateScroll(out ScrollRect scroll)
        {
            var root = new GameObject("VirtualList Test", typeof(RectTransform), typeof(ScrollRect));
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(root.transform, false);
            ((RectTransform)viewport.transform).sizeDelta = new Vector2(300, 300);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = (RectTransform)content.transform;
            return root;
        }

        private sealed class TestFactory : IUIVirtualListItemFactory
        {
            private readonly UIVirtualListAxis _axis;
            public int Created { get; private set; }
            public TestFactory(UIVirtualListAxis axis) { _axis = axis; }
            public int GetItemType(int index, object data) => index % 2;
            public IUIVirtualListItem Create(int itemType, Transform parent)
            {
                Created++;
                var go = new GameObject("Test Item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new TestItem(go, _axis);
            }
            public void Destroy(IUIVirtualListItem item)
            {
                if (item.GameObject != null) UnityEngine.Object.DestroyImmediate(item.GameObject);
            }
        }

        private sealed class TestItem : IUIVirtualListItem
        {
            private readonly UIVirtualListAxis _axis;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public TestItem(GameObject gameObject, UIVirtualListAxis axis)
            {
                GameObject = gameObject;
                _axis = axis;
            }
            public void Bind(int index, object data)
            {
                RectTransform.SetSizeWithCurrentAnchors(
                    _axis == UIVirtualListAxis.Vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal,
                    (float)data);
            }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }
}
