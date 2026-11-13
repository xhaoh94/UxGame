using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    /// <summary>
    /// 已测尺寸复用：数据引用与交叉轴不变时滑回视口不再测量；
    /// 数据替换/增删/Refresh 会失效；漏通知造成的尺寸漂移会自动纠正。
    /// </summary>
    public sealed class UIVirtualListMeasuredKeyTests
    {
        private GameObject _root;
        private ScrollRect _scroll;
        private UIVirtualList _list;
        private CountingFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("MeasuredKey Test", typeof(RectTransform), typeof(ScrollRect));
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(_root.transform, false);
            ((RectTransform)viewport.transform).sizeDelta = new Vector2(300f, 300f);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            _scroll = _root.GetComponent<ScrollRect>();
            _scroll.viewport = (RectTransform)viewport.transform;
            _scroll.content = (RectTransform)content.transform;

            _factory = new CountingFactory();
            var layout = new UIFlowLayout(
                UIVirtualListAxis.Vertical,
                itemSize: new Vector2(300f, 60f),
                variableSize: true);
            _list = new UIVirtualList(_scroll, layout, _factory);
        }

        [TearDown]
        public void TearDown()
        {
            _list?.Dispose();
            _list = null;
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void ScrollingBackReusesMeasuredSize()
        {
            _list.SetDatas(new object[50]);
            GoTo(10);
            int firstMeasure = _factory.CountFor(10);
            Assert.Greater(firstMeasure, 0, "首次进入视口必须测量一次");

            GoTo(40);
            GoTo(10);
            Assert.AreEqual(firstMeasure, _factory.CountFor(10),
                "数据没变，滑回来不应重复测量");
        }

        [Test]
        public void UpdateDataInvalidatesMeasuredSize()
        {
            _list.SetDatas(new object[50]);
            GoTo(10);
            int before = _factory.CountFor(10);

            _list.UpdateData(10, new object());
            GoTo(40);
            GoTo(10);
            Assert.Greater(_factory.CountFor(10), before, "替换数据后必须重测");
        }

        [Test]
        public void InsertBeforeMovesMeasuredKeyWithData()
        {
            _list.SetDatas(new object[50]);
            GoTo(10);
            int before = _factory.CountFor(10);

            GoTo(40);
            _list.InsertData(0, new object()); // 原 index 10 变成 11
            GoTo(11);
            Assert.AreEqual(before, _factory.CountFor(11),
                "插入后测量缓存必须跟着数据一起平移");
        }

        [Test]
        public void RefreshClearsMeasuredSizes()
        {
            _list.SetDatas(new object[50]);
            GoTo(10);
            int before = _factory.CountFor(10);

            GoTo(40);
            _list.Refresh();
            GoTo(10);
            Assert.Greater(_factory.CountFor(10), before,
                "Refresh 是强制刷新入口，不能复用旧测量结果");
        }

        [Test]
        public void ReuseDoesNotSilentlyRepairContentDrivenSize()
        {
            _list.SetDatas(new object[50]);
            GoTo(10);
            Assert.IsTrue(_list.TryGetItem(10, out var item));

            // 模拟业务改了内容高度，但漏调 NotifyItemSizeChanged。
            item.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200f);
            int before = _factory.CountFor(10);

            // 列表不做隐式修正：不重测，也不把主轴写回缓存值。
            // 错位会一直存在，直到业务发现并补上通知——这是刻意的“错误可见”。
            Assert.IsFalse(_list.RefreshViewportIfChanged(), "没有视口变化时不应自己刷新");
            Assert.AreEqual(before, _factory.CountFor(10), "不应隐式重测");
            Assert.AreEqual(200f, item.RectTransform.rect.height, 0.5f, "内容改的尺寸不应被列表覆盖");
        }

        private void GoTo(int index)
        {
            _list.ScrollToView(index, UIVirtualListAlignment.Start);
            _list.RefreshLayout();
        }

        private sealed class CountingFactory : IUIVirtualListItemFactory
        {
            private readonly Dictionary<int, int> _counts = new();
            public int CountFor(int index) => _counts.TryGetValue(index, out var count) ? count : 0;

            public int GetItemType(int index, object data) => 0;

            public IUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Counting Item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new CountingItem(go, this);
            }

            public void Destroy(IUIVirtualListItem item)
            {
                if (item.GameObject != null) UnityEngine.Object.DestroyImmediate(item.GameObject);
            }

            public void Report(int index)
            {
                if (index < 0) return;
                _counts[index] = CountFor(index) + 1;
            }
        }

        private sealed class CountingItem : IUIVirtualListItem
        {
            private readonly CountingFactory _owner;
            private int _index = -1;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;

            public CountingItem(GameObject gameObject, CountingFactory owner)
            {
                GameObject = gameObject;
                _owner = owner;
            }

            public void Bind(int index, object data) => _index = index;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }

            public Vector2 Measure()
            {
                _owner.Report(_index);
                return new Vector2(300f, 60f);
            }
        }
    }
}
