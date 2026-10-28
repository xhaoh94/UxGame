using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualStructureEditTests
    {
        [Test]
        public void TreeInsertRemoveAndMoveKeepUnaffectedRowsBound()
        {
            using var f = new TreeFixture();
            f.Tree.SetRoots(new[]
            {
                new UGUIVirtualTreeNode("a", "A", new[] { new UGUIVirtualTreeNode("a1", "A1") }),
                new UGUIVirtualTreeNode("b", "B")
            });
            f.Tree.Expand("a");
            var a = f.View(0);
            var a1 = f.View(1);
            Assert.IsTrue(f.Tree.AddChild("a", new UGUIVirtualTreeNode("a2", "A2")));
            Assert.AreEqual(4, f.Core.Count);
            Assert.AreSame(a, f.View(0));
            Assert.AreSame(a1, f.View(1));
            Assert.AreEqual("a2", ((UGUIVirtualTreeItem)f.Core.GetData(2)).Id);

            Assert.IsTrue(f.Tree.RemoveNode("a1"));
            Assert.AreEqual(3, f.Core.Count);
            Assert.AreEqual(1, a1.Binds, "编辑后只应重绑受影响行");
            Assert.AreEqual("a2", ((UGUIVirtualTreeItem)f.Core.GetData(1)).Id);

            Assert.IsTrue(f.Tree.MoveNode("a2", null, 0));
            Assert.AreEqual("a2", ((UGUIVirtualTreeItem)f.Core.GetData(0)).Id);
            Assert.AreEqual("a", ((UGUIVirtualTreeItem)f.Core.GetData(1)).Id);
            Assert.AreEqual(0, ((UGUIVirtualTreeItem)f.Core.GetData(0)).Depth);
            Assert.IsTrue(f.Tree.TryGetItem("a2", out var moved));
            Assert.IsNull(moved.Parent);
        }

        [Test]
        public void TreeRejectsCyclesDuplicateIdsAndUnknownParents()
        {
            using var f = new TreeFixture();
            f.Tree.SetRoots(new[]
            {
                new UGUIVirtualTreeNode("a", "A", new[] { new UGUIVirtualTreeNode("a1", "A1") }),
                new UGUIVirtualTreeNode("b", "B")
            });
            Assert.IsFalse(f.Tree.MoveNode("a", "a1", 0), "不能移动到自己的子孙下");
            Assert.IsFalse(f.Tree.InsertChild("missing", 0, new UGUIVirtualTreeNode("x", "X")));
            Assert.Throws<ArgumentException>(() => f.Tree.AddRoot(new UGUIVirtualTreeNode("a", "Duplicate")));
            Assert.IsTrue(f.Tree.OwnsCurrentData);
            Assert.AreEqual(2, f.Core.Count, "未展开时只显示两个根节点");
        }

        [Test]
        public void GroupInsertRemoveMoveAndItemEditsKeepStickyDataConsistent()
        {
            using var f = new GroupFixture();
            f.Groups.SetGroups(new[]
            {
                UGUIVirtualGroup.Create("g1", "G1", new[] { "a", "b" }),
                UGUIVirtualGroup.Create("g2", "G2", new[] { "c" })
            });
            var g2Header = f.View(3);
            Assert.IsTrue(f.Groups.InsertGroup(1, UGUIVirtualGroup.Create("mid", "MID", new[] { "m" })));
            Assert.AreEqual(7, f.Core.Count);
            Assert.AreEqual("MID", ((UGUIVirtualGroupItem)f.Core.GetData(3)).Data);
            Assert.AreSame(g2Header, f.View(5), "被移动的组头应复用原行实例");

            Assert.IsTrue(f.Groups.InsertItem("g1", 1, "ab"));
            Assert.AreEqual(6, f.Groups.GetHeaderIndex("g2"));
            Assert.IsTrue(f.Groups.RemoveItem("g1", 0));
            Assert.AreEqual(5, f.Groups.GetHeaderIndex("g2"));
            Assert.IsTrue(f.Groups.RemoveGroup("mid"));
            Assert.AreEqual(3, f.Groups.GetHeaderIndex("g2"));

            Assert.IsTrue(f.Groups.MoveGroup("g2", 0));
            Assert.AreEqual(0, f.Groups.GetHeaderIndex("g2"));
            Assert.AreEqual(2, f.Groups.GetHeaderIndex("g1"));
            Assert.IsTrue(f.Groups.OwnsCurrentData);
            f.Groups.ScrollToGroup("g2");
            f.Sticky.Tick();
            Assert.AreEqual("g2", f.Sticky.CurrentGroupId);
        }

        [Test]
        public void GroupRejectsDuplicateAndOutOfRangeEditsWithoutChangingRows()
        {
            using var f = new GroupFixture();
            f.Groups.SetGroups(new[] { UGUIVirtualGroup.Create("g", "G", new[] { "a" }), UGUIVirtualGroup.Create("h", "H", new[] { "b" }) });
            Assert.Throws<ArgumentException>(() => f.Groups.AddGroup(new UGUIVirtualGroup("g", "Dup")));
            Assert.Throws<ArgumentOutOfRangeException>(() => f.Groups.InsertItem("g", 5, "x"));
            Assert.Throws<ArgumentOutOfRangeException>(() => f.Groups.RemoveItem("g", -1));
            Assert.IsFalse(f.Groups.RemoveGroup("missing"));
            Assert.AreEqual(4, f.Core.Count);
            Assert.IsTrue(f.Groups.OwnsCurrentData);
        }

        private sealed class TreeFixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualTreeAdapter Tree;
            public TreeFixture()
            {
                Root = new GameObject("Tree edit test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                Core = MakeCore(Root, out _);
                Tree = new UGUIVirtualTreeAdapter(Core);
            }
            public View View(int index) { Assert.IsTrue(Core.TryGetItem(index, out var item)); return (View)item; }
            public void Dispose() { Tree.Dispose(); Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }

        private sealed class GroupFixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualGroupAdapter Groups;
            public readonly UGUIVirtualStickyHeader Sticky;
            public GroupFixture()
            {
                Root = new GameObject("Group edit test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                Core = MakeCore(Root, out var factory);
                Groups = new UGUIVirtualGroupAdapter(Core);
                Sticky = new UGUIVirtualStickyHeader(Core, Groups, factory);
            }
            public View View(int index) { Assert.IsTrue(Core.TryGetItem(index, out var item)); return (View)item; }
            public void Dispose() { Sticky.Dispose(); Groups.Dispose(); Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }

        private static UGUIVirtualList MakeCore(GameObject root, out IUGUIVirtualListItemFactory factory)
        {
            var scroll = root.GetComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(root.transform, false);
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.viewport.sizeDelta = new Vector2(300, 200);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            scroll.content = (RectTransform)content.transform;
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var f = new Factory();
            factory = f;
            return new UGUIVirtualList(scroll,
                new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 40), variableSize: true, spacing: new Vector2(0, 2)),
                f) { BufferSize = 100 };
        }

        private sealed class Factory : IUGUIVirtualListItemFactory
        {
            public int GetItemType(int index, object data)
            {
                if (data is UGUIVirtualTreeItem tree) return tree.HasChildren ? 0 : 1;
                var group = (UGUIVirtualGroupItem)data;
                return group.IsHeader ? 0 : 1;
            }
            public IUGUIVirtualListItem Create(int type, Transform parent)
            {
                var go = new GameObject("Row", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
        }

        private sealed class View : IUGUIVirtualListItem
        {
            public int Binds;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go) { GameObject = go; }
            public void Bind(int index, object data)
            {
                Binds++;
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);
            }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }
}
