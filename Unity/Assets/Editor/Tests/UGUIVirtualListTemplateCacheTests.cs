using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualListTemplateCacheTests
    {
        [Test]
        public void LoadsTemplatesAndAppliesThemToComponent()
        {
            using var f = new Fixture();
            var cache = new UGUIVirtualListTemplateCache(f.Load, f.Release);
            try
            {
                Assert.IsTrue(cache.LoadAsync(f.Component, new Dictionary<int, string>
                {
                    { 0, "Assets/A.prefab" },
                    { 1, "Assets/B.prefab" }
                }).GetAwaiter().GetResult());
                Assert.AreEqual(2, cache.LoadedCount);
                Assert.AreEqual(2, f.LoadCalls);
                Assert.AreEqual(2, f.RuntimeTemplateCount(f.Component));
                Assert.IsTrue(cache.IsLoaded("Assets/A.prefab"));
                Assert.AreEqual(2, f.RuntimeTemplateCount(f.Component));
            }
            finally { cache.Dispose(); }
        }

        [Test]
        public void ConcurrentRequestsForSameLocationShareOneLoad()
        {
            using var f = new Fixture(delay: true);
            var cache = new UGUIVirtualListTemplateCache(f.Load, f.Release);
            try
            {
                var first = cache.LoadAsync(f.Component, new Dictionary<int, string> { { 0, "Assets/A.prefab" } });
                var second = cache.LoadAsync(f.Component, new Dictionary<int, string> { { 1, "Assets/A.prefab" } });
                f.CompleteAll();
                Assert.IsTrue(first.GetAwaiter().GetResult());
                Assert.IsTrue(second.GetAwaiter().GetResult());
                Assert.AreEqual(1, f.LoadCalls, "同一地址并发只需加载一次");
                Assert.AreEqual(1, cache.LoadedCount);
                Assert.AreEqual(2, f.RuntimeTemplateCount(f.Component), "两个类型可共用同一模板资源");
            }
            finally { cache.Dispose(); }
        }

        [Test]
        public void PartialFailureKeepsSuccessesAndCanRetryOnlyFailedLocations()
        {
            using var f = new Fixture(fail: "Assets/B.prefab");
            var cache = new UGUIVirtualListTemplateCache(f.Load, f.Release);
            try
            {
                var locations = new Dictionary<int, string> { { 0, "Assets/A.prefab" }, { 1, "Assets/B.prefab" } };
                Assert.IsFalse(cache.LoadAsync(f.Component, locations).GetAwaiter().GetResult());
                Assert.IsNotNull(cache.LastError);
                Assert.AreEqual(1, cache.LoadedCount);
                Assert.IsTrue(cache.IsLoaded("Assets/A.prefab"));
                Assert.IsFalse(cache.IsLoaded("Assets/B.prefab"));
                f.Fail.clear = true;
                Assert.IsTrue(cache.LoadAsync(f.Component, locations).GetAwaiter().GetResult());
                Assert.AreEqual(2, cache.LoadedCount);
                Assert.AreEqual(1, f.CallsFor("Assets/A.prefab"), "已成功的地址不应重复加载");
                Assert.AreEqual(2, f.CallsFor("Assets/B.prefab"), "失败地址应重试一次");
            }
            finally { cache.Dispose(); }
        }

        [Test]
        public void CancellationAndDisposeReleaseEverythingOnce()
        {
            using var f = new Fixture(delay: true);
            var cache = new UGUIVirtualListTemplateCache(f.Load, f.Release);
            var cts = new CancellationTokenSource();
            var task = cache.LoadAsync(f.Component, new Dictionary<int, string> { { 0, "Assets/A.prefab" } }, cts.Token);
            cts.Cancel();
            f.CompleteAll();
            Assert.IsFalse(task.GetAwaiter().GetResult());
            Assert.AreEqual(0, cache.LoadedCount);
            cache.Dispose();
            Assert.AreEqual(0, f.ReleaseCalls);
            Assert.IsFalse(cache.LoadAsync(f.Component, new Dictionary<int, string> { { 0, "Assets/A.prefab" } })
                .GetAwaiter().GetResult());
        }

        [Test]
        public void DisposingAfterLoadReleasesEachLocationExactlyOnce()
        {
            using var f = new Fixture();
            var cache = new UGUIVirtualListTemplateCache(f.Load, f.Release);
            cache.LoadAsync(f.Component, new Dictionary<int, string> { { 0, "Assets/A.prefab" }, { 1, "Assets/B.prefab" } })
                .GetAwaiter().GetResult();
            cache.Dispose();
            Assert.AreEqual(2, f.ReleaseCalls);
            cache.Dispose();
            Assert.AreEqual(2, f.ReleaseCalls, "重复 Dispose 不应重复释放");
        }

        [Test]
        public void EmptyLocationFailsWithoutCallingLoader()
        {
            using var f = new Fixture();
            var cache = new UGUIVirtualListTemplateCache(f.Load, f.Release);
            try
            {
                Assert.IsFalse(cache.LoadAsync(f.Component, new Dictionary<int, string> { { 0, "" } }).GetAwaiter().GetResult());
                Assert.AreEqual(0, f.LoadCalls);
            }
            finally { cache.Dispose(); }
        }

        [Test]
        public void RuntimeTemplateMustBeSetBeforeInitializeAndSurvivesRelease()
        {
            using var f = new Fixture();
            f.Component.SetRuntimeTemplate(3, f.MakePrefab("Runtime"));
            Assert.Throws<ArgumentNullException>(() => f.Component.SetRuntimeTemplate(4, null));
            Assert.Throws<ArgumentException>(() => f.Component.SetRuntimeTemplate(4, new GameObject("NoRect")));
            f.Component.ClearRuntimeTemplates();
            Assert.AreEqual(0, f.RuntimeTemplateCount(f.Component));
        }

        private sealed class FailureBag { public bool clear; }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualListComponent Component;
            public readonly FailureBag Fail = new FailureBag();
            public int LoadCalls;
            public int ReleaseCalls;
            private readonly Dictionary<string, int> _calls = new Dictionary<string, int>();
            public int CallsFor(string location) => _calls.TryGetValue(location, out var n) ? n : 0;
            private readonly string _fail;
            private readonly bool _delay;
            private readonly List<TaskCompletionSource<GameObject>> _pending = new List<TaskCompletionSource<GameObject>>();

            public Fixture(string fail = null, bool delay = false)
            {
                _fail = fail;
                _delay = delay;
                Root = new GameObject("Template cache test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                var scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                scroll.viewport = (RectTransform)viewport.transform;
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                scroll.content = (RectTransform)content.transform;
                Component = Root.AddComponent<UGUIVirtualListComponent>();
                Set(Component, "_scrollRect", scroll);
                Set(Component, "_itemSizeMode", UGUIVirtualListItemSizeMode.Manual);
                Set(Component, "_itemSize", new Vector2(100, 60));
                Set(Component, "_listType", UGUIVirtualListType.Flat);
                Set(Component, "_listTypeConfigured", true);
                var array = new UGUIVirtualListComponent.ItemTemplate[1];
                array[0] = new UGUIVirtualListComponent.ItemTemplate { TypeId = 99, Template = MakePrefab("Serialized") };
                Set(Component, "_itemTemplates", array);
            }

            public GameObject MakePrefab(string name)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(null, false);
                go.SetActive(false);
                return go;
            }

            public Task<GameObject> Load(string location, CancellationToken token)
            {
                LoadCalls++;
                _calls[location] = (_calls.TryGetValue(location, out var n0) ? n0 : 0) + 1;
                if (_delay)
                {
                    var tcs = new TaskCompletionSource<GameObject>();
                    _pending.Add(tcs);
                    return tcs.Task;
                }
                if (location == _fail && !Fail.clear)
                    return Task.FromException<GameObject>(new InvalidOperationException("模拟加载失败"));
                return Task.FromResult(MakePrefab(location));
            }

            public void CompleteAll()
            {
                foreach (var tcs in _pending) tcs.TrySetResult(MakePrefab("Loaded"));
                _pending.Clear();
            }

            public void Release(string location) => ReleaseCalls++;

            public int RuntimeTemplateCount(UGUIVirtualListComponent component) =>
                ((System.Collections.IDictionary)typeof(UGUIVirtualListComponent)
                    .GetField("_runtimeTemplates", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(component)).Count;

            private static void Set(object target, string name, object value) => typeof(UGUIVirtualListComponent)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

            public void Dispose() { UnityEngine.Object.DestroyImmediate(Root); }
        }
    }
}
