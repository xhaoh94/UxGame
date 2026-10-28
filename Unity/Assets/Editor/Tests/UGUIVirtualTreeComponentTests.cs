using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualTreeComponentTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void InspectorTreeModeOwnsAdapterAndClickPolicyRunsBeforeBusinessEvent(bool automatic)
        {
            using var f = new Fixture();
            Set(f.Component, "_treeToggleOnClick", automatic);
            f.Component.SetTreeRoots(Roots());
            int countAtEvent = -1;
            bool expandedAtEvent = false;
            f.Component.ItemClicked += (index, data) =>
            {
                countAtEvent = f.Component.Count;
                expandedAtEvent = ((UGUIVirtualTreeItem)data).IsExpanded;
            };
            f.View(0).Click();
            Assert.AreEqual(automatic ? 2 : 1, f.Component.Count);
            Assert.AreEqual(automatic ? 2 : 1, countAtEvent);
            Assert.AreEqual(automatic, expandedAtEvent);
            if (!automatic) Assert.IsTrue(f.Component.ExpandTreeNode("root"));
            Assert.IsTrue(f.Component.TryGetTreeItem("root", out var root) && root.IsExpanded);
            Assert.AreSame(f.Tree, f.Component.TreeAdapter);
        }

        [Test]
        public void TreeFacadeSupportsIdNavigationUpdatesAndClearWithoutLosingOwnership()
        {
            using var f = new Fixture();
            f.Component.SetTreeRoots(Roots());
            Assert.IsTrue(f.Component.ScrollToTreeNode("root/leaf"));
            Assert.AreEqual(1, f.Component.GetTreeVisibleIndex("root/leaf"));
            Assert.IsTrue(f.Component.UpdateTreeNodeData("root/leaf", "Updated"));
            Assert.AreEqual("Updated", ((UGUIVirtualTreeItem)f.Component.GetData(1)).Data);
            Assert.IsTrue(f.Component.RefreshTreeNode("root/leaf"));
            f.Component.Clear();
            Assert.AreEqual(0, f.Component.Count);
            Assert.IsTrue(f.Component.TreeAdapter.OwnsCurrentData);
        }

        [Test]
        public void FlatMutationMethodsCannotCorruptComponentOwnedTree()
        {
            using var f = new Fixture();
            f.Component.SetTreeRoots(Roots());
            Assert.Throws<InvalidOperationException>(() => f.Component.SetDatas(new[] { "bad" }));
            Assert.Throws<InvalidOperationException>(() => f.Component.AddData("bad"));
            Assert.Throws<InvalidOperationException>(() => f.Component.RemoveAt(0));
            Assert.Throws<InvalidOperationException>(() => f.Component.UpdateData(0, "bad"));
            Assert.AreEqual(1, f.Component.Count);
            Assert.IsTrue(f.Component.TreeAdapter.OwnsCurrentData);
        }

        [Test]
        public void ReleaseDisposesOwnedTreeAndLateNodeCallsDoNotInitializeAgain()
        {
            using var f = new Fixture();
            f.Component.SetTreeRoots(Roots());
            var adapter = f.Component.TreeAdapter;
            f.Component.Release();
            Assert.IsFalse(f.Component.IsInitialized);
            Assert.IsFalse(f.Component.TryGetTreeAdapter(out _));
            Assert.AreEqual(0, adapter.VisibleCount);
            Assert.IsFalse(f.Component.ExpandTreeNode("root"));
            Assert.IsFalse(f.Component.UpdateTreeNodeData("root", "late"));
            Assert.IsFalse(f.Component.ScrollToTreeNode("root"));
            Assert.IsFalse(f.Component.IsInitialized);
            Assert.Throws<ObjectDisposedException>(() => adapter.Expand("root"));
        }

        [Test]
        public void TreeLayoutIgnoresInactiveMultiAxisLimitAndSpacingSettings()
        {
            using var f = new Fixture();
            Set(f.Component, "_axis", (UGUIVirtualListAxis)999);
            Set(f.Component, "_allowMultiple", true);
            Set(f.Component, "_limitGridCount", true);
            Set(f.Component, "_gridCount", -1);
            Set(f.Component, "_gridSpacing", new Vector2(float.NaN, float.NaN));
            var flow = (UGUIFlowLayout)typeof(UGUIVirtualListComponent)
                .GetMethod("CreateLayout", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(f.Component, new object[] { new Vector2(100, 40) });
            flow.SetViewportSize(new Vector2(1000, 1000));
            Assert.IsTrue(flow.IsVertical);
            Assert.AreEqual(1, flow.CountPerLine);
            Assert.IsFalse(f.Component.AllowMultiple);
        }

        [Test]
        public void ExplicitTypeSwitchRequiresReleaseWhenDataExists()
        {
            using var f = new Fixture(UGUIVirtualListType.Flat);
            f.Component.SetDatas(new[] { "flat" });
            Assert.Throws<InvalidOperationException>(() => f.Component.ConfigureListType(UGUIVirtualListType.Tree));
            Assert.AreEqual(UGUIVirtualListType.Flat, f.Component.ListType);
            f.Component.Release();
            f.Component.ConfigureListType(UGUIVirtualListType.Tree);
            Assert.AreEqual(UGUIVirtualListType.Tree, f.Component.ListType);
            Assert.IsTrue(f.Component.IsListTypeConfigured);
            Assert.IsFalse(f.Component.IsInitialized);
        }

        [Test]
        public void EmptyLegacyWidgetCanChangeTypeWithoutLeavingOldCoreAlive()
        {
            using var f = new Fixture(UGUIVirtualListType.Flat);
            f.Component.ConfigureListType(UGUIVirtualListType.Tree);
            Assert.IsTrue(f.Core.IsDisposalComplete);
            Assert.IsFalse(f.Component.IsInitialized);
            Assert.AreEqual(UGUIVirtualListType.Tree, f.Component.ListType);
        }

        [Test]
        public void ClosingWidgetWhileAutomaticToggleRendersDoesNotEmitStaleBusinessClick()
        {
            using var f = new Fixture();
            f.Component.SetTreeRoots(Roots());
            int clicks = 0;
            f.Component.ItemClicked += (index, data) => clicks++;
            f.BindCallback = data => { if (data is UGUIVirtualTreeItem item && item.IsExpanded) f.Component.Release(); };
            f.View(0).Click();
            Assert.AreEqual(0, clicks);
            Assert.IsFalse(f.Component.IsInitialized);
            Assert.IsTrue(f.Core.IsDisposalComplete);
        }

        [TestCase(true, false, UGUIVirtualListType.Tree)]
        [TestCase(false, false, UGUIVirtualListType.Flat)]
        [TestCase(true, true, UGUIVirtualListType.Flat)]
        public void InspectorMigratesOnlyUnconfiguredLegacyTreeDemo(bool demoEnabled, bool configured, UGUIVirtualListType expected)
        {
            var root = new GameObject("Tree inspector schema test");
            UnityEditor.Editor inspector = null;
            try
            {
                var component = root.AddComponent<UGUIVirtualListComponent>();
                var demo = root.AddComponent<Ux.UI.UGUIVirtualTreeDemo>();
                demo.enabled = demoEnabled;
                Set(component, "_listType", UGUIVirtualListType.Flat);
                Set(component, "_listTypeConfigured", configured);
                inspector = UnityEditor.Editor.CreateEditor(component, typeof(UGUIVirtualListComponentEditor));
                Assert.AreEqual(expected, component.ListType);
                Assert.IsTrue(component.IsListTypeConfigured);
            }
            finally
            {
                if (inspector != null) UnityEngine.Object.DestroyImmediate(inspector);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static UGUIVirtualTreeNode[] Roots() => new[] {
            new UGUIVirtualTreeNode("root", "Root", new[] { new UGUIVirtualTreeNode("root/leaf", "Leaf") }) };

        private static void Set(object target, string name, object value) => typeof(UGUIVirtualListComponent)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualListComponent Component;
            public readonly UGUIVirtualTreeAdapter Tree;
            public Action<object> BindCallback;
            public Fixture(UGUIVirtualListType type = UGUIVirtualListType.Tree)
            {
                Root = new GameObject("Tree component test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
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
                scroll.movementType = ScrollRect.MovementType.Clamped;
                var layout = new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 40));
                Core = new UGUIVirtualList(scroll, layout, this);
                Component = Root.AddComponent<UGUIVirtualListComponent>();
                Set(Component, "_listType", type);
                Set(Component, "_listTypeConfigured", true);
                Set(Component, "_list", Core);
                Set(Component, "_flowLayout", layout);
                if (type == UGUIVirtualListType.Tree)
                {
                    Tree = new UGUIVirtualTreeAdapter(Core);
                    Set(Component, "_treeAdapter", Tree);
                    Tree.SetRoots(null);
                }
                var forward = typeof(UGUIVirtualListComponent).GetMethod("ForwardClick", BindingFlags.Instance | BindingFlags.NonPublic);
                Core.ItemClicked += (Action<int, object>)Delegate.CreateDelegate(typeof(Action<int, object>), Component, forward);
            }
            public View View(int index)
            {
                Assert.IsTrue(Core.TryGetItem(index, out var item));
                return (View)item;
            }
            public int GetItemType(int index, object data) => data is UGUIVirtualTreeItem item && item.HasChildren ? 0 : 1;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Tree component item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go, this);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose()
            {
                BindCallback = null;
                Component.Release();
                Core.Dispose();
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            private readonly Fixture _owner;
            private Action _click;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go, Fixture owner) { GameObject = go; _owner = owner; }
            public void Bind(int index, object data) => _owner.BindCallback?.Invoke(data);
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { _click = callback; }
            public void Click() => _click?.Invoke();
        }
    }
}
