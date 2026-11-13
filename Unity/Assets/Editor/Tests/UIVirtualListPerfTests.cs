using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    /// <summary>
    /// 性能相关的行为回归：数据暂存交换不能串数据；测量抖动不能把列表拖进 20 轮全量重排。
    /// </summary>
    public sealed class UIVirtualListPerfTests
    {
        private GameObject _root;
        private ScrollRect _scroll;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Perf Test", typeof(RectTransform), typeof(ScrollRect));
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(_root.transform, false);
            ((RectTransform)viewport.transform).sizeDelta = new Vector2(300f, 300f);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            _scroll = _root.GetComponent<ScrollRect>();
            _scroll.viewport = (RectTransform)viewport.transform;
            _scroll.content = (RectTransform)content.transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void SetDatasTwiceKeepsDataConsistentAfterBufferSwap()
        {
            var list = CreateList(new FixedFactory(40f), variableSize: false);
            try
            {
                list.SetDatas(new[] { "a", "b", "c" });
                Assert.AreEqual(3, list.Count);
                Assert.AreEqual("a", list.GetData(0));

                // 第二次全量替换：暂存列表与 _datas 交换引用，数据不能串。
                list.SetDatas(new[] { "d", "e" });
                Assert.AreEqual(2, list.Count);
                Assert.AreEqual("d", list.GetData(0));
                Assert.AreEqual("e", list.GetData(1));

                list.AddData("f");
                Assert.AreEqual(3, list.Count);
                Assert.AreEqual("f", list.GetData(2));
            }
            finally { list.Dispose(); }
        }

        [Test]
        public void UnstableMeasureIsDeferredInsteadOfRunningFullPassLimit()
        {
            var factory = new OscillatingFactory();
            var list = CreateList(factory, variableSize: true);
            try
            {
                Assert.DoesNotThrow(() => list.SetDatas(new object[20]));
                // 每项在本轮刷新里最多测 3 次，其余留到下一帧。
                Assert.LessOrEqual(factory.MaxPasses, 3);
                Assert.Greater(factory.TotalMeasures, 0);
                Assert.IsTrue(list.HasPendingLayoutWork, "未收敛时应保留待处理状态给下一帧");
            }
            finally { list.Dispose(); }
        }

        private UIVirtualList CreateList(IUIVirtualListItemFactory factory, bool variableSize)
        {
            var layout = new UIFlowLayout(
                UIVirtualListAxis.Vertical,
                itemSize: new Vector2(300f, 40f),
                variableSize: variableSize);
            var list = new UIVirtualList(_scroll, layout, factory);
            if (factory is OscillatingFactory oscillating) oscillating.List = list;
            return list;
        }

        private sealed class FixedFactory : IUIVirtualListItemFactory
        {
            private readonly float _height;
            public FixedFactory(float height) => _height = height;
            public int GetItemType(int index, object data) => 0;

            public IUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Fixed Item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new FixedItem(go, _height);
            }

            public void Destroy(IUIVirtualListItem item)
            {
                if (item.GameObject != null) UnityEngine.Object.DestroyImmediate(item.GameObject);
            }
        }

        private sealed class FixedItem : IUIVirtualListItem
        {
            private readonly float _height;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;

            public FixedItem(GameObject gameObject, float height)
            {
                GameObject = gameObject;
                _height = height;
            }

            public void Bind(int index, object data) { }
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
            public Vector2 Measure() => new Vector2(300f, _height);
        }

        /// <summary>每项每次测量都换一个高度并主动通知，用来验证“抖动项”被限流而不是跑满全局限流。</summary>
        private sealed class OscillatingFactory : IUIVirtualListItemFactory
        {
            private readonly List<OscillatingItem> _items = new();
            public UIVirtualList List;
            public int TotalMeasures { get; private set; }
            public int MaxPasses { get; private set; }

            public int GetItemType(int index, object data) => 0;

            public IUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Oscillating Item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var item = new OscillatingItem(go, this);
                _items.Add(item);
                return item;
            }

            public void Destroy(IUIVirtualListItem item)
            {
                if (item.GameObject != null) UnityEngine.Object.DestroyImmediate(item.GameObject);
            }

            public void Report(int measures)
            {
                TotalMeasures += measures;
                if (measures > MaxPasses) MaxPasses = measures;
            }
        }

        private sealed class OscillatingItem : IUIVirtualListItem
        {
            private readonly OscillatingFactory _owner;
            private bool _tall;
            private int _measures;
            private int _index = -1;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;

            public OscillatingItem(GameObject gameObject, OscillatingFactory owner)
            {
                GameObject = gameObject;
                _owner = owner;
            }

            public void Bind(int index, object data)
            {
                _index = index;
                _measures = 0;
                _tall = false;
            }

            public void OnRecycle() => _owner.Report(_measures);
            public void SetClickHandler(Action callback) { }

            public Vector2 Measure()
            {
                _measures++;
                _owner.Report(_measures);
                _tall = !_tall;
                // 模拟业务在 Measure 里又改了尺寸并主动通知：
                // 这是唯一能触发“同一项在同一轮刷新里反复被测”的路径（Bind 只发生一次）。
                _owner.List?.NotifyItemSizeChanged(_index);
                return new Vector2(300f, _tall ? 80f : 40f);
            }
        }
    }
}
