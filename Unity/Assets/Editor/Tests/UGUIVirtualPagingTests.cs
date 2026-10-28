using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualPagingTests
    {
        [Test]
        public void FirstPageStartsAtTopAndBusinessRowsKeepBindingWhenNextPageAppends()
        {
            using var f = new Fixture((page, size, token) => Task.FromResult(Page(page * 20, 20, page == 0)));
            Assert.IsTrue(Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual(20, f.Pager.LoadedCount);
            Assert.AreEqual(21, f.Core.Count);
            Assert.AreEqual(0, f.Position, 0.01f, "不能把旧尾项当锚点而直接跳到第一页末尾");
            f.Core.TryGetItemToken(0, out var token);
            Assert.IsTrue(Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual(40, f.Pager.LoadedCount);
            Assert.IsTrue(token.IsCurrent);
            Assert.AreEqual(UGUIVirtualPageState.Completed, f.Pager.State);
            Assert.AreEqual(2, f.Pager.NextPageIndex);
            Assert.IsFalse(Done(f.Pager.LoadNextPageAsync()));
        }

        [Test]
        public void ConcurrentRequestsShareTaskAndOnlyCallLoaderOnce()
        {
            var backend = new TaskCompletionSource<UGUIVirtualPage>();
            int calls = 0;
            using var f = new Fixture((page, size, token) => { calls++; return backend.Task; });
            var first = f.Pager.LoadNextPageAsync();
            var second = f.Pager.LoadNextPageAsync();
            Assert.AreSame(first, second);
            Assert.AreEqual(1, calls);
            Assert.IsFalse(first.IsCompleted);
            Assert.AreEqual(UGUIVirtualPageItemKind.Loading, f.Pager.Footer.Kind);
            backend.SetResult(Page(0, 2, false));
            Assert.IsTrue(Done(first));
            Assert.AreEqual(2, f.Pager.LoadedCount);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NetworkFailureDoesNotAutoRetryAndExplicitRetryUsesSamePage(bool synchronous)
        {
            var pages = new List<int>();
            int calls = 0;
            using var f = new Fixture((page, size, token) =>
            {
                pages.Add(page);
                if (calls++ == 0)
                {
                    if (synchronous) throw new InvalidOperationException("模拟网络错误");
                    return Task.FromException<UGUIVirtualPage>(new InvalidOperationException("模拟网络错误"));
                }
                return Task.FromResult(Page(0, 2, false));
            });
            Assert.IsFalse(Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual(UGUIVirtualPageState.Error, f.Pager.State);
            Assert.AreEqual(UGUIVirtualPageItemKind.Error, f.Pager.Footer.Kind);
            Assert.IsNotNull(f.Pager.LastError);
            Assert.IsNull(f.Pager.Tick());
            Assert.IsFalse(Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual(1, calls);
            Assert.IsTrue(Done(f.Pager.RetryAsync()));
            CollectionAssert.AreEqual(new[] { 0, 0 }, pages);
            Assert.AreEqual(2, f.Pager.LoadedCount);
        }

        [Test]
        public void RefreshCancelsUncooperativeOldRequestAndItsLateResultCannotAppend()
        {
            var old = new TaskCompletionSource<UGUIVirtualPage>();
            CancellationToken oldToken = default;
            int calls = 0;
            using var f = new Fixture((page, size, token) =>
            {
                Assert.AreEqual(0, page);
                if (calls++ == 0) { oldToken = token; return old.Task; }
                return Task.FromResult(UGUIVirtualPage.Create(new[] { "new" }, false));
            });
            var previous = f.Pager.LoadNextPageAsync();
            Assert.IsTrue(Done(f.Pager.RefreshAsync()));
            Assert.IsTrue(oldToken.IsCancellationRequested);
            Assert.IsFalse(Done(previous));
            old.SetResult(UGUIVirtualPage.Create(new[] { "old" }, true));
            Assert.AreEqual(1, f.Pager.LoadedCount);
            Assert.AreEqual("new", ((UGUIVirtualPageItem)f.Core.GetData(0)).Data);
            Assert.AreEqual(UGUIVirtualPageState.Completed, f.Pager.State);
        }

        [Test]
        public void DisposeCompletesPendingWaitersAndIgnoresLateResultsWithoutDisposingCore()
        {
            var backend = new TaskCompletionSource<UGUIVirtualPage>();
            CancellationToken captured = default;
            using var f = new Fixture((page, size, token) => { captured = token; return backend.Task; });
            var request = f.Pager.LoadNextPageAsync();
            f.Pager.Dispose();
            Assert.IsFalse(Done(request));
            Assert.IsTrue(captured.IsCancellationRequested);
            backend.SetResult(Page(0, 10, true));
            Assert.AreEqual(0, f.Pager.LoadedCount);
            Assert.AreEqual(1, f.Core.Count);
            Assert.IsFalse(f.Core.IsDisposalComplete);
            Assert.IsFalse(Done(f.Pager.LoadNextPageAsync()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptyFinalPageShowsEndWhileEmptyHasMoreIsRejected(bool hasMore)
        {
            using var f = new Fixture((page, size, token) => Task.FromResult(Page(0, 0, hasMore)));
            Assert.AreEqual(!hasMore, Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual(hasMore ? UGUIVirtualPageState.Error : UGUIVirtualPageState.Completed, f.Pager.State);
            Assert.AreEqual(hasMore ? 0 : 1, f.Pager.NextPageIndex);
            Assert.AreEqual(1, f.Core.Count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NullTaskOrNullPageBecomesRetryableFailure(bool nullTask)
        {
            using var f = new Fixture((page, size, token) => nullTask ? null : Task.FromResult<UGUIVirtualPage>(null));
            Assert.IsFalse(Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual(UGUIVirtualPageState.Error, f.Pager.State);
            Assert.AreEqual(0, f.Pager.NextPageIndex);
        }

        [Test]
        public void AutoLoadingUsesActualViewportNotBufferAndDoesNotJumpToNewEnd()
        {
            int calls = 0;
            using var f = new Fixture((page, size, token) => { calls++; return Task.FromResult(Page(page * 20, 20, page == 0)); });
            Assert.IsTrue(Done(f.Pager.LoadNextPageAsync()));
            Assert.IsNull(f.Pager.Tick(true, 5));
            Assert.AreEqual(1, calls);
            f.Core.ScrollToView(20, UGUIVirtualListAlignment.End);
            float before = f.Position;
            var next = f.Pager.Tick(true, 5);
            Assert.IsNotNull(next);
            Assert.IsTrue(Done(next));
            Assert.AreEqual(2, calls);
            Assert.AreEqual(before, f.Position, 0.01f);
        }

        [Test]
        public void ShortPagesCanFillViewportButOnlyStartOneRequestPerTick()
        {
            int calls = 0;
            using var f = new Fixture((page, size, token) => { calls++; return Task.FromResult(Page(page, 1, true)); });
            for (int i = 0; i < 20; i++)
            {
                int before = calls;
                var task = f.Pager.Tick(true, 0);
                Assert.LessOrEqual(calls - before, 1);
                if (task == null) break;
                Assert.IsTrue(Done(task));
            }
            Assert.Greater(calls, 1);
            Assert.Less(calls, 20);
        }

        [Test]
        public void ResultCompletedDuringBindingWaitsForNextSafeTick()
        {
            var backend = new TaskCompletionSource<UGUIVirtualPage>();
            using var f = new Fixture((page, size, token) => backend.Task);
            var request = f.Pager.LoadNextPageAsync();
            f.BindCallback = row => backend.TrySetResult(Page(0, 2, false));
            f.Core.Refresh();
            Assert.IsFalse(request.IsCompleted);
            f.BindCallback = null;
            Assert.IsNull(f.Pager.Tick(false));
            Assert.IsTrue(Done(request));
            Assert.AreEqual(2, f.Pager.LoadedCount);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CommittedPageIsNotRequestedAgainAfterRendererFailure(bool hasMore)
        {
            int calls = 0;
            using var f = new Fixture((page, size, token) => { calls++; return Task.FromResult(Page(0, 2, hasMore)); });
            f.BindCallback = row => { if (!row.IsStatus) throw new InvalidOperationException("预期的分页渲染错误"); };
            Assert.Throws<InvalidOperationException>(() => Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual(2, f.Pager.LoadedCount);
            Assert.AreEqual(1, f.Pager.NextPageIndex);
            Assert.AreEqual(3, f.Core.Count);
            Assert.IsFalse(Done(f.Pager.RetryAsync()));
            Assert.IsNull(f.Pager.Tick(), "视图错误修复前不能自动继续请求下一页");
            f.BindCallback = null;
            Assert.IsTrue(f.Pager.RefreshPresentation());
            Assert.AreEqual(1, calls);
            Assert.IsNull(f.Pager.LastPresentationError);
            Assert.IsTrue(f.Pager.OwnsCurrentData);
        }

        [Test]
        public void LoadingFooterFailureDoesNotCallNetworkUntilRendererIsFixed()
        {
            int calls = 0;
            using var f = new Fixture((page, size, token) => { calls++; return Task.FromResult(Page(0, 1, false)); });
            f.BindCallback = row => { if (row.Kind == UGUIVirtualPageItemKind.Loading) throw new InvalidOperationException("预期的状态项绑定错误"); };
            Assert.Throws<InvalidOperationException>(() => Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual(0, calls);
            Assert.AreEqual(UGUIVirtualPageState.Error, f.Pager.State);
            f.BindCallback = null;
            Assert.IsTrue(Done(f.Pager.RetryAsync()));
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void FailedRefreshBeforeCommitPreservesOldProjectionAndPageCounter()
        {
            using var f = new Fixture((page, size, token) => Task.FromResult(Page(0, 2, true)));
            Done(f.Pager.LoadNextPageAsync());
            f.RecycleCallback = () => throw new InvalidOperationException("预期的分页刷新回收错误");
            Assert.Throws<InvalidOperationException>(() => f.Pager.RefreshAsync());
            Assert.AreEqual(2, f.Pager.LoadedCount);
            Assert.AreEqual(3, f.Core.Count);
            Assert.AreEqual(1, f.Pager.NextPageIndex);
            Assert.IsTrue(f.Pager.OwnsCurrentData);
            f.RecycleCallback = null;
            f.Pager.RefreshPresentation();
        }

        [Test]
        public void DirectCoreMutationStopsAutoLoadingUntilExplicitRefreshTakesOver()
        {
            using var f = new Fixture((page, size, token) => Task.FromResult(Page(0, 1, false)));
            f.Core.AddData(f.Pager.Footer);
            Assert.IsFalse(f.Pager.OwnsCurrentData);
            Assert.IsNull(f.Pager.Tick());
            Assert.AreEqual(UGUIVirtualPageState.Error, f.Pager.State);
            Assert.IsTrue(Done(f.Pager.RefreshAsync()));
            Assert.IsTrue(f.Pager.OwnsCurrentData);
        }

        [Test]
        public void RefreshDoesNotOverwriteDisposedStateWhenCancellationCallbackDisposesAdapter()
        {
            var backend = new TaskCompletionSource<UGUIVirtualPage>();
            UGUIVirtualPagedAdapter pager = null;
            using var f = new Fixture((page, size, token) =>
            {
                token.Register(() => pager.Dispose());
                return backend.Task;
            });
            pager = f.Pager;
            var footer = f.Core.GetData(0);
            var first = pager.LoadNextPageAsync();
            Assert.IsFalse(Done(pager.RefreshAsync()));
            Assert.IsFalse(Done(first));
            Assert.IsTrue(pager.IsDisposed);
            Assert.AreEqual(UGUIVirtualPageState.Disposed, pager.State);
            Assert.AreSame(footer, f.Core.GetData(0), "释放适配器本身不能被 Refresh 继续改写投影");
            backend.SetResult(Page(0, 1, false));
        }

        [Test]
        public void CancellationMutationDoesNotMakeFailedRefreshClaimAlienProjection()
        {
            var backend = new TaskCompletionSource<UGUIVirtualPage>();
            Fixture f = null;
            int calls = 0;
            using (f = new Fixture((page, size, token) =>
            {
                int call = calls++;
                if (call == 0) return Task.FromResult(Page(0, 2, true));
                if (call == 1)
                {
                    token.Register(() =>
                    {
                        f.Core.AddData(f.Pager.Footer);
                        f.RecycleCallback = () => throw new InvalidOperationException("取消后模拟回收失败");
                    });
                    return backend.Task;
                }
                return Task.FromResult(Page(0, 1, false));
            }))
            {
                Assert.IsTrue(Done(f.Pager.LoadNextPageAsync()));
                var waiting = f.Pager.LoadNextPageAsync();
                Assert.Throws<InvalidOperationException>(() => f.Pager.RefreshAsync());
                Assert.IsFalse(Done(waiting));
                Assert.IsFalse(f.Pager.OwnsCurrentData);
                Assert.AreEqual(2, f.Pager.LoadedCount);
                Assert.AreEqual(1, f.Pager.NextPageIndex);
                Assert.AreEqual(4, f.Core.Count);
                f.RecycleCallback = null;
                Assert.IsTrue(Done(f.Pager.RefreshAsync()));
                Assert.AreEqual(1, f.Pager.LoadedCount);
                Assert.IsTrue(f.Pager.OwnsCurrentData);
                backend.SetResult(Page(100, 1, true));
                Assert.AreEqual(1, f.Pager.LoadedCount);
            }
        }

        [Test]
        public void ThrowingCancellationCallbackIsRecordedWithoutBreakingDispose()
        {
            var backend = new TaskCompletionSource<UGUIVirtualPage>();
            using var f = new Fixture((page, size, token) =>
            {
                token.Register(() => throw new InvalidOperationException("预期的取消回调错误"));
                return backend.Task;
            });
            var waiting = f.Pager.LoadNextPageAsync();
            Assert.DoesNotThrow(f.Pager.Dispose);
            Assert.IsFalse(Done(waiting));
            Assert.IsNotNull(f.Pager.LastCancellationError);
            backend.SetResult(Page(0, 1, false));
            Assert.AreEqual(0, f.Pager.LoadedCount);
        }

        [Test]
        public void BackgroundPublicCallsAreRejectedBeforeTouchingUnity()
        {
            using var f = new Fixture((page, size, token) => Task.FromResult(Page(0, 1, false)));
            Assert.Throws<InvalidOperationException>(() => Task.Run(() => f.Pager.LoadNextPageAsync()).GetAwaiter().GetResult());
            Assert.AreEqual(UGUIVirtualPageState.Idle, f.Pager.State);
        }

        [Test]
        public void MissingSynchronizationContextRejectsPendingTaskSafely()
        {
            var backend = new TaskCompletionSource<UGUIVirtualPage>();
            using var f = new Fixture((page, size, token) => backend.Task);
            var old = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);
                Assert.IsFalse(Done(f.Pager.LoadNextPageAsync()));
                Assert.AreEqual(UGUIVirtualPageState.Error, f.Pager.State);
            }
            finally { SynchronizationContext.SetSynchronizationContext(old); backend.TrySetResult(Page(0, 1, false)); }
        }

        private static UGUIVirtualPage Page(int first, int count, bool more)
        {
            var data = new List<string>();
            for (int i = 0; i < count; i++) data.Add("Data " + (first + i));
            return UGUIVirtualPage.Create(data, more);
        }
        private static bool Done(Task<bool> task)
        {
            Assert.IsTrue(task.IsCompleted, "本用例使用同步/TCS主线程完成，禁止阻塞等待 Unity 上下文");
            return task.GetAwaiter().GetResult();
        }

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly ScrollRect Scroll;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualPagedAdapter Pager;
            public Action<UGUIVirtualPageItem> BindCallback;
            public Action RecycleCallback;
            public float Position => Scroll.content.anchoredPosition.y;
            public Fixture(Func<int, int, CancellationToken, Task<UGUIVirtualPage>> loader)
            {
                Root = new GameObject("Paging test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                Scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                Scroll.viewport = (RectTransform)viewport.transform;
                Scroll.viewport.sizeDelta = new Vector2(300, 300);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                Scroll.content = (RectTransform)content.transform;
                Scroll.vertical = true;
                Scroll.horizontal = false;
                Scroll.movementType = ScrollRect.MovementType.Clamped;
                Core = new UGUIVirtualList(Scroll, new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 50)), this) { BufferSize = 100 };
                Pager = new UGUIVirtualPagedAdapter(Core, loader);
                Pager.Initialize();
            }
            public int GetItemType(int index, object data) => ((UGUIVirtualPageItem)data).IsStatus ? 1 : 0;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Page row", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go, this);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose() { BindCallback = null; RecycleCallback = null; Pager.Dispose(); Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            private readonly Fixture _owner;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go, Fixture owner) { GameObject = go; _owner = owner; }
            public void Bind(int index, object data) => _owner.BindCallback?.Invoke((UGUIVirtualPageItem)data);
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() => _owner.RecycleCallback?.Invoke();
            public void SetClickHandler(Action callback) { }
        }
    }
}
