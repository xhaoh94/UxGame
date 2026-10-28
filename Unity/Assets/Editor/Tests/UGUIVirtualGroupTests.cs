using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualGroupTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ProjectionIncludesHeadersAndOptionallyEmptyGroups(bool showEmpty)
        {
            using var f = new Fixture(showEmpty: showEmpty);
            f.Groups.SetGroups(new[] { new UGUIVirtualGroup("empty", "Empty"), UGUIVirtualGroup.Create("a", "A", new[] { 1, 2 }) });
            Assert.AreEqual(2, f.Groups.GroupCount);
            Assert.AreEqual(showEmpty ? 2 : 1, f.Groups.VisibleGroupCount);
            Assert.AreEqual(2, f.Groups.DataCount);
            Assert.AreEqual(showEmpty ? 4 : 3, f.Core.Count);
            Assert.AreEqual(showEmpty ? 1 : 0, f.Groups.GetHeaderIndex("a"));
            Assert.AreEqual(showEmpty ? 0 : -1, f.Groups.GetHeaderIndex("empty"));
            Assert.IsTrue(f.Groups.UpdateHeader("empty", "New empty"));
            Assert.IsTrue(f.Groups.OwnsCurrentData);
        }

        [Test]
        public void DuplicateOrNullGroupsAndEnumerationFailureDoNotReplaceOldProjection()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample());
            f.Core.TryGetItemToken(0, out var token);
            Assert.Throws<ArgumentException>(() => new UGUIVirtualGroup(" ", null));
            Assert.Throws<ArgumentException>(() => f.Groups.SetGroups(new[] { new UGUIVirtualGroup("a", "A"), new UGUIVirtualGroup("a", "Again") }));
            Assert.Throws<ArgumentException>(() => f.Groups.SetGroups(new UGUIVirtualGroup[] { null }));
            Assert.Throws<InvalidOperationException>(() => f.Groups.SetGroups(FailingGroups()));
            Assert.IsTrue(token.IsCurrent);
            Assert.AreEqual(3, f.Groups.GroupCount);
            Assert.IsTrue(f.Groups.OwnsCurrentData);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StickyHeaderAppearsAtTopAndNextHeaderPushesItOut(bool variable)
        {
            using var f = new Fixture(variable);
            f.Groups.SetGroups(Sample());
            f.Sticky.Tick();
            Assert.IsFalse(f.Sticky.IsVisible, "起点留白尚未越过，不能提前出现副本");
            float first = -f.Core.GetItemLayout(0).Position.y;
            f.ScrollTo(first);
            f.Sticky.Tick();
            Assert.AreEqual("g-0", f.Sticky.CurrentGroupId);
            Assert.IsTrue(f.Sticky.CurrentObject.GetComponentInParent<CanvasGroup>().blocksRaycasts == false);
            Assert.IsTrue(f.Core.TryGetItem(0, out var inline));
            Assert.AreSame(f.Scroll.content, inline.GameObject.transform.parent, "不能把真实 Header 移到吸顶层");
            float next = -f.Core.GetItemLayout(f.Groups.GetHeaderIndex("g-1")).Position.y;
            f.ScrollTo(next - 20);
            f.Sticky.Tick();
            float expected = Math.Min(0, 20 - f.Sticky.HeaderLength);
            Assert.AreEqual(expected, f.Sticky.PushOffset, 0.01f);
            Assert.AreEqual(-expected, f.Sticky.CurrentObject.GetComponent<RectTransform>().anchoredPosition.y, 0.01f);
            f.ScrollTo(next);
            f.Sticky.Tick();
            Assert.AreEqual("g-1", f.Sticky.CurrentGroupId);
            Assert.AreEqual(0, f.Sticky.PushOffset, 0.01f);
        }

        [Test]
        public void SameGroupScrollingDoesNotRebindOrChangeTokenAndHiddenInlineHeaderIsIndependent()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample());
            f.ScrollTo(12);
            f.Sticky.Tick();
            Assert.IsTrue(f.Sticky.TryGetToken(out var token));
            var view = f.LastStickyView;
            int binds = view.Binds;
            f.ScrollTo(100); f.Sticky.Tick();
            f.ScrollTo(150); f.Sticky.Tick();
            Assert.AreEqual(binds, view.Binds);
            Assert.IsTrue(token.IsCurrent);
            Assert.IsFalse(f.Core.TryGetItem(0, out _));
            Assert.IsNotNull(token.GameObject);
            Assert.IsTrue(view.Row.IsSticky);
        }

        [Test]
        public void HeaderUpdateAndForestReplacementImmediatelyExpireOldStickyTokens()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample()); f.ScrollTo(100); f.Sticky.Tick();
            f.Sticky.TryGetToken(out var previous);
            f.Groups.UpdateHeader("g-0", "Updated header");
            Assert.IsFalse(previous.IsCurrent);
            f.Sticky.Tick();
            Assert.AreEqual("Updated header", f.LastStickyView.Row.Data);
            f.Sticky.TryGetToken(out previous);
            f.Groups.SetGroups(Sample());
            Assert.IsFalse(previous.IsCurrent);
            f.ScrollTo(100); f.Sticky.Tick();
            Assert.AreEqual("Header 0", f.LastStickyView.Row.Data);
        }

        [Test]
        public void ItemUpdatesDoNotRebindStickyHeaderAndGroupNavigationUsesHeaderId()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample()); f.ScrollTo(100); f.Sticky.Tick();
            int binds = f.LastStickyView.Binds;
            f.Groups.UpdateItem("g-0", 1, "Changed content");
            f.Sticky.Tick();
            Assert.AreEqual(binds, f.LastStickyView.Binds);
            Assert.IsTrue(f.Groups.ScrollToGroup("g-1"));
            f.Sticky.Tick();
            Assert.AreEqual("g-1", f.Sticky.CurrentGroupId);
            Assert.IsFalse(f.Groups.ScrollToGroup("unknown"));
        }

        [Test]
        public void StickyFactoriesPoolByTypeAndDoNotInstantiateOnEveryGroupSwitch()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample());
            for (int n = 0; n < 5; n++)
            {
                f.Groups.ScrollToGroup("g-0"); f.Sticky.Tick();
                f.Groups.ScrollToGroup("g-1"); f.Sticky.Tick();
            }
            Assert.AreEqual(2, f.StickyCreated);
        }

        [Test]
        public void StickySizeNotificationDoesNotBindAndOldTokensCannotResizeNewHeader()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample()); f.ScrollTo(100); f.Sticky.Tick();
            f.Sticky.TryGetToken(out var token);
            int binds = f.LastStickyView.Binds;
            f.LastStickyView.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 90);
            Assert.IsTrue(f.Sticky.NotifySizeChanged(token));
            Assert.AreEqual(90, f.Sticky.HeaderLength, 0.01f);
            Assert.AreEqual(binds, f.LastStickyView.Binds);
            f.Groups.ScrollToGroup("g-1"); f.Sticky.Tick();
            Assert.IsFalse(f.Sticky.NotifySizeChanged(token));
        }

        [Test]
        public void ReentrantDataChangesInStickyRendererAreGuardedAndCanRecover()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample()); f.ScrollTo(100);
            f.StickyBind = row => f.Groups.SetGroups(Sample());
            Assert.Throws<InvalidOperationException>(f.Sticky.Tick);
            Assert.IsNotNull(f.Sticky.LastError);
            Assert.IsFalse(f.Sticky.IsVisible);
            Assert.IsTrue(f.Groups.OwnsCurrentData);
            f.StickyBind = null;
            f.Sticky.Refresh();
            Assert.IsTrue(f.Sticky.IsVisible);
        }

        [Test]
        public void ReleasingStickyDuringItsBindIsDeferredAndDoesNotDisposeCore()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample()); f.ScrollTo(100);
            f.StickyBind = row => f.Sticky.Dispose();
            f.Sticky.Tick();
            Assert.IsTrue(f.Sticky.IsDisposalComplete);
            Assert.AreEqual(f.StickyCreated, f.StickyDestroyed);
            Assert.IsFalse(f.Core.IsDisposalComplete);
            Assert.IsFalse(f.Sticky.TryGetToken(out _));
        }

        [Test]
        public void DataOwnershipMismatchHidesOldHeaderAndSetGroupsCanTakeOverAgain()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample()); f.ScrollTo(100); f.Sticky.Tick();
            f.Core.AddData(f.Core.GetData(0));
            f.Sticky.Tick();
            Assert.IsFalse(f.Sticky.IsVisible);
            Assert.Throws<InvalidOperationException>(() => f.Groups.UpdateHeader("g-0", "No"));
            f.Groups.SetGroups(Sample()); f.ScrollTo(100); f.Sticky.Tick();
            Assert.IsTrue(f.Sticky.IsVisible);
        }

        [Test]
        public void SetGroupsRecycleFailureKeepsOldModelAndCommittedBindFailureKeepsNewModel()
        {
            using var f = new Fixture();
            f.Groups.SetGroups(Sample());
            f.NormalRecycle = () => throw new InvalidOperationException("预期分组回收错误");
            Assert.Throws<InvalidOperationException>(() => f.Groups.SetGroups(null));
            Assert.AreEqual(3, f.Groups.GroupCount);
            Assert.AreEqual(f.Groups.RowCount, f.Core.Count);
            f.NormalRecycle = null;
            f.Core.RefreshLayout();
            f.NormalBind = row => throw new InvalidOperationException("预期分组绑定错误");
            Assert.Throws<InvalidOperationException>(() => f.Groups.SetGroups(new[] { new UGUIVirtualGroup("new", "New") }));
            Assert.AreEqual(1, f.Groups.GroupCount);
            Assert.IsTrue(f.Groups.OwnsCurrentData);
            f.NormalBind = null;
            f.Core.RefreshLayout();
        }

        private static IEnumerable<UGUIVirtualGroup> FailingGroups()
        {
            yield return new UGUIVirtualGroup("temp", "Temp");
            throw new InvalidOperationException("预期枚举错误");
        }
        private static UGUIVirtualGroup[] Sample()
        {
            var result = new List<UGUIVirtualGroup>();
            for (int g = 0; g < 3; g++) result.Add(UGUIVirtualGroup.Create("g-" + g, "Header " + g, new[] { "A", "B", "C" }));
            return result.ToArray();
        }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly ScrollRect Scroll;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualGroupAdapter Groups;
            public readonly UGUIVirtualStickyHeader Sticky;
            public int StickyCreated;
            public int StickyDestroyed;
            public View LastStickyView;
            public Action<UGUIVirtualGroupItem> StickyBind;
            public Action<UGUIVirtualGroupItem> NormalBind;
            public Action NormalRecycle;
            public Fixture(bool variable = true, bool showEmpty = true)
            {
                Root = new GameObject("Group test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                Scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                Scroll.viewport = (RectTransform)viewport.transform;
                Scroll.viewport.sizeDelta = new Vector2(300, 120);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                Scroll.content = (RectTransform)content.transform;
                Scroll.vertical = true; Scroll.horizontal = false;
                Scroll.movementType = ScrollRect.MovementType.Clamped;
                Core = new UGUIVirtualList(Scroll, new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 60),
                    variable, spacing: new Vector2(0, 4), paddingStart: 12, paddingEnd: 12), new Factory(this, false)) { BufferSize = 80 };
                Groups = new UGUIVirtualGroupAdapter(Core, showEmpty);
                Sticky = new UGUIVirtualStickyHeader(Core, Groups, new Factory(this, true));
            }
            public void ScrollTo(float position)
            {
                var p = Scroll.content.anchoredPosition; p.y = position; Scroll.content.anchoredPosition = p;
                Core.RefreshLayout();
            }
            public void Dispose()
            {
                StickyBind = null; NormalBind = null; NormalRecycle = null;
                Sticky.Dispose(); Groups.Dispose(); Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root);
            }
        }
        private sealed class Factory : IUGUIVirtualListItemFactory
        {
            private readonly Fixture _owner;
            private readonly bool _sticky;
            public Factory(Fixture owner, bool sticky) { _owner = owner; _sticky = sticky; }
            public int GetItemType(int index, object data)
            {
                var row = (UGUIVirtualGroupItem)data;
                return row.IsHeader ? row.GroupIndex % 2 == 0 ? 0 : 2 : 1;
            }
            public IUGUIVirtualListItem Create(int type, Transform parent)
            {
                var root = new GameObject("Group view", typeof(RectTransform)); root.transform.SetParent(parent, false);
                var view = new View(root, _owner, _sticky);
                if (_sticky) { _owner.StickyCreated++; _owner.LastStickyView = view; }
                return view;
            }
            public void Destroy(IUGUIVirtualListItem item)
            { if (_sticky) _owner.StickyDestroyed++; UnityEngine.Object.DestroyImmediate(item.GameObject); }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            private readonly Fixture _owner;
            private readonly bool _sticky;
            public int Binds;
            public UGUIVirtualGroupItem Row;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject root, Fixture owner, bool sticky) { GameObject = root; _owner = owner; _sticky = sticky; }
            public void Bind(int index, object data)
            {
                Row = (UGUIVirtualGroupItem)data;
                Assert.AreEqual(_sticky, Row.IsSticky);
                Binds++;
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Row.IsHeader ? 40 : 60);
                if (_sticky) { _owner.LastStickyView = this; _owner.StickyBind?.Invoke(Row); }
                else _owner.NormalBind?.Invoke(Row);
            }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { if (!_sticky) _owner.NormalRecycle?.Invoke(); }
            public void SetClickHandler(Action callback) { }
        }
    }
}
