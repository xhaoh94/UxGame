using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualScrollNavigationTests
    {
        [TestCase(UGUIVirtualListAlignment.Start)]
        [TestCase(UGUIVirtualListAlignment.Center)]
        [TestCase(UGUIVirtualListAlignment.End)]
        public void ImmediateNavigationCorrectsAfterRealMeasurement(UGUIVirtualListAlignment alignment)
        {
            using var f = new Fixture();
            var nav = new UGUIVirtualScrollNavigator(f.Core);
            try
            {
                Assert.IsTrue(nav.ScrollTo(60, alignment));
                for (int i = 0; i < 30 && nav.IsActive; i++) nav.Tick();
                Assert.IsFalse(nav.IsActive);
                AssertAlignment(f, 60, alignment);
            }
            finally { nav.Dispose(); }
        }

        [Test]
        public void SmoothNavigationAdvancesOverTimeAndLandsOnAlignment()
        {
            using var f = new Fixture();
            var nav = new UGUIVirtualScrollNavigator(f.Core);
            int completed = 0;
            nav.Completed += () => completed++;
            try
            {
                Assert.IsTrue(nav.ScrollTo(80, UGUIVirtualListAlignment.Center, 0f, 0.2f));
                float before = f.Core.ScrollPositionValue;
                Assert.IsTrue(nav.Tick(0.05f));
                float after = f.Core.ScrollPositionValue;
                Assert.AreNotEqual(before, after, "平滑滚动应在本帧产生位移");
                for (int i = 0; i < 200 && nav.IsActive; i++) nav.Tick(0.05f);
                Assert.AreEqual(1, completed);
                AssertAlignment(f, 80, UGUIVirtualListAlignment.Center);
            }
            finally { nav.Dispose(); }
        }

        [Test]
        public void NewNavigationAndUserDragCancelPreviousTask()
        {
            using var f = new Fixture();
            var nav = new UGUIVirtualScrollNavigator(f.Core);
            int cancelled = 0;
            nav.Cancelled += () => cancelled++;
            try
            {
                Assert.IsTrue(nav.ScrollTo(80, UGUIVirtualListAlignment.Start, 0f, 0.3f));
                Assert.IsTrue(nav.ScrollTo(20, UGUIVirtualListAlignment.End, 0f, 0.3f));
                Assert.AreEqual(1, cancelled, "重复请求取消旧任务");
                Assert.AreEqual(20, nav.Target.Index);
                f.Scroll.OnBeginDrag(new UnityEngine.EventSystems.PointerEventData(
                    new GameObject("Nav EventSystem", typeof(UnityEngine.EventSystems.EventSystem)).GetComponent<UnityEngine.EventSystems.EventSystem>()));
                Assert.IsFalse(nav.IsActive, "用户拖拽取消导航");
                Assert.AreEqual(2, cancelled);
                Assert.IsFalse(nav.Tick());
            }
            finally { nav.Dispose(); }
        }

        [Test]
        public void DataChangeAndReleaseCancelAndDisposedNavigatorCannotStart()
        {
            using var f = new Fixture();
            var nav = new UGUIVirtualScrollNavigator(f.Core);
            int cancelled = 0;
            nav.Cancelled += () => cancelled++;
            Assert.IsTrue(nav.ScrollTo(80, UGUIVirtualListAlignment.Center, 0f, 0.5f));
            f.Core.AddData(f.Data[0]);
            Assert.IsFalse(nav.IsActive);
            Assert.AreEqual(1, cancelled);
            nav.Dispose();
            Assert.IsFalse(nav.ScrollTo(0, UGUIVirtualListAlignment.Start));
            f.Core.Dispose();
            Assert.IsFalse(nav.Tick());
        }

        [Test]
        public void NearestLeavesVisibleTargetAloneAndHonorsPadding()
        {
            using var f = new Fixture();
            var nav = new UGUIVirtualScrollNavigator(f.Core);
            try
            {
                nav.ScrollTo(10, UGUIVirtualListAlignment.Start, 0f, 0f);
                for (int i = 0; i < 30 && nav.IsActive; i++) nav.Tick();
                float position = f.Core.ScrollPositionValue;
                Assert.IsTrue(f.Core.TryGetItem(10, out _));
                nav.ScrollTo(10, UGUIVirtualListAlignment.Nearest, 0f, 0f);
                for (int i = 0; i < 30 && nav.IsActive; i++) nav.Tick();
                Assert.AreEqual(position, f.Core.ScrollPositionValue, 0.5f);
                nav.ScrollTo(30, UGUIVirtualListAlignment.Start, 12f, 0f);
                for (int i = 0; i < 30 && nav.IsActive; i++) nav.Tick();
                Assert.AreEqual(-f.Core.GetItemLayout(30).Position.y - 12, f.Core.ScrollPositionValue, 0.5f);
            }
            finally { nav.Dispose(); }
        }

        [Test]
        public void RapidTickDuringRendererCallbackCannotRunConcurrently()
        {
            using var f = new Fixture();
            var nav = new UGUIVirtualScrollNavigator(f.Core);
            try
            {
                int inner = 0;
                f.BindCallback = () => { if (inner++ == 0 && nav.IsActive) nav.Tick(0.05f); };
                Assert.IsTrue(nav.ScrollTo(50, UGUIVirtualListAlignment.Center, 0f, 0.2f));
                for (int i = 0; i < 40 && nav.IsActive; i++) nav.Tick(0.05f);
                Assert.IsFalse(nav.IsActive);
            }
            finally { nav.Dispose(); }
        }

        [TestCase(-1)]
        [TestCase(float.NaN)]
        public void InvalidDurationIsRejectedWithoutChangingCurrentTask(float duration)
        {
            using var f = new Fixture();
            var nav = new UGUIVirtualScrollNavigator(f.Core);
            try
            {
                nav.ScrollTo(10, UGUIVirtualListAlignment.Start, 0f, 0.2f);
                Assert.Throws<ArgumentOutOfRangeException>(() => nav.ScrollTo(20, UGUIVirtualListAlignment.Start, 0f, duration));
                Assert.IsTrue(nav.IsActive);
                Assert.AreEqual(10, nav.Target.Index);
                Assert.Throws<ArgumentOutOfRangeException>(() => nav.ScrollTo(1000, UGUIVirtualListAlignment.Start));
                var badCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, float.PositiveInfinity));
                try { nav.ScrollTo(20, UGUIVirtualListAlignment.Start, 0f, 0.2f, badCurve); }
                catch (ArgumentException) { /* Unity 保留异常值时应在此拒绝。 */ }
                for (int i = 0; i < 40 && nav.IsActive; i++) nav.Tick(0.05f);
                Assert.IsFalse(float.IsNaN(f.Core.ScrollPositionValue) || float.IsInfinity(f.Core.ScrollPositionValue),
                    "无效曲线不能把滚动位置写成 NaN/Infinity");
            }
            finally { nav.Dispose(); }
        }

        [Test]
        public void HorizontalListRejectsSmoothNavigatorExplicitly()
        {
            using var f = new Fixture(true);
            Assert.Throws<NotSupportedException>(() => new UGUIVirtualScrollNavigator(f.Core));
        }

        private static void AssertAlignment(Fixture f, int index, UGUIVirtualListAlignment alignment)
        {
            var layout = f.Core.GetItemLayout(index);
            float start = -layout.Position.y;
            float length = layout.Size.y;
            float viewport = f.Core.ViewportSize.y;
            float expected = alignment == UGUIVirtualListAlignment.Start ? start
                : alignment == UGUIVirtualListAlignment.Center ? start - (viewport - length) * 0.5f
                : start + length - viewport;
            float max = Mathf.Max(0f, f.Core.ContentLength - viewport);
            expected = Mathf.Clamp(expected, 0f, max);
            Assert.AreEqual(expected, f.Core.ScrollPositionValue, 0.6f, $"{alignment} 应在测量后仍对齐");
        }

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly ScrollRect Scroll;
            public readonly UGUIVirtualList Core;
            public readonly object[] Data;
            public Action BindCallback;
            public Fixture(bool horizontal = false)
            {
                Root = new GameObject("Navigation test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                Scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                Scroll.viewport = (RectTransform)viewport.transform;
                Scroll.viewport.sizeDelta = new Vector2(300, 300);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(Root.transform.parent ?? Root.transform, false);
                content.transform.SetParent(viewport.transform, false);
                Scroll.content = (RectTransform)content.transform;
                Scroll.horizontal = horizontal;
                Scroll.vertical = !horizontal;
                Scroll.movementType = ScrollRect.MovementType.Clamped;
                Core = new UGUIVirtualList(Scroll,
                    new UGUIFlowLayout(horizontal ? UGUIVirtualListAxis.Horizontal : UGUIVirtualListAxis.Vertical,
                        new Vector2(100, 100), variableSize: true), this) { BufferSize = 200 };
                Data = new object[200];
                for (int i = 0; i < Data.Length; i++) Data[i] = i % 3 == 0 ? (object)220f : i % 3 == 1 ? 60f : 140f;
                Core.SetDatas(Data);
            }
            public int GetItemType(int index, object data) => 0;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Nav item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                rect.sizeDelta = new Vector2(100, 100);
                return new View(go, this);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose() { BindCallback = null; Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
        private sealed class View : IUGUIVirtualListItem
        {
            private readonly Fixture _owner;
            private float _value;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject go, Fixture owner) { GameObject = go; _owner = owner; }
            public void Bind(int index, object data)
            {
                _value = (float)data;
                RectTransform.SetSizeWithCurrentAnchors(_owner.Scroll.vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, _value);
                _owner.BindCallback?.Invoke();
            }
            public Vector2 Measure() => RectTransform.rect.size;
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }
}
