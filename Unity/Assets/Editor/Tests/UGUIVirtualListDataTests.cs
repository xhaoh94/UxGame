using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualListDataTests
    {
        [TestCase(UGUIVirtualListAxis.Vertical, false, false)]
        [TestCase(UGUIVirtualListAxis.Horizontal, false, false)]
        [TestCase(UGUIVirtualListAxis.Vertical, true, false)]
        [TestCase(UGUIVirtualListAxis.Horizontal, true, false)]
        [TestCase(UGUIVirtualListAxis.Vertical, false, true)]
        [TestCase(UGUIVirtualListAxis.Horizontal, false, true)]
        [TestCase(UGUIVirtualListAxis.Vertical, true, true)]
        [TestCase(UGUIVirtualListAxis.Horizontal, true, true)]
        public void InsertAndRemoveBeforeViewportKeepLogicalAnchorAndReuseItsView(UGUIVirtualListAxis axis, bool variable, bool multiple)
        {
            using var f = new Fixture(axis, variable, multiple);
            f.ShowMiddle();
            var anchor = f.View(60);
            var data = f.List.GetData(60);
            float before = f.Visual(60);
            f.List.TryGetItemToken(60, out var token);
            f.List.InsertDatas(0, new[] { new Row(1000), new Row(1001) });
            Assert.AreEqual(102, f.List.Count);
            Assert.AreSame(data, f.List.GetData(62));
            Assert.AreSame(anchor, f.View(62));
            Assert.AreEqual(before, f.Visual(62), 0.01f);
            Assert.AreEqual(62, anchor.Index);
            Assert.IsFalse(token.IsCurrent);
            int clicked = -1;
            object clickedData = null;
            f.List.ItemClicked += (index, value) => { clicked = index; clickedData = value; };
            anchor.Click();
            Assert.AreEqual(62, clicked);
            Assert.AreSame(data, clickedData);
            // 多列重新分组后，旧锚点前面可能多出同一行的项；下一次操作以当前首个可见项为锚点。
            int removalAnchor = f.List.FirstVisibleIndex;
            object removalData = f.List.GetData(removalAnchor);
            float removalVisual = f.Visual(removalAnchor);
            f.List.RemoveRange(0, 2);
            Assert.AreEqual(100, f.List.Count);
            Assert.AreSame(data, f.List.GetData(60));
            Assert.AreSame(anchor, f.View(60));
            Assert.AreSame(removalData, f.List.GetData(removalAnchor - 2));
            Assert.AreEqual(removalVisual, f.Visual(removalAnchor - 2), 0.01f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyLinearBackendsAlsoSupportIncrementalChanges(bool variable)
        {
            using var f = new Fixture(variable: variable, legacy: true);
            f.ShowMiddle();
            object data = f.List.GetData(60);
            float before = f.Visual(60);
            f.List.InsertData(0, new Row(1000));
            Assert.AreSame(data, f.List.GetData(61));
            Assert.AreEqual(before, f.Visual(61), 0.01f);
            f.List.RemoveAt(0);
            Assert.AreSame(data, f.List.GetData(60));
            f.List.TryGetItemToken(60, out var token);
            f.List.AddData(new Row(1001));
            Assert.IsTrue(token.IsCurrent);
        }

        [Test]
        public void AppendingOutsideViewportKeepsExistingBindingTokenAndPosition()
        {
            using var f = new Fixture();
            f.ShowMiddle();
            var view = f.View(60);
            int binds = view.Binds;
            float position = f.Position;
            f.List.TryGetItemToken(60, out var token);
            Assert.AreEqual(100, f.List.AddData(new Row(1000)));
            f.List.AddDatas(new[] { new Row(1001), new Row(1002) });
            Assert.AreEqual(103, f.List.Count);
            Assert.IsTrue(token.IsCurrent);
            Assert.AreSame(view, f.View(60));
            Assert.AreEqual(binds, view.Binds);
            Assert.AreEqual(position, f.Position, 0.01f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TailFollowingIsOptionalAndCoversNewItemMeasurements(bool follow)
        {
            using var f = new Fixture(variable: true);
            f.List.KeepEndOnDataChange = follow;
            f.List.KeepEndOnSizeChange = false;
            f.List.ScrollToView(99, UGUIVirtualListAlignment.End);
            float before = f.Position;
            f.List.AddDatas(new[] { new Row(1000, 220), new Row(1001, 240) });
            Assert.AreEqual(follow ? f.Layout.ContentLength - 300 : before, f.Position, 0.01f);
            f.ShowMiddle();
            before = f.Position;
            f.List.AddData(new Row(1002));
            Assert.AreEqual(before, f.Position, 0.01f, "浏览历史内容时不能强行拉到末尾");
        }

        [Test]
        public void RemovingAnchorSelectsSuccessorAndClearingKeepsPoolReusable()
        {
            using var f = new Fixture();
            f.ShowMiddle();
            object successor = f.List.GetData(61);
            f.List.TryGetItemToken(60, out var token);
            f.List.RemoveAt(60);
            Assert.AreSame(successor, f.List.GetData(60));
            Assert.AreSame(successor, f.View(60).Data);
            Assert.AreEqual(0, f.Visual(60), 0.01f);
            Assert.IsFalse(token.IsCurrent);
            int created = f.Created;
            f.List.Clear();
            Assert.AreEqual(0, f.List.Count);
            Assert.AreEqual(0, f.Position, 0.01f);
            Assert.AreEqual(-1, f.List.FirstVisibleIndex);
            Assert.AreEqual(0, f.Destroyed, "Clear 不销毁对象池");
            f.List.AddData(new Row(1000));
            Assert.AreEqual(created, f.Created);
            Assert.AreEqual(1000, ((Row)f.List.GetData(0)).Id);
        }

        [Test]
        public void RemovingTailClampsBoundsAndKeepsRemainingLastItemVisible()
        {
            using var f = new Fixture();
            f.List.ScrollToView(99, UGUIVirtualListAlignment.End);
            f.List.RemoveRange(95, 5);
            Assert.AreEqual(95, f.List.Count);
            Assert.AreEqual(9200, f.Position, 0.01f);
            Assert.IsTrue(f.List.TryGetItem(94, out _));
        }

        [Test]
        public void UpdatingOneItemChangesTypeAndDoesNotRebindItsNeighbour()
        {
            using var f = new Fixture(variable: true);
            f.ShowMiddle();
            var neighbour = f.View(61);
            int binds = neighbour.Binds;
            f.List.TryGetItemToken(60, out var oldToken);
            f.List.TryGetItemToken(61, out var neighbourToken);
            var replacement = new Row(2000, 180, 1);
            f.List.UpdateData(60, replacement);
            Assert.AreSame(replacement, f.List.GetData(60));
            Assert.AreSame(replacement, f.View(60).Data);
            Assert.AreEqual(1, f.View(60).ViewType);
            Assert.IsFalse(oldToken.IsCurrent);
            Assert.IsTrue(neighbourToken.IsCurrent);
            Assert.AreEqual(binds, neighbour.Binds);
        }

        [Test]
        public void OffscreenUpdateInvalidatesOnlyItsSizeAndDoesNotRebindVisibleAnchor()
        {
            using var f = new Fixture(variable: true);
            f.Rows[5].Size = 150;
            f.List.ScrollToView(5, UGUIVirtualListAlignment.Start);
            f.ShowMiddle();
            Assert.AreEqual(150, f.Layout.GetItemLayout(5).Size.y, 0.01f);
            f.List.TryGetItemToken(60, out var token);
            float before = f.Visual(60);
            f.List.UpdateData(5, new Row(5, 220));
            Assert.AreEqual(100, f.Layout.GetItemLayout(5).Size.y, 0.01f);
            Assert.IsTrue(token.IsCurrent);
            Assert.AreEqual(before, f.Visual(60), 0.01f);
            f.List.ScrollToView(5, UGUIVirtualListAlignment.Start);
            Assert.AreEqual(220, f.Layout.GetItemLayout(5).Size.y, 0.01f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MutableSizeCachesMoveWithDataInsteadOfRemainingAtOldIndex(bool legacy)
        {
            IUGUIVirtualListLayout layout = legacy
                ? (IUGUIVirtualListLayout)new UGUIVariableLinearLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 100))
                : new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 100), variableSize: true, allowMultiple: true);
            layout.SetItemCount(4, true);
            layout.SetItemSize(0, new Vector2(100, 150));
            layout.SetItemSize(1, new Vector2(100, 200));
            layout.SetItemSize(2, new Vector2(100, 250));
            layout.SetItemSize(3, new Vector2(100, 300));
            var mutable = (IUGUIVirtualListMutableLayout)layout;
            mutable.InsertItems(1, 2);
            Assert.AreEqual(200, layout.GetItemLayout(3).Size.y);
            Assert.AreEqual(100, layout.GetItemLayout(1).Size.y);
            mutable.RemoveItems(0, 1);
            Assert.AreEqual(200, layout.GetItemLayout(2).Size.y);
            mutable.InvalidateItemSize(2);
            Assert.AreEqual(100, layout.GetItemLayout(2).Size.y);
            Assert.AreEqual(250, layout.GetItemLayout(3).Size.y);
        }

        [Test]
        public void IndexBasedProvidersAreReevaluatedAfterInsertion()
        {
            using var f = new Fixture(indexTypes: true);
            f.ShowMiddle();
            object data = f.List.GetData(60);
            f.List.InsertData(0, new Row(1000));
            Assert.AreSame(data, f.List.GetData(61));
            Assert.AreEqual(1, f.View(61).ViewType);
            Assert.AreEqual(61, f.View(61).Index);
            Assert.AreSame(data, f.View(61).Data);
        }

        [Test]
        public void InvalidArgumentsAndFailingEnumerationsDoNotModifyExistingData()
        {
            using var f = new Fixture();
            f.List.TryGetItemToken(0, out var token);
            Assert.Throws<ArgumentOutOfRangeException>(() => f.List.InsertData(-1, new Row(999)));
            Assert.Throws<ArgumentOutOfRangeException>(() => f.List.RemoveRange(99, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => f.List.UpdateData(100, new Row(999)));
            Assert.Throws<ArgumentNullException>(() => f.List.AddDatas<Row>(null));
            Assert.Throws<InvalidOperationException>(() => f.List.AddDatas(FailingRows(f, false)));
            Assert.Throws<InvalidOperationException>(() => f.List.SetDatas(FailingRows(f, false)));
            Assert.Throws<InvalidOperationException>(() => f.List.AddDatas(FailingRows(f, true)));
            f.List.InsertDatas(0, Array.Empty<Row>());
            f.List.RemoveRange(100, 0);
            Assert.AreEqual(100, f.List.Count);
            Assert.AreSame(f.Rows[0], f.List.GetData(0));
            Assert.IsTrue(token.IsCurrent);
        }

        [Test]
        public void ReentrantStructuralChangeDuringBindIsRejectedAndGuardRecovers()
        {
            using var f = new Fixture();
            f.BindCallback = () => f.List.AddData(new Row(5000));
            Assert.Throws<InvalidOperationException>(() => f.List.UpdateData(0, new Row(1000)));
            Assert.AreEqual(100, f.List.Count);
            f.BindCallback = null;
            f.List.RefreshItem(0);
            f.List.AddData(new Row(5001));
            Assert.AreEqual(101, f.List.Count);
        }

        [Test]
        public void ReentrantChangeDuringRecycleCannotOrphanTheReleasedView()
        {
            using var f = new Fixture();
            int created = f.Created;
            f.RecycleCallback = () => f.List.AddData(new Row(5000));
            Assert.Throws<InvalidOperationException>(() => f.List.RemoveAt(0));
            Assert.AreEqual(100, f.List.Count);
            f.RecycleCallback = null;
            f.List.RefreshLayout();
            Assert.AreEqual(created, f.Created);
            Assert.AreSame(f.Rows[0], f.View(0).Data);
        }

        [Test]
        public void FailedBindInvalidatesItsTokenAndCanRetryWithoutRebindingEverything()
        {
            using var f = new Fixture();
            UGUIVirtualListItemToken failed = default;
            f.BindCallback = () =>
            {
                Assert.IsTrue(f.List.TryGetItemToken(0, out failed));
                throw new InvalidOperationException("预期的绑定失败");
            };
            Assert.Throws<InvalidOperationException>(() => f.List.UpdateData(0, new Row(1000)));
            Assert.IsFalse(failed.IsCurrent);
            Assert.IsFalse(f.List.TryGetItemToken(0, out _));
            int clicks = 0;
            f.List.ItemClicked += (index, data) => clicks++;
            f.View(0).Click();
            Assert.AreEqual(0, clicks);
            f.BindCallback = null;
            f.List.RefreshLayout();
            Assert.IsTrue(f.List.TryGetItemToken(0, out var current) && current.IsCurrent);
            Assert.AreEqual(1000, f.View(0).Data.Id);
        }

        [Test]
        public void RecycleFailureDuringSetDatasKeepsOldLayoutAndDataCountsConsistent()
        {
            using var f = new Fixture(variable: true);
            float length = f.Layout.ContentLength;
            f.RecycleCallback = () => throw new InvalidOperationException("预期的回收失败");
            Assert.Throws<InvalidOperationException>(() => f.List.SetDatas(new[] { new Row(1000) }));
            Assert.AreEqual(100, f.List.Count);
            Assert.AreEqual(length, f.Layout.ContentLength, 0.01f);
            f.RecycleCallback = null;
            f.List.Clear();
            Assert.AreEqual(0, f.List.Count);
            Assert.AreEqual(0, f.Layout.ContentLength);
        }

        [Test]
        public void OldClickHandlersCannotReadWrongIndicesDuringShiftRebinding()
        {
            using var f = new Fixture();
            f.ShowMiddle();
            var moving = f.View(61);
            int invalid = 0;
            int clicks = 0;
            f.List.ItemClicked += (index, data) =>
            {
                clicks++;
                if (index != moving.Index || !ReferenceEquals(data, moving.Data)) invalid++;
            };
            f.BindCallback = moving.Click;
            f.List.InsertData(0, new Row(1000));
            Assert.AreEqual(0, invalid);
            Assert.Greater(clicks, 0);
            f.BindCallback = null;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisposeRequestedInsideCallbacksWaitsUntilSafeAndCleansEverything(bool recycle)
        {
            using var f = new Fixture();
            if (recycle)
            {
                f.RecycleCallback = f.List.Dispose;
                f.List.RemoveAt(0);
            }
            else
            {
                f.BindCallback = f.List.Dispose;
                f.List.UpdateData(0, new Row(1000));
            }
            Assert.IsTrue(f.List.IsDisposalComplete);
            Assert.AreEqual(0, f.List.Count);
            Assert.AreEqual(f.Created, f.Destroyed);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ComponentReleaseRejectsReinitializationEvenWhenCleanupWasDeferred(bool deferred)
        {
            using var f = new Fixture();
            var component = f.Root.AddComponent<UGUIVirtualListComponent>();
            typeof(UGUIVirtualListComponent).GetField("_list", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, f.List);
            int attempts = 0;
            f.RecycleCallback = () =>
            {
                attempts++;
                var error = Assert.Throws<InvalidOperationException>(() => component.AddData(new Row(9000)));
                StringAssert.Contains("释放", error.Message);
            };
            if (deferred)
            {
                f.BindCallback = component.Release;
                f.List.UpdateData(0, new Row(1000));
            }
            else component.Release();
            Assert.Greater(attempts, 0);
            Assert.IsFalse(component.IsInitialized);
            Assert.IsTrue(f.List.IsDisposalComplete);
            Assert.AreEqual(f.Created, f.Destroyed);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExtremeSizeEstimatesContinueNextTickAndKeepTailFollowState(bool append)
        {
            int warnings = 0;
            Application.LogCallback logger = (message, trace, type) =>
            {
                if (type == LogType.Warning && message.StartsWith("虚拟列表布局在 20 轮内未收敛")) warnings++;
            };
            Application.logMessageReceived += logger;
            try
            {
                using var f = new Fixture(variable: true, count: append ? 0 : 100, initialSize: 1, estimate: 1000);
                if (append)
                {
                    f.List.KeepEndOnDataChange = true;
                    var rows = new List<Row>();
                    for (int i = 0; i < 100; i++) rows.Add(new Row(i, 1));
                    f.List.AddDatas(rows);
                }
                Assert.IsTrue(f.List.HasPendingLayoutWork);
                for (int tick = 0; tick < 10 && f.List.HasPendingLayoutWork; tick++)
                    f.List.RefreshViewportIfChanged();
                Assert.IsFalse(f.List.HasPendingLayoutWork);
                Assert.AreEqual(100, f.Layout.ContentLength, 0.01f);
                Assert.AreEqual(0, f.Position, 0.01f);
                Assert.AreEqual(1, warnings, "连续未收敛只提示一次，不每帧刷屏");
            }
            finally { Application.logMessageReceived -= logger; }
        }

        [Test]
        public void CustomLayoutWithoutMutableCapabilitySafelyRemeasuresVisibleItems()
        {
            using var f = new Fixture(variable: true, fallback: true);
            f.ShowMiddle();
            f.Rows[60].Size = 150;
            f.List.RefreshItem(60);
            f.List.TryGetItemToken(60, out var token);
            f.List.AddData(new Row(1000));
            Assert.IsTrue(token.IsCurrent);
            Assert.AreEqual(150, f.Layout.GetItemLayout(60).Size.y, 0.01f);
            Assert.AreEqual(150, f.View(60).RectTransform.rect.height, 0.01f);
        }

        [Test]
        public void AddFromEmptyCanFollowEndAndDisposedListRejectsChanges()
        {
            using var f = new Fixture(variable: true);
            f.List.Clear();
            f.List.KeepEndOnDataChange = true;
            f.List.AddDatas(new[] { new Row(1000, 240), new Row(1001, 240) });
            Assert.AreEqual(f.Layout.ContentLength - 300, f.Position, 0.01f);
            f.List.Dispose();
            Assert.Throws<ObjectDisposedException>(() => f.List.AddData(new Row(1002)));
            Assert.Throws<ObjectDisposedException>(() => f.List.Clear());
        }

        private static IEnumerable<Row> FailingRows(Fixture f, bool nested)
        {
            yield return new Row(1000);
            if (nested) f.List.AddData(new Row(1001));
            throw new InvalidOperationException("预期的枚举失败");
        }

        private sealed class Row
        {
            public readonly int Id;
            public readonly int Kind;
            public float Size;
            public Row(int id, float size = 100, int? kind = null) { Id = id; Size = size; Kind = kind ?? id % 2; }
        }

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly ScrollRect Scroll;
            public readonly UGUIVirtualList List;
            public readonly IUGUIVirtualListLayout Layout;
            public readonly List<Row> Rows = new();
            public int Created;
            public int Destroyed;
            public Action BindCallback;
            public Action RecycleCallback;
            private readonly UGUIVirtualListAxis _axis;
            private readonly bool _indexTypes;
            public Fixture(UGUIVirtualListAxis axis = UGUIVirtualListAxis.Vertical, bool variable = false,
                bool multiple = false, bool indexTypes = false, bool fallback = false, bool legacy = false,
                int count = 100, float initialSize = 100, float estimate = 100)
            {
                _axis = axis;
                _indexTypes = indexTypes;
                Root = new GameObject("Data update test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                Scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                Scroll.viewport = (RectTransform)viewport.transform;
                Scroll.viewport.sizeDelta = new Vector2(300, 300);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                Scroll.content = (RectTransform)content.transform;
                Scroll.vertical = axis == UGUIVirtualListAxis.Vertical;
                Scroll.horizontal = !Scroll.vertical;
                Scroll.movementType = ScrollRect.MovementType.Clamped;
                Vector2 size = axis == UGUIVirtualListAxis.Vertical ? new Vector2(100, estimate) : new Vector2(estimate, 100);
                Layout = fallback ? (IUGUIVirtualListLayout)new FallbackLayout(axis)
                    : legacy ? variable ? (IUGUIVirtualListLayout)new UGUIVariableLinearLayout(axis, size) : new UGUIFixedLinearLayout(axis, size)
                    : new UGUIFlowLayout(axis, size, variable, multiple);
                List = new UGUIVirtualList(Scroll, Layout, this) { BufferSize = 200 };
                for (int i = 0; i < count; i++) Rows.Add(new Row(i, initialSize));
                List.SetDatas(Rows);
            }
            public void ShowMiddle() => List.ScrollToView(60, UGUIVirtualListAlignment.Start);
            public float Position => _axis == UGUIVirtualListAxis.Vertical ? Scroll.content.anchoredPosition.y : -Scroll.content.anchoredPosition.x;
            public View View(int index)
            {
                Assert.IsTrue(List.TryGetItem(index, out var item), $"Item {index} 应已创建");
                return (View)item;
            }
            public float Visual(int index)
            {
                var p = View(index).RectTransform.anchoredPosition + Scroll.content.anchoredPosition;
                return _axis == UGUIVirtualListAxis.Vertical ? -p.y : p.x;
            }
            public int GetItemType(int index, object data) => _indexTypes ? index % 2 : ((Row)data).Kind;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                Created++;
                var go = new GameObject("Data Item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go, itemType, _axis, this);
            }
            public void Destroy(IUGUIVirtualListItem item) { Destroyed++; UnityEngine.Object.DestroyImmediate(item.GameObject); }
            public void Dispose() { List.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }

        private sealed class View : IUGUIVirtualListItem
        {
            private readonly UGUIVirtualListAxis _axis;
            private readonly Fixture _owner;
            private Action _click;
            public int Index;
            public int Binds;
            public Row Data;
            public readonly int ViewType;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go, int viewType, UGUIVirtualListAxis axis, Fixture owner)
            { GameObject = go; ViewType = viewType; _axis = axis; _owner = owner; }
            public void Bind(int index, object data)
            {
                Index = index;
                Data = (Row)data;
                Binds++;
                RectTransform.SetSizeWithCurrentAnchors(_axis == UGUIVirtualListAxis.Vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, Data.Size);
                _owner.BindCallback?.Invoke();
            }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { _owner.RecycleCallback?.Invoke(); }
            public void SetClickHandler(Action callback) { _click = callback; }
            public void Click() => _click?.Invoke();
        }

        private sealed class FallbackLayout : IUGUIVirtualListLayout
        {
            private readonly UGUIVariableLinearLayout _inner;
            public FallbackLayout(UGUIVirtualListAxis axis) { _inner = new UGUIVariableLinearLayout(axis, new Vector2(100, 100)); }
            public bool IsVertical => _inner.IsVertical;
            public bool SupportsDynamicSize => true;
            public float ContentLength => _inner.ContentLength;
            public float CrossSize => _inner.CrossSize;
            public void SetItemCount(int count, bool resetSizes) => _inner.SetItemCount(count, resetSizes);
            public bool SetItemSize(int index, Vector2 size) => _inner.SetItemSize(index, size);
            public UGUIVirtualListItemLayout GetItemLayout(int index) => _inner.GetItemLayout(index);
            public UGUIVirtualListRange GetVisibleRange(float position, float length, float buffer) => _inner.GetVisibleRange(position, length, buffer);
        }
    }
}
