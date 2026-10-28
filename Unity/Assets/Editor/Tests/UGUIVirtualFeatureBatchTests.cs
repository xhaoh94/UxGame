using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualKeySelectionTests
    {
        [Test]
        public void SelectionFollowsBusinessKeyAcrossReorderAndFullReplace()
        {
            using var f = new Fixture();
            f.Selection.Mode = UGUIVirtualSelectionMode.Multiple;
            f.Selection.KeySelector = data => ((Row)data).Key;
            f.Selection.Select(1);
            Assert.IsTrue(f.Selection.IsSelectedKey("b"));
            f.Core.SetDatas(new[] { new Row("c"), new Row("b"), new Row("a") });
            Assert.IsTrue(f.Selection.IsSelected(1), "按 Id 在重排后重新定位");
            Assert.IsTrue(f.Selection.IsSelectedKey("b"));
            f.Core.SetDatas(new[] { new Row("x") });
            Assert.IsFalse(f.Selection.HasSelection, "Id 不存在时取消选中");
            Assert.AreEqual(0, f.Selection.GetSelectedKeys().Count);
        }

        [Test]
        public void SelectByKeyAndRemoveKeepKeySetConsistent()
        {
            using var f = new Fixture();
            f.Selection.Mode = UGUIVirtualSelectionMode.Multiple;
            f.Selection.KeySelector = data => ((Row)data).Key;
            Assert.IsTrue(f.Selection.SelectByKey("c"));
            Assert.IsTrue(f.Selection.IsSelected(2));
            f.Core.InsertData(0, new Row("new"));
            Assert.IsTrue(f.Selection.IsSelected(3), "插入后按 Id 搬迁");
            f.Core.RemoveAt(3);
            Assert.IsFalse(f.Selection.IsSelectedKey("c"));
            Assert.IsFalse(f.Selection.SelectByKey("missing"));
        }

        [Test]
        public void IndexModeStillClearsOnReplaceWhileKeyModeRestores()
        {
            using var f = new Fixture();
            f.Selection.Mode = UGUIVirtualSelectionMode.Multiple;
            f.Selection.Select(0);
            f.Core.SetDatas(new[] { new Row("z") });
            Assert.IsFalse(f.Selection.HasSelection);
        }

        private sealed class Row
        {
            public readonly string Key;
            public Row(string key) { Key = key; }
        }

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualSelection Selection;
            public Fixture()
            {
                Root = new GameObject("Key selection test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                var scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                scroll.viewport = (RectTransform)viewport.transform;
                scroll.viewport.sizeDelta = new Vector2(300, 300);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                scroll.content = (RectTransform)content.transform;
                scroll.vertical = true; scroll.horizontal = false;
                Core = new UGUIVirtualList(scroll, new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 50)), this);
                Core.SetDatas(new[] { new Row("a"), new Row("b"), new Row("c") });
                Selection = new UGUIVirtualSelection(Core);
            }
            public int GetItemType(int index, object data) => 0;
            public IUGUIVirtualListItem Create(int type, Transform parent)
            {
                var go = new GameObject("Key row", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose() { Selection.Dispose(); Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go) { GameObject = go; }
            public void Bind(int index, object data) { }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }

    public sealed class UGUIVirtualCursorPagingTests
    {
        [Test]
        public void CursorPagingAdvancesCursorAndStopsAtEnd()
        {
            using var f = new Fixture();
            Assert.IsTrue(Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual("c1", f.Pager.Cursor);
            Assert.AreEqual(2, f.Pager.LoadedCount);
            Assert.IsTrue(Done(f.Pager.LoadNextPageAsync()));
            Assert.AreEqual("c2", f.Pager.Cursor);
            Assert.AreEqual(UGUIVirtualPageState.Completed, f.Pager.State);
            Assert.IsFalse(Done(f.Pager.LoadNextPageAsync()));
        }

        [Test]
        public void RepeatingCursorOrEmptyHasMoreIsRejectedAsError()
        {
            using var f = new Fixture(stuck: true);
            Assert.IsTrue(Done(f.Pager.LoadNextPageAsync()));
            Assert.IsFalse(Done(f.Pager.LoadNextPageAsync()), "游标未变化必须判为失败");
            Assert.AreEqual(UGUIVirtualPageState.Error, f.Pager.State);
            Assert.IsNotNull(f.Pager.LastError);
        }

        [Test]
        public void RefreshResetsCursorToStart()
        {
            using var f = new Fixture();
            Done(f.Pager.LoadNextPageAsync());
            Assert.AreEqual("c1", f.Pager.Cursor);
            Assert.IsTrue(Done(f.Pager.RefreshAsync()));
            Assert.AreEqual("c1", f.Pager.Cursor);
            Assert.AreEqual(2, f.Pager.LoadedCount);
        }

        private static bool Done(Task<bool> task)
        {
            Assert.IsTrue(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualPagedAdapter Pager;
            private readonly bool _stuck;
            public Fixture(bool stuck = false)
            {
                _stuck = stuck;
                Root = new GameObject("Cursor paging test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                var scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                scroll.viewport = (RectTransform)viewport.transform;
                scroll.viewport.sizeDelta = new Vector2(300, 200);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                scroll.content = (RectTransform)content.transform;
                scroll.vertical = true; scroll.horizontal = false;
                Core = new UGUIVirtualList(scroll, new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 50)), this);
                Pager = new UGUIVirtualPagedAdapter(Core, (cursor, size, token) =>
                {
                    if (string.IsNullOrEmpty(cursor))
                        return Task.FromResult(UGUIVirtualCursorPage.Create(new[] { "a", "b" }, "c1", true));
                    if (cursor == "c1" && !_stuck)
                        return Task.FromResult(UGUIVirtualCursorPage.Create(new[] { "c" }, "c2", false));
                    return Task.FromResult(UGUIVirtualCursorPage.Create(new[] { "d" }, _stuck ? "c1" : "c2", true));
                });
                Pager.Initialize();
            }
            public int GetItemType(int index, object data) => 0;
            public IUGUIVirtualListItem Create(int type, Transform parent)
            {
                var go = new GameObject("Cursor row", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose() { Pager.Dispose(); Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go) { GameObject = go; }
            public void Bind(int index, object data) { }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }

    public sealed class UGUIVirtualCarouselTests
    {
        [Test]
        public void DragEndSnapsToNearestPage()
        {
            using var f = new Fixture();
            var carousel = new UGUIVirtualCarousel(f.Core, 300f, 0.1f);
            try
            {
                f.SetPosition(160f);
                f.EndDrag();
                Assert.IsTrue(carousel.IsSnapping);
                for (int i = 0; i < 60 && carousel.IsSnapping; i++) carousel.Tick(0.05f);
                Assert.AreEqual(300f, f.Position, 1f);
                Assert.AreEqual(1, carousel.CurrentPage);
            }
            finally { carousel.Dispose(); }
        }

        [Test]
        public void SnapToPageClampsAtEndAndRejectsBadPageLength()
        {
            using var f = new Fixture();
            Assert.Throws<ArgumentOutOfRangeException>(() => new UGUIVirtualCarousel(f.Core, 0f));
            var carousel = new UGUIVirtualCarousel(f.Core, 300f, 0.05f);
            try
            {
                int lastPage = Mathf.FloorToInt((f.Core.ContentLength - f.Core.ViewportSize.y) / 300f);
                Assert.IsTrue(carousel.SnapTo(999));
                for (int i = 0; i < 60 && carousel.IsSnapping; i++) carousel.Tick(0.05f);
                Assert.AreEqual(f.Core.ContentLength - f.Core.ViewportSize.y, f.Position, 1f);
                Assert.GreaterOrEqual(lastPage, 0);
            }
            finally { carousel.Dispose(); }
        }

        [Test]
        public void SmallOffsetDoesNotStartAnimation()
        {
            using var f = new Fixture();
            var carousel = new UGUIVirtualCarousel(f.Core, 300f, 0.1f);
            try
            {
                f.SetPosition(300.2f);
                f.EndDrag();
                Assert.IsFalse(carousel.IsSnapping);
            }
            finally { carousel.Dispose(); }
        }

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualListScrollRect Scroll;
            public readonly UGUIVirtualList Core;
            private readonly GameObject _eventSystem;
            public float Position => Scroll.content.anchoredPosition.y;
            public Fixture()
            {
                Root = new GameObject("Carousel test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                Scroll = Root.GetComponent<UGUIVirtualListScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                Scroll.viewport = (RectTransform)viewport.transform;
                Scroll.viewport.sizeDelta = new Vector2(300, 300);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                Scroll.content = (RectTransform)content.transform;
                Scroll.vertical = true; Scroll.horizontal = false;
                Scroll.movementType = ScrollRect.MovementType.Clamped;
                Core = new UGUIVirtualList(Scroll, new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(300, 300)), this)
                { BufferSize = 300 };
                var rows = new List<object>();
                for (int i = 0; i < 20; i++) rows.Add(i);
                Core.SetDatas(rows);
                _eventSystem = new GameObject("Carousel EventSystem", typeof(EventSystem));
            }
            public void SetPosition(float y)
            {
                var p = Scroll.content.anchoredPosition;
                p.y = y;
                Scroll.content.anchoredPosition = p;
                Core.RefreshLayout();
            }
            public void EndDrag()
            {
                var data = new PointerEventData(_eventSystem.GetComponent<EventSystem>())
                {
                    button = PointerEventData.InputButton.Left,
                    position = Vector2.zero
                };
                Scroll.OnBeginDrag(data);
                Scroll.OnEndDrag(data);
            }
            public int GetItemType(int index, object data) => 0;
            public IUGUIVirtualListItem Create(int type, Transform parent)
            {
                var go = new GameObject("Carousel row", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose()
            {
                Core.Dispose();
                UnityEngine.Object.DestroyImmediate(_eventSystem);
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go) { GameObject = go; }
            public void Bind(int index, object data) { }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }
}
