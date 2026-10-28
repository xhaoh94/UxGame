using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualGroupComponentTests
    {
        [Test]
        public void FacadeOwnsGroupDataAndRejectsFlatWrites()
        {
            using var f = new Fixture();
            f.Component.SetGroups(Groups());
            Assert.AreEqual(1, f.Component.GroupAdapter.GroupCount);
            Assert.AreEqual(20, f.Component.GroupAdapter.DataCount);
            Assert.Throws<InvalidOperationException>(() => f.Component.SetDatas(new[] { "bad" }));
            Assert.Throws<InvalidOperationException>(() => f.Component.AddData("bad"));
            Assert.IsTrue(f.Component.UpdateGroupHeader("g", "Changed"));
            Assert.IsTrue(f.Component.UpdateGroupItem("g", 1, "Item changed"));
            f.Component.Clear();
            Assert.AreEqual(0, f.Component.Count);
            Assert.IsTrue(f.Component.GroupAdapter.OwnsCurrentData);
        }

        [Test]
        public void ReleaseDuringStickyRenderProtectsDeferredCleanupFromReinitialization()
        {
            using var f = new Fixture();
            f.Component.SetGroups(Groups());
            f.Core.ScrollToView(0, UGUIVirtualListAlignment.Start);
            int attempts = 0;
            f.StickyRecycle = () =>
            {
                attempts++;
                var error = Assert.Throws<InvalidOperationException>(() => f.Component.SetGroups(Groups()));
                StringAssert.Contains("释放", error.Message);
            };
            f.StickyBind = f.Component.Release;
            f.Sticky.Tick();
            Assert.Greater(attempts, 0);
            Assert.IsTrue(f.Sticky.IsDisposalComplete);
            Assert.IsTrue(f.Core.IsDisposalComplete);
            Assert.IsFalse(f.Component.IsInitialized);
            Assert.IsFalse(f.Component.TryGetStickyHeaderToken(out _));
            Assert.IsFalse(f.Component.UpdateGroupHeader("g", "late"));
        }

        [Test]
        public void GroupedLayoutIgnoresHiddenGridAndHorizontalConfiguration()
        {
            using var f = new Fixture();
            Set(f.Component, "_axis", (UGUIVirtualListAxis)123);
            Set(f.Component, "_allowMultiple", true);
            Set(f.Component, "_limitGridCount", true);
            Set(f.Component, "_gridCount", -1);
            Set(f.Component, "_gridSpacing", new Vector2(float.NaN, float.NaN));
            var flow = (UGUIFlowLayout)typeof(UGUIVirtualListComponent)
                .GetMethod("CreateLayout", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(f.Component, new object[] { new Vector2(100, 60) });
            flow.SetViewportSize(new Vector2(1000, 1000));
            Assert.IsTrue(flow.IsVertical);
            Assert.AreEqual(1, flow.CountPerLine);
            Assert.IsFalse(f.Component.AllowMultiple);
        }
        private static UGUIVirtualGroup[] Groups() => new[] { UGUIVirtualGroup.Create("g", "Header", new string[20]) };
        private static void Set(object owner, string name, object value) => typeof(UGUIVirtualListComponent)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly UGUIVirtualList Core;
            public readonly UGUIVirtualListComponent Component;
            public readonly UGUIVirtualGroupAdapter Groups;
            public readonly UGUIVirtualStickyHeader Sticky;
            public Action StickyBind;
            public Action StickyRecycle;
            public Fixture()
            {
                Root = new GameObject("Group component test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                var scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform)); viewport.transform.SetParent(Root.transform, false);
                scroll.viewport = (RectTransform)viewport.transform; scroll.viewport.sizeDelta = new Vector2(300, 120);
                var content = new GameObject("Content", typeof(RectTransform)); content.transform.SetParent(viewport.transform, false);
                scroll.content = (RectTransform)content.transform; scroll.vertical = true; scroll.horizontal = false;
                var layout = new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 60), paddingStart: 12);
                Core = new UGUIVirtualList(scroll, layout, new Factory(this, false));
                Groups = new UGUIVirtualGroupAdapter(Core); Groups.SetGroups(null);
                Sticky = new UGUIVirtualStickyHeader(Core, Groups, new Factory(this, true));
                Component = Root.AddComponent<UGUIVirtualListComponent>();
                Set(Component, "_listType", UGUIVirtualListType.Grouped); Set(Component, "_listTypeConfigured", true);
                Set(Component, "_list", Core); Set(Component, "_flowLayout", layout); Set(Component, "_groupAdapter", Groups); Set(Component, "_stickyHeader", Sticky);
            }
            public void Dispose()
            {
                StickyBind = null; StickyRecycle = null;
                Component.Release(); Sticky.Dispose(); Groups.Dispose(); Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root);
            }
        }
        private sealed class Factory : IUGUIVirtualListItemFactory
        {
            private readonly Fixture _owner;
            private readonly bool _sticky;
            public Factory(Fixture owner, bool sticky) { _owner = owner; _sticky = sticky; }
            public int GetItemType(int index, object data) => 0;
            public IUGUIVirtualListItem Create(int type, Transform parent)
            {
                var root = new GameObject("Component group row", typeof(RectTransform)); root.transform.SetParent(parent, false);
                return new View(root, _owner, _sticky);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
        }
        private sealed class View : IUGUIVirtualListItem
        {
            private readonly Fixture _owner;
            private readonly bool _sticky;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject root, Fixture owner, bool sticky) { GameObject = root; _owner = owner; _sticky = sticky; }
            public void Bind(int index, object data) { if (_sticky) _owner.StickyBind?.Invoke(); }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { if (_sticky) _owner.StickyRecycle?.Invoke(); }
            public void SetClickHandler(Action callback) { }
        }
    }
}
