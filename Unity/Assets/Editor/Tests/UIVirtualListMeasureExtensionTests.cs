using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    /// <summary>
    /// 测量扩展链：无扩展时保持原路径，有扩展时按注册顺序包成链，
    /// next 的返回值是权威尺寸，注册/注销都会让已有链失效。
    /// </summary>
    public sealed class UIVirtualListMeasureExtensionTests
    {
        private GameObject _root;
        private RectTransform _itemsRoot;
        private UIVirtualList _list;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("MeasureExt Test", typeof(RectTransform), typeof(ScrollRect));
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(_root.transform, false);
            ((RectTransform)viewport.transform).sizeDelta = new Vector2(300f, 300f);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            var itemsRoot = new GameObject("Items", typeof(RectTransform));
            itemsRoot.transform.SetParent(_root.transform, false);
            _itemsRoot = (RectTransform)itemsRoot.transform;

            var scroll = _root.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = (RectTransform)content.transform;

            _list = new UIVirtualList(
                scroll,
                new UIFixedLinearLayout(UIVirtualListAxis.Vertical, new Vector2(300f, 40f)),
                new NullFactory());
        }

        [TearDown]
        public void TearDown()
        {
            _list?.Dispose();
            _list = null;
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        private FixedItem NewItem(float height, float growth = 0f) => new FixedItem(_itemsRoot, height, growth);

        [Test]
        public void WithoutExtensionUsesItemMeasureDirectly()
        {
            var item = NewItem(50f);
            Assert.AreEqual(50f, _list.MeasureItem(0, item).y, 0.01f);
            Assert.AreEqual(1, item.MeasureCalls);
        }

        [Test]
        public void ExtensionWrapsAuthoritativeMeasureResult()
        {
            var item = NewItem(50f);
            _list.RegisterMeasureExtension(new ScaleExtension(2f));

            Assert.AreEqual(100f, _list.MeasureItem(0, item).y, 0.01f);
            Assert.AreEqual(1, item.MeasureCalls);
        }

        [Test]
        public void ExtensionsRunInRegistrationOrderOuterToInner()
        {
            var trace = new List<string>();
            _list.RegisterMeasureExtension(new TraceExtension("first", trace));
            _list.RegisterMeasureExtension(new TraceExtension("second", trace));

            _list.MeasureItem(0, NewItem(10f));

            // 先注册的在外层先执行，后注册的更靠近原始 Measure。
            CollectionAssert.AreEqual(new[] { "first", "second" }, trace);
        }

        [Test]
        public void UnregisterRebuildsTheCachedChain()
        {
            var item = NewItem(50f);
            var extension = new ScaleExtension(2f);
            _list.RegisterMeasureExtension(extension);

            Assert.AreEqual(100f, _list.MeasureItem(0, item).y, 0.01f);
            Assert.IsTrue(_list.UnregisterMeasureExtension(extension));
            Assert.AreEqual(50f, _list.MeasureItem(0, item).y, 0.01f);
        }

        [Test]
        public void RegisteringLaterExtensionInvalidatesCachedChain()
        {
            var item = NewItem(50f);
            _list.RegisterMeasureExtension(new ScaleExtension(2f));
            Assert.AreEqual(100f, _list.MeasureItem(0, item).y, 0.01f);

            // 链是按 item 实例缓存的：新增扩展后必须失效重建。
            // 先注册的 2f 在外层：50 * 3 * 2 = 300。
            _list.RegisterMeasureExtension(new ScaleExtension(3f));
            Assert.AreEqual(300f, _list.MeasureItem(0, item).y, 0.01f);
        }

        [Test]
        public void ExtensionCanCallNextRepeatedlyAndUseLatestResult()
        {
            var item = NewItem(20f, growth: 10f);
            _list.RegisterMeasureExtension(new RepeatExtension(calls: 3));

            // 每次测量高度 +10：返回 20 → 30 → 40，扩展采用最后一次结果。
            Assert.AreEqual(40f, _list.MeasureItem(0, item).y, 0.01f);
            Assert.AreEqual(3, item.MeasureCalls);
        }

        private sealed class FixedItem : IUIVirtualListItem
        {
            private float _height;
            private readonly float _growth;
            public int MeasureCalls { get; private set; }
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;

            public FixedItem(Transform parent, float height, float growth)
            {
                _height = height;
                _growth = growth;
                GameObject = new GameObject("Fixed Item", typeof(RectTransform));
                GameObject.transform.SetParent(parent, false);
            }

            public void Bind(int index, object data) { }
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }

            public Vector2 Measure()
            {
                MeasureCalls++;
                var size = new Vector2(300f, _height);
                _height += _growth;
                return size;
            }
        }

        private sealed class NullFactory : IUIVirtualListItemFactory
        {
            public int GetItemType(int index, object data) => 0;
            public IUIVirtualListItem Create(int itemType, Transform parent) =>
                throw new NotSupportedException("测量扩展测试不创建实例。");
            public void Destroy(IUIVirtualListItem item) { }
        }

        private sealed class ScaleExtension : IUIVirtualListMeasureExtension
        {
            private readonly float _scale;
            public ScaleExtension(float scale) => _scale = scale;

            public Vector2 Measure(in UIVirtualListMeasureContext context, Func<int, Vector2> next)
            {
                var size = next(context.Index);
                return new Vector2(size.x, size.y * _scale);
            }
        }

        private sealed class TraceExtension : IUIVirtualListMeasureExtension
        {
            private readonly string _name;
            private readonly List<string> _trace;
            public TraceExtension(string name, List<string> trace)
            {
                _name = name;
                _trace = trace;
            }

            public Vector2 Measure(in UIVirtualListMeasureContext context, Func<int, Vector2> next)
            {
                _trace.Add(_name);
                return next(context.Index);
            }
        }

        private sealed class RepeatExtension : IUIVirtualListMeasureExtension
        {
            private readonly int _calls;
            public RepeatExtension(int calls) => _calls = calls;

            public Vector2 Measure(in UIVirtualListMeasureContext context, Func<int, Vector2> next)
            {
                var size = next(context.Index);
                for (int i = 1; i < _calls; i++)
                    size = next(context.Index);
                return size;
            }
        }
    }
}
