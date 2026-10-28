using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualPagingComponentTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void StatusClicksAreConsumedWhenAutomaticAndBusinessIndicesStayCorrect(bool automatic)
        {
            using var f = new Fixture();
            Set(f.Component, "_pageStatusClick", automatic);
            int calls = 0;
            var initial = f.Component.SetPageLoader((page, size, token) =>
            { calls++; return Task.FromResult(UGUIVirtualPage.Create(new[] { "A", "B" }, false)); }, false);
            Assert.IsFalse(initial.GetAwaiter().GetResult());
            int clicks = 0;
            object clicked = null;
            f.Component.ItemClicked += (index, data) => { clicks++; clicked = data; };
            f.View(0).Click();
            Assert.AreEqual(automatic ? 1 : 0, calls);
            Assert.AreEqual(automatic ? 0 : 1, clicks);
            if (!automatic) Assert.IsTrue(f.Component.LoadNextPageAsync().GetAwaiter().GetResult());
            f.View(0).Click();
            Assert.AreEqual(automatic ? 1 : 2, clicks);
            Assert.AreEqual("A", ((UGUIVirtualPageItem)clicked).Data);
            Assert.AreEqual(2, f.Component.LoadedPageDataCount);
        }

        [Test]
        public void FacadeRejectsFlatWritesAndReleaseCancelsWithoutLateReinitialization()
        {
            using var f = new Fixture();
            var backend = new TaskCompletionSource<UGUIVirtualPage>();
            var load = f.Component.SetPageLoader((page, size, token) => backend.Task);
            Assert.IsTrue(f.Component.TryGetPagedAdapter(out var adapter));
            Assert.Throws<InvalidOperationException>(() => f.Component.AddData("bad"));
            Assert.Throws<InvalidOperationException>(() => f.Component.SetDatas(new[] { "bad" }));
            Assert.Throws<InvalidOperationException>(() => f.Component.Clear());
            f.Component.Release();
            Assert.IsFalse(load.GetAwaiter().GetResult());
            Assert.IsTrue(adapter.IsDisposed);
            Assert.IsFalse(f.Component.IsInitialized);
            Assert.IsFalse(f.Component.LoadNextPageAsync().GetAwaiter().GetResult());
            Assert.IsFalse(f.Component.RetryPageAsync().GetAwaiter().GetResult());
            Assert.IsFalse(f.Component.RefreshPagesAsync().GetAwaiter().GetResult());
            backend.SetResult(UGUIVirtualPage.Create(new[] { "late" }, false));
            Assert.IsFalse(f.Component.IsInitialized);
        }

        [Test]
        public void ReplacingLoaderCanBeInterruptedByReleaseInsideCancellationCallback()
        {
            using var f = new Fixture();
            var backend = new TaskCompletionSource<UGUIVirtualPage>();
            var first = f.Component.SetPageLoader((page, size, token) =>
            {
                token.Register(f.Component.Release);
                return backend.Task;
            });
            var replacement = f.Component.SetPageLoader((page, size, token) => Task.FromResult(UGUIVirtualPage.Create(new[] { "new" }, false)));
            Assert.IsFalse(replacement.GetAwaiter().GetResult());
            Assert.IsFalse(first.GetAwaiter().GetResult());
            Assert.IsFalse(f.Component.IsInitialized);
            Assert.IsFalse(f.Component.TryGetPagedAdapter(out _));
            backend.SetResult(UGUIVirtualPage.Create(new[] { "old" }, false));
            Assert.IsFalse(f.Component.IsInitialized);
        }

        [Test]
        public void DisabledAutomaticPreloadIgnoresItsHiddenThreshold()
        {
            using var f = new Fixture();
            Set(f.Component, "_pagePreloadItems", -100);
            int calls = 0;
            f.Component.SetPageLoader((page, size, token) =>
            { calls++; return Task.FromResult(UGUIVirtualPage.Create(new[] { "A" }, false)); }, false).GetAwaiter().GetResult();
            typeof(UGUIVirtualListComponent).GetMethod("TickPaging", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Component, null);
            Assert.AreEqual(0, calls);
        }

        private static void Set(object target, string field, object value) => typeof(UGUIVirtualListComponent)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualListComponent Component;
            public Fixture()
            {
                Root = new GameObject("Paging component test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                var scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                scroll.viewport = (RectTransform)viewport.transform;
                scroll.viewport.sizeDelta = new Vector2(300, 300);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                scroll.content = (RectTransform)content.transform;
                scroll.vertical = true;
                scroll.horizontal = false;
                Core = new UGUIVirtualList(scroll, new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 50)), this);
                Component = Root.AddComponent<UGUIVirtualListComponent>();
                Set(Component, "_listType", UGUIVirtualListType.Paged);
                Set(Component, "_listTypeConfigured", true);
                Set(Component, "_autoLoadPages", false);
                Set(Component, "_list", Core);
                var forward = typeof(UGUIVirtualListComponent).GetMethod("ForwardClick", BindingFlags.Instance | BindingFlags.NonPublic);
                Core.ItemClicked += (Action<int, object>)Delegate.CreateDelegate(typeof(Action<int, object>), Component, forward);
            }
            public View View(int index) { Assert.IsTrue(Core.TryGetItem(index, out var row)); return (View)row; }
            public int GetItemType(int index, object data) => ((UGUIVirtualPageItem)data).IsStatus ? 1 : 0;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Paged component row", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose() { Component.Release(); Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            private Action _click;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go) { GameObject = go; }
            public void Bind(int index, object data) { }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { _click = callback; }
            public void Click() => _click?.Invoke();
        }
    }
}
