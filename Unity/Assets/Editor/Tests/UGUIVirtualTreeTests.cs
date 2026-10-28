using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualTreeTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void TreeProjectionWorksWithBothFixedAndVariableSizeBackends(bool variable)
        {
            using var f = new Fixture(variable);
            f.Tree.SetRoots(Forest());
            Assert.IsTrue(f.Tree.Expand("a"));
            Assert.AreEqual(6, f.List.Count);
            Assert.IsTrue(f.View(0).RenderedExpanded);
            f.Tree.UpdateNodeData("a/leaf", "Changed");
            Assert.AreEqual("Changed", f.View(1).RenderedData);
            Assert.IsTrue(f.Tree.Collapse("a"));
            Assert.AreEqual(2, f.List.Count);
        }

        [Test]
        public void NestedExpansionMetadataAndChildStateArePreserved()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            Assert.AreEqual(6, f.Tree.TotalNodeCount);
            AssertIds(f.Tree, "a", "b");
            Assert.IsTrue(f.Tree.Expand("a"));
            AssertIds(f.Tree, "a", "a/leaf", "a/branch", "a/branch/x", "a/branch/y", "b");
            Assert.IsTrue(f.Tree.TryGetItem("a/branch/x", out var nested));
            Assert.AreEqual(2, nested.Depth);
            Assert.AreEqual("a/branch", nested.ParentId);
            f.Tree.Collapse("a");
            AssertIds(f.Tree, "a", "b");
            f.Tree.Expand("a");
            Assert.AreEqual(6, f.Tree.VisibleCount);
            f.Tree.Collapse("a/branch");
            Assert.AreEqual(4, f.Tree.VisibleCount);
            f.Tree.Collapse("a");
            f.Tree.Expand("a");
            AssertIds(f.Tree, "a", "a/leaf", "a/branch", "b");
        }

        [Test]
        public void HiddenBranchCanRememberExpansionIntentWithoutTouchingVisibleBindings()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            f.List.TryGetItemToken(0, out var token);
            Assert.IsTrue(f.Tree.Collapse("a/branch"));
            Assert.AreEqual(2, f.List.Count);
            Assert.IsTrue(token.IsCurrent);
            f.Tree.Expand("a");
            Assert.AreEqual(4, f.List.Count);
            Assert.AreEqual(-1, f.Tree.GetVisibleIndex("a/branch/x"));
        }

        [Test]
        public void ExpandingOnlyRebindsParentAndShiftedRows()
        {
            using var f = new Fixture();
            var roots = new List<UGUIVirtualTreeNode> { new UGUIVirtualTreeNode("prefix", "Prefix") };
            roots.AddRange(Forest());
            f.Tree.SetRoots(roots);
            var prefix = f.View(0);
            int binds = prefix.Binds;
            f.List.TryGetItemToken(0, out var prefixToken);
            f.List.TryGetItemToken(2, out var tailToken);
            f.Tree.Expand("a");
            Assert.IsTrue(prefixToken.IsCurrent);
            Assert.AreEqual(binds, prefix.Binds);
            Assert.IsFalse(tailToken.IsCurrent);
            Assert.IsTrue(f.View(1).RenderedExpanded);
            Assert.AreEqual(6, f.Tree.GetVisibleIndex("b"));
            Assert.AreEqual("b", f.View(6).Item.Id);
            f.Tree.Collapse("a");
            Assert.IsTrue(prefixToken.IsCurrent);
            Assert.IsFalse(f.View(1).RenderedExpanded);
        }

        [Test]
        public void ScrollToHiddenNodeCanExpandAncestorsOrRefuseWithoutExpanding()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            Assert.IsFalse(f.Tree.ScrollToNode("a/branch/y", false));
            Assert.IsTrue(f.Tree.ScrollToNode("a/branch/y"));
            Assert.AreEqual(4, f.Tree.GetVisibleIndex("a/branch/y"));
            Assert.IsTrue(f.List.TryGetItem(4, out _));
            Assert.AreEqual(6, f.Tree.VisibleCount);
        }

        [Test]
        public void CollapsingAncestorWhileViewingDescendantsBringsParentBackIntoView()
        {
            using var f = new Fixture();
            var leaves = new List<UGUIVirtualTreeNode>();
            for (int i = 0; i < 100; i++) leaves.Add(new UGUIVirtualTreeNode("leaf-" + i, i));
            f.Tree.SetRoots(new[] { new UGUIVirtualTreeNode("root", "Root", leaves, true), new UGUIVirtualTreeNode("tail", "Tail") });
            Assert.IsTrue(f.Tree.ScrollToNode("leaf-80"));
            Assert.IsFalse(f.List.TryGetItem(0, out _));
            f.Tree.Collapse("root");
            Assert.AreEqual(2, f.List.Count);
            Assert.IsTrue(f.List.TryGetItem(0, out var header));
            Assert.AreEqual("root", ((UGUIVirtualTreeItem)f.List.GetData(0)).Id);
            Assert.IsFalse(((View)header).RenderedExpanded);
        }

        [Test]
        public void ExpandingOrCollapsingUnrelatedBranchBeforeViewportKeepsLogicalAnchor()
        {
            using var f = new Fixture();
            var children = new List<UGUIVirtualTreeNode>();
            for (int i = 0; i < 20; i++) children.Add(new UGUIVirtualTreeNode("child-" + i, i));
            var roots = new List<UGUIVirtualTreeNode> { new UGUIVirtualTreeNode("folder", "Folder", children) };
            for (int i = 0; i < 100; i++) roots.Add(new UGUIVirtualTreeNode("tail-" + i, i));
            f.Tree.SetRoots(roots);
            f.Tree.ScrollToNode("tail-80");
            int oldIndex = f.Tree.GetVisibleIndex("tail-80");
            var view = f.View(oldIndex);
            float before = f.Visual(oldIndex);
            int created = f.Created;
            f.Tree.Expand("folder");
            int expandedIndex = f.Tree.GetVisibleIndex("tail-80");
            Assert.AreEqual(oldIndex + 20, expandedIndex);
            Assert.AreSame(view, f.View(expandedIndex));
            Assert.AreEqual(before, f.Visual(expandedIndex), 0.01f);
            Assert.AreEqual(created, f.Created, "只展开数据，不创建视口外的子节点实例");
            f.Tree.Collapse("folder");
            Assert.AreEqual(before, f.Visual(f.Tree.GetVisibleIndex("tail-80")), 0.01f);
        }

        [Test]
        public void ExpandingLastFolderDoesNotFollowChatTailPolicy()
        {
            using var f = new Fixture();
            var roots = new List<UGUIVirtualTreeNode>();
            for (int i = 0; i < 20; i++) roots.Add(new UGUIVirtualTreeNode("prefix-" + i, i));
            var children = new List<UGUIVirtualTreeNode>();
            for (int i = 0; i < 100; i++) children.Add(new UGUIVirtualTreeNode("child-" + i, i));
            roots.Add(new UGUIVirtualTreeNode("folder", "Folder", children));
            f.Tree.SetRoots(roots);
            f.List.KeepEndOnDataChange = true;
            f.List.ScrollToView(20, UGUIVirtualListAlignment.End);
            float before = f.Position;
            f.Tree.Expand("folder");
            Assert.AreEqual(before, f.Position, 0.01f);
            Assert.IsTrue(f.List.TryGetItem(20, out _));
            Assert.IsTrue(f.List.KeepEndOnDataChange);
        }

        [Test]
        public void HiddenDataUpdatesDoNotCreateViewsAndVisibleDataUpdatesInvalidateOnlyTarget()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            int created = f.Created;
            f.List.TryGetItemToken(0, out var rootToken);
            Assert.IsTrue(f.Tree.UpdateNodeData("a/branch/x", "Updated hidden leaf"));
            Assert.AreEqual(created, f.Created);
            Assert.IsTrue(rootToken.IsCurrent);
            f.Tree.ScrollToNode("a/branch/x");
            Assert.AreEqual("Updated hidden leaf", f.View(3).RenderedData);
            f.List.TryGetItemToken(1, out var neighbour);
            f.List.TryGetItemToken(0, out rootToken);
            f.Tree.UpdateNodeData("a", "Updated folder");
            Assert.IsFalse(rootToken.IsCurrent);
            Assert.IsTrue(neighbour.IsCurrent);
            Assert.IsTrue(f.Tree.TryGetItem("a", out var root) && root.IsExpanded);
        }

        [Test]
        public void DuplicateIdsSharedNodesNullRootsAndFailingEnumerationLeaveOldProjectionUntouched()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            f.List.TryGetItemToken(0, out var token);
            Assert.Throws<ArgumentException>(() => new UGUIVirtualTreeNode(" ", null));
            Assert.Throws<ArgumentException>(() => new UGUIVirtualTreeNode("bad", null, new UGUIVirtualTreeNode[] { null }));
            Assert.Throws<ArgumentException>(() => f.Tree.SetRoots(new[] { new UGUIVirtualTreeNode("same", 1), new UGUIVirtualTreeNode("same", 2) }));
            var shared = new UGUIVirtualTreeNode("shared", 1);
            Assert.Throws<ArgumentException>(() => f.Tree.SetRoots(new[] {
                new UGUIVirtualTreeNode("left", 1, new[] { shared }), new UGUIVirtualTreeNode("right", 2, new[] { shared }) }));
            Assert.Throws<ArgumentException>(() => f.Tree.SetRoots(new UGUIVirtualTreeNode[] { null }));
            Assert.Throws<InvalidOperationException>(() => f.Tree.SetRoots(FailingRoots()));
            Assert.IsTrue(token.IsCurrent);
            AssertIds(f.Tree, "a", "b");
            Assert.IsTrue(f.Tree.OwnsCurrentData);
        }

        [Test]
        public void PreserveExpansionUsesIdsAndExpansionStateIsNotStoredInSharedModel()
        {
            using var a = new Fixture();
            using var b = new Fixture();
            var roots = Forest();
            a.Tree.SetRoots(roots);
            b.Tree.SetRoots(roots);
            a.Tree.Expand("a");
            Assert.AreEqual(6, a.Tree.VisibleCount);
            Assert.AreEqual(2, b.Tree.VisibleCount);
            a.Tree.SetRoots(Forest(), true);
            Assert.AreEqual(6, a.Tree.VisibleCount);
            a.Tree.SetRoots(Forest(), false);
            Assert.AreEqual(2, a.Tree.VisibleCount);
        }

        [Test]
        public void DirectCoreMutationIsDetectedEvenWhenCountDidNotChange()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            f.List.UpdateData(0, f.Tree.VisibleItems[1]);
            Assert.IsFalse(f.Tree.OwnsCurrentData);
            Assert.Throws<InvalidOperationException>(() => f.Tree.Expand("a"));
            f.Tree.SetRoots(Forest());
            Assert.IsTrue(f.Tree.OwnsCurrentData);
            f.List.AddData(f.Tree.VisibleItems[0]);
            Assert.Throws<InvalidOperationException>(() => f.Tree.Toggle("a"));
            f.Tree.SetRoots(null);
            Assert.AreEqual(0, f.List.Count);
            Assert.IsTrue(f.Tree.OwnsCurrentData);
        }

        [Test]
        public void RendererFailureAfterDataCommitKeepsProjectionAndArrowRecoverable()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            f.BindCallback = item => { if (item.Id == "a/leaf") throw new InvalidOperationException("预期的树行绑定错误"); };
            Assert.Throws<InvalidOperationException>(() => f.Tree.Expand("a"));
            Assert.AreEqual(6, f.Tree.VisibleCount);
            Assert.AreEqual(6, f.List.Count);
            Assert.IsTrue(f.Tree.OwnsCurrentData);
            f.BindCallback = null;
            f.List.RefreshLayout();
            Assert.IsTrue(f.View(0).RenderedExpanded);
            Assert.AreEqual("a/leaf", f.View(1).Item.Id);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MeasurementFailureAfterExpansionCanRetryWithoutRebindingData(bool invalidSize)
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            f.MeasureCallback = item =>
            {
                if (item.Id != "a/leaf") return null;
                if (invalidSize) return new Vector2(float.NaN, 0);
                throw new InvalidOperationException("预期的树行测量错误");
            };
            Assert.Throws<InvalidOperationException>(() => f.Tree.Expand("a"));
            Assert.AreEqual(6, f.List.Count);
            Assert.IsTrue(f.Tree.OwnsCurrentData);
            int binds = f.View(1).Binds;
            f.MeasureCallback = null;
            f.List.RefreshLayout();
            Assert.AreEqual(30f, f.View(1).RectTransform.rect.height, 0.01f);
            Assert.AreEqual(binds, f.View(1).Binds);
            Assert.IsTrue(f.View(0).RenderedExpanded);
        }

        [Test]
        public void RecycleFailureBeforeCollapseCommitRollsBackTreeState()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            f.Tree.Expand("a");
            f.RecycleCallback = () => throw new InvalidOperationException("预期的树行回收错误");
            Assert.Throws<InvalidOperationException>(() => f.Tree.Collapse("a"));
            Assert.AreEqual(6, f.Tree.VisibleCount);
            Assert.AreEqual(6, f.List.Count);
            Assert.IsTrue(f.Tree.TryGetItem("a", out var item) && item.IsExpanded);
            Assert.IsTrue(f.Tree.OwnsCurrentData);
            f.RecycleCallback = null;
            f.List.RefreshLayout();
            Assert.IsTrue(f.View(0).RenderedExpanded);
            Assert.IsTrue(f.Tree.Collapse("a"));
            Assert.AreEqual(2, f.Tree.VisibleCount);
        }

        [Test]
        public void RecycleFailureDuringSetRootsRestoresThePreviousTree()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            f.RecycleCallback = () => throw new InvalidOperationException("预期的森林替换回收错误");
            Assert.Throws<InvalidOperationException>(() => f.Tree.SetRoots(new[] { new UGUIVirtualTreeNode("new", "New") }));
            AssertIds(f.Tree, "a", "b");
            Assert.AreEqual(2, f.List.Count);
            Assert.IsTrue(f.Tree.OwnsCurrentData);
            f.RecycleCallback = null;
            f.List.RefreshLayout();
        }

        [Test]
        public void ReentrantTreeChangeDuringBindIsRejectedAndUnknownOrLeafChangesAreNoOps()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            Assert.IsFalse(f.Tree.Expand("missing"));
            Assert.IsFalse(f.Tree.Collapse("b"));
            Assert.IsFalse(f.Tree.Expand("b"));
            Assert.IsFalse(f.Tree.ScrollToNode("missing"));
            f.BindCallback = item => f.Tree.Toggle("a");
            Assert.Throws<InvalidOperationException>(() => f.Tree.Expand("a"));
            f.BindCallback = null;
            f.List.RefreshLayout();
            Assert.IsTrue(f.Tree.OwnsCurrentData);
        }

        [Test]
        public void DisposeDoesNotDisposeOrClearUnderlyingList()
        {
            using var f = new Fixture();
            f.Tree.SetRoots(Forest());
            f.Tree.Dispose();
            Assert.AreEqual(0, f.Tree.VisibleCount);
            Assert.AreEqual(2, f.List.Count);
            Assert.IsFalse(f.List.IsDisposalComplete);
            Assert.Throws<ObjectDisposedException>(() => f.Tree.Expand("a"));
            f.List.Clear();
            Assert.AreEqual(0, f.List.Count);
        }

        [Test]
        public void DeepTreesUseIterativeTraversalAndLargeTreesRemainVirtual()
        {
            using var f = new Fixture();
            var root = new UGUIVirtualTreeNode("leaf", "Leaf");
            for (int i = 1999; i >= 0; i--) root = new UGUIVirtualTreeNode("n-" + i, i, new[] { root }, true);
            f.Tree.SetRoots(new[] { root });
            Assert.AreEqual(2001, f.Tree.TotalNodeCount);
            Assert.AreEqual(2001, f.Tree.VisibleCount);
            Assert.Less(f.Created, 40);
            Assert.IsTrue(f.Tree.TryGetItem("leaf", out var leaf));
            Assert.AreEqual(2000, leaf.Depth);
            f.Tree.Collapse("n-0");
            Assert.AreEqual(1, f.Tree.VisibleCount);
            f.Tree.Expand("n-0");
            Assert.AreEqual(2001, f.Tree.VisibleCount);
        }

        private static List<UGUIVirtualTreeNode> Forest() => new List<UGUIVirtualTreeNode> {
            new UGUIVirtualTreeNode("a", "A", new[] {
                new UGUIVirtualTreeNode("a/leaf", "A leaf"),
                new UGUIVirtualTreeNode("a/branch", "A branch", new[] {
                    new UGUIVirtualTreeNode("a/branch/x", "X"), new UGUIVirtualTreeNode("a/branch/y", "Y") }, true) }),
            new UGUIVirtualTreeNode("b", "B") };
        private static IEnumerable<UGUIVirtualTreeNode> FailingRoots()
        {
            yield return new UGUIVirtualTreeNode("temp", "Temp");
            throw new InvalidOperationException("预期的节点枚举失败");
        }
        private static void AssertIds(UGUIVirtualTreeAdapter tree, params string[] expected)
        {
            Assert.AreEqual(expected.Length, tree.VisibleCount);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], tree.VisibleItems[i].Id);
                Assert.AreEqual(i, tree.GetVisibleIndex(expected[i]));
            }
        }

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualList List;
            public readonly UGUIVirtualTreeAdapter Tree;
            public readonly ScrollRect Scroll;
            public float Position => Scroll.content.anchoredPosition.y;
            public int Created;
            public Action<UGUIVirtualTreeItem> BindCallback;
            public Func<UGUIVirtualTreeItem, Vector2?> MeasureCallback;
            public Action RecycleCallback;
            public Fixture(bool variable = true)
            {
                Root = new GameObject("Tree test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                var scroll = Root.GetComponent<ScrollRect>();
                Scroll = scroll;
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                scroll.viewport = (RectTransform)viewport.transform;
                scroll.viewport.sizeDelta = new Vector2(300, 300);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                scroll.content = (RectTransform)content.transform;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                List = new UGUIVirtualList(scroll, new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 40),
                    variableSize: variable, spacing: new Vector2(0, 4)), this) { BufferSize = 100 };
                Tree = new UGUIVirtualTreeAdapter(List);
            }
            public View View(int index)
            {
                Assert.IsTrue(List.TryGetItem(index, out var item));
                return (View)item;
            }
            public float Visual(int index) => View(index).RectTransform.anchoredPosition.y + Scroll.content.anchoredPosition.y;
            public int GetItemType(int index, object data) => ((UGUIVirtualTreeItem)data).HasChildren ? 0 : 1;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                Created++;
                var go = new GameObject("Tree row", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go, this);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose() { BindCallback = null; MeasureCallback = null; RecycleCallback = null; Tree.Dispose(); List.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            private readonly Fixture _owner;
            public int Binds;
            public UGUIVirtualTreeItem Item;
            public object RenderedData;
            public bool RenderedExpanded;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject gameObject, Fixture owner) { GameObject = gameObject; _owner = owner; }
            public void Bind(int index, object data)
            {
                Item = (UGUIVirtualTreeItem)data;
                RenderedData = Item.Data;
                RenderedExpanded = Item.IsExpanded;
                Binds++;
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Item.HasChildren ? 40 : 30);
                _owner.BindCallback?.Invoke(Item);
            }
            public Vector2 Measure() => _owner.MeasureCallback?.Invoke(Item) ?? RectTransform.rect.size;
            public void OnRecycle() => _owner.RecycleCallback?.Invoke();
            public void SetClickHandler(Action callback) { }
        }
    }
}
