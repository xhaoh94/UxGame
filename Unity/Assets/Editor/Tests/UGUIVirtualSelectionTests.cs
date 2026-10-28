using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualSelectionTests
    {
        [Test]
        public void SingleModeKeepsExactlyOneAndReplacesPrevious()
        {
            using var f = new Fixture(UGUIVirtualSelectionMode.Single);
            Assert.IsTrue(f.Selection.Select(3));
            Assert.IsTrue(f.Selection.IsSelected(3));
            Assert.IsTrue(f.Selection.Select(5));
            Assert.IsFalse(f.Selection.IsSelected(3));
            Assert.IsTrue(f.Selection.IsSelected(5));
            Assert.AreEqual(1, f.Selection.SelectedCount);
            Assert.IsFalse(f.Selection.Select(5));
            Assert.IsTrue(f.Selection.Deselect(5));
            Assert.IsFalse(f.Selection.HasSelection);
        }

        [Test]
        public void MultipleModeTogglesAndSelectRangeAddsOrReplaces()
        {
            using var f = new Fixture(UGUIVirtualSelectionMode.Multiple);
            Assert.IsTrue(f.Selection.Toggle(2));
            Assert.IsTrue(f.Selection.Toggle(4));
            Assert.IsTrue(f.Selection.Toggle(2));
            Assert.IsFalse(f.Selection.IsSelected(2));
            f.Selection.SelectRange(5, 8);
            CollectionAssert.AreEqual(new[] { 5, 6, 7, 8 }, f.Selection.GetSelectedIndices());
            f.Selection.SelectRange(0, 1, additive: true);
            Assert.AreEqual(6, f.Selection.SelectedCount);
            f.Selection.SelectAll();
            Assert.AreEqual(f.Core.Count, f.Selection.SelectedCount);
            f.Selection.ClearSelection();
            Assert.AreEqual(0, f.Selection.SelectedCount);
        }

        [Test]
        public void NoneModeIgnoresRequestsAndModeSwitchNarrowsSelection()
        {
            using var f = new Fixture(UGUIVirtualSelectionMode.None);
            Assert.IsFalse(f.Selection.Select(1));
            Assert.IsFalse(f.Selection.Toggle(1));
            Assert.IsFalse(f.Selection.HasSelection);
            f.Selection.Mode = UGUIVirtualSelectionMode.Multiple;
            f.Selection.SelectRange(3, 5);
            Assert.AreEqual(3, f.Selection.SelectedCount);
            f.Selection.Mode = UGUIVirtualSelectionMode.Single;
            Assert.AreEqual(1, f.Selection.SelectedCount);
            Assert.IsTrue(f.Selection.IsSelected(3));
            f.Selection.Mode = UGUIVirtualSelectionMode.None;
            Assert.IsFalse(f.Selection.HasSelection);
        }

        [Test]
        public void SelectionSurvivesRecyclingAndRebind()
        {
            using var f = new Fixture(UGUIVirtualSelectionMode.Multiple);
            f.Selection.Select(0);
            Assert.IsTrue(f.Core.TryGetItem(0, out _));
            f.Core.ScrollToView(500, UGUIVirtualListAlignment.Start);
            Assert.IsFalse(f.Core.TryGetItem(0, out _));
            f.Core.ScrollToView(0, UGUIVirtualListAlignment.Start);
            Assert.IsTrue(f.Selection.IsSelected(0), "滚动复用后选择必须仍保留");
            Assert.IsTrue(f.Core.TryGetItem(0, out _));
        }

        [Test]
        public void InsertAndRemoveRemapSelectionIndices()
        {
            using var f = new Fixture(UGUIVirtualSelectionMode.Multiple);
            f.Selection.Select(2);
            f.Selection.Select(5);
            f.Core.InsertData(0, "inserted");
            CollectionAssert.AreEqual(new[] { 3, 6 }, f.Selection.GetSelectedIndices());
            f.Core.RemoveRange(3, 1);
            CollectionAssert.AreEqual(new[] { 5 }, f.Selection.GetSelectedIndices());
            f.Core.RemoveAt(0);
            CollectionAssert.AreEqual(new[] { 4 }, f.Selection.GetSelectedIndices());
            f.Core.AddData("appended");
            CollectionAssert.AreEqual(new[] { 4 }, f.Selection.GetSelectedIndices());
        }

        [Test]
        public void DataResetClearsSelectionAndNotifiesDeselected()
        {
            using var f = new Fixture(UGUIVirtualSelectionMode.Multiple);
            f.Selection.Select(1);
            var changes = new List<(int, bool)>();
            f.Selection.SelectionChanged += (index, data, selected) => changes.Add((index, selected));
            f.Core.SetDatas(new[] { "a", "b" });
            Assert.IsFalse(f.Selection.HasSelection);
            CollectionAssert.AreEqual(new[] { (1, false) }, changes);
        }

        [Test]
        public void ChangedEventReportsOnlyDifferencesAndRefreshesVisibleItems()
        {
            using var f = new Fixture(UGUIVirtualSelectionMode.Multiple);
            var changes = new List<(int, bool)>();
            f.Selection.SelectionChanged += (index, data, selected) => changes.Add((index, selected));
            f.Selection.Select(0);
            f.Selection.Select(1);
            f.Selection.Deselect(0);
            CollectionAssert.AreEqual(new[] { (0, true), (1, true), (0, false) }, changes);
            int binds = f.BindCountFor(1);
            f.Selection.Deselect(1);
            Assert.AreEqual(binds + 1, f.BindCountFor(1), "取消选中只重绑该项一次");
            binds = f.BindCountFor(1);
            f.Selection.Select(1);
            f.Selection.Select(1);
            Assert.AreEqual(binds + 1, f.BindCountFor(1), "重复选中同一项不应再次重绑");
        }

        [Test]
        public void RendererCanReadSelectionDuringBindAndBatchCompletes()
        {
            using var f = new Fixture(UGUIVirtualSelectionMode.Multiple, inspectSelection: true);
            int batch = 0;
            f.Selection.SelectionBatchCompleted += () => batch++;
            f.Selection.SelectRange(0, 2);
            Assert.AreEqual(1, batch);
            Assert.IsTrue(f.SawSelectedInBind, "Bind 时 IsSelected 应已经生效");
        }

        [Test]
        public void DisposeStopsTrackingAndInvalidIndicesAreRejected()
        {
            var f = new Fixture(UGUIVirtualSelectionMode.Multiple);
            try
            {
                Assert.IsFalse(f.Selection.Select(-1));
                Assert.IsFalse(f.Selection.Select(1000));
                f.Selection.Select(1);
                f.Selection.Dispose();
                Assert.IsFalse(f.Selection.Select(2));
                f.Core.AddData("x");
                Assert.Throws<ObjectDisposedException>(() => f.Selection.Mode = UGUIVirtualSelectionMode.Single);
            }
            finally { f.Dispose(); }
        }

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualSelection Selection;
            public bool SawSelectedInBind;
            private readonly bool _inspectSelection;
            private readonly List<object> _data = new List<object>();
            public Fixture(UGUIVirtualSelectionMode mode, bool inspectSelection = false)
            {
                _inspectSelection = inspectSelection;
                Root = new GameObject("Selection test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
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
                for (int i = 0; i < 1000; i++) _data.Add("row " + i);
                Core.SetDatas(_data);
                Selection = new UGUIVirtualSelection(Core, mode);
                if (_inspectSelection)
                    Selection.SelectionChanged += (index, data, selected) => { if (selected) SawSelectedInBind = true; };
            }
            public int BindCountFor(int index) => Core.TryGetItem(index, out var item) ? ((View)item).Binds : 0;
            public int GetItemType(int index, object data) => 0;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Selection item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose()
            {
                Selection.Dispose();
                Core.Dispose();
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            public int Binds;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go) { GameObject = go; }
            public void Bind(int index, object data) => Binds++;
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }
}
