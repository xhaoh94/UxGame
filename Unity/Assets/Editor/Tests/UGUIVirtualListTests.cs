using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualListTests
    {
        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void FixedLayoutCalculatesContentAndItemOffset(UGUIVirtualListAxis axis)
        {
            var layout = new UGUIFixedLinearLayout(axis, new Vector2(100, 100), 8, 12, 20);
            layout.SetItemCount(10, true);
            Assert.AreEqual(1104, layout.ContentLength, 0.01f);
            var info = layout.GetItemLayout(3);
            Assert.AreEqual(336, axis == UGUIVirtualListAxis.Vertical ? -info.Position.y : info.Position.x, 0.01f);
        }

        [Test]
        public void VariableLayoutCachesMeasurementAndResetsOnNewData()
        {
            var layout = new UGUIVariableLinearLayout(UGUIVirtualListAxis.Vertical, new Vector2(300, 100));
            layout.SetItemCount(10, true);
            Assert.IsTrue(layout.SetItemSize(0, new Vector2(300, 60)));
            Assert.AreEqual(960, layout.ContentLength, 0.01f);
            Assert.AreEqual(-60, layout.GetItemLayout(1).Position.y, 0.01f);
            layout.SetItemCount(10, true);
            Assert.AreEqual(1000, layout.ContentLength, 0.01f);
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void VariableListMeasuresBindSizeBeforeOverwritingGeometry(UGUIVirtualListAxis axis)
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(axis);
            var list = new UGUIVirtualList(scroll,
                new UGUIVariableLinearLayout(axis, new Vector2(100, 100)), factory);
            try
            {
                list.SetDatas(new[] { 60f, 140f, 90f, 110f });
                Assert.IsTrue(list.TryGetItem(0, out var item));
                float actual = axis == UGUIVirtualListAxis.Vertical
                    ? item.RectTransform.rect.height : item.RectTransform.rect.width;
                Assert.AreEqual(60, actual, 0.01f);
            }
            finally
            {
                list.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void DistantScrollReusesItemsAndShowsRequestedIndex(UGUIVirtualListAxis axis)
        {
            var root = CreateScroll(out var scroll);
            var factory = new TestFactory(axis);
            var list = new UGUIVirtualList(scroll,
                new UGUIFixedLinearLayout(axis, new Vector2(100, 100)), factory);
            try
            {
                var data = new List<float>();
                for (int i = 0; i < 1000; i++) data.Add(100);
                list.SetDatas(data);
                Assert.Less(factory.Created, 20);
                int before = factory.Created;
                list.ScrollToView(500, UGUIVirtualListAlignment.Start);
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

        private sealed class TestFactory : IUGUIVirtualListItemFactory
        {
            private readonly UGUIVirtualListAxis _axis;
            public int Created { get; private set; }
            public TestFactory(UGUIVirtualListAxis axis) { _axis = axis; }
            public int GetItemType(int index, object data) => index % 2;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                Created++;
                var go = new GameObject("Test Item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new TestItem(go, _axis);
            }
            public void Destroy(IUGUIVirtualListItem item)
            {
                if (item.GameObject != null) UnityEngine.Object.DestroyImmediate(item.GameObject);
            }
        }

        private sealed class TestItem : IUGUIVirtualListItem
        {
            private readonly UGUIVirtualListAxis _axis;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public TestItem(GameObject gameObject, UGUIVirtualListAxis axis)
            {
                GameObject = gameObject;
                _axis = axis;
            }
            public void Bind(int index, object data)
            {
                RectTransform.SetSizeWithCurrentAnchors(
                    _axis == UGUIVirtualListAxis.Vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal,
                    (float)data);
            }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }
}
