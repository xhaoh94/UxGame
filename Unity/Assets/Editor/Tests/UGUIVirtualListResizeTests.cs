using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualListResizeTests
    {
        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void ResizingBufferedItemPreservesPartiallyVisibleAnchorWithoutRebinding(UGUIVirtualListAxis axis)
        {
            using var f = new Fixture(axis);
            f.ShowMiddle();
            f.SetPosition(2035f);
            var anchor = f.View(20);
            float before = f.VisualStart(20);
            int bindCount = anchor.Binds;
            int resizedBinds = f.View(19).Binds;
            f.SetSize(19, 180f);
            Assert.IsTrue(f.List.NotifyItemSizeChanged(19));
            Assert.AreEqual(before, f.VisualStart(20), 0.01f);
            Assert.AreEqual(2115f, f.Position, 0.01f);
            Assert.AreEqual(bindCount, anchor.Binds);
            Assert.AreEqual(resizedBinds, f.View(19).Binds);
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void BatchNotificationUsesOneAnchorAndDoesNotRebindExistingViews(UGUIVirtualListAxis axis)
        {
            using var f = new Fixture(axis);
            f.ShowMiddle();
            float before = f.VisualStart(20);
            int binds = f.View(20).Binds;
            f.SetSize(18, 150f);
            f.SetSize(19, 180f);
            Assert.Greater(f.List.NotifyVisibleItemSizesChanged(), 0);
            Assert.AreEqual(2130f, f.Position, 0.01f);
            Assert.AreEqual(before, f.VisualStart(20), 0.01f);
            Assert.AreEqual(binds, f.View(20).Binds);
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void ViewportStartingInSpacingAnchorsTheNextActuallyVisibleItem(UGUIVirtualListAxis axis)
        {
            using var f = new Fixture(axis, spacing: 20);
            f.ShowMiddle();
            f.SetPosition(2390f);
            Assert.AreEqual(20, f.List.FirstVisibleIndex);
            float before = f.VisualStart(20);
            f.SetSize(19, 180f);
            f.List.NotifyItemSizeChanged(19);
            Assert.AreEqual(before, f.VisualStart(20), 0.01f);
            Assert.AreEqual(2470f, f.Position, 0.01f);
        }

        [Test]
        public void ItemsAfterAnchorDoNotShiftViewportAndPreserveCanBeDisabled()
        {
            using var f = new Fixture(UGUIVirtualListAxis.Vertical);
            f.ShowMiddle();
            f.SetSize(23, 200f);
            f.List.NotifyItemSizeChanged(23);
            Assert.AreEqual(2000f, f.Position, 0.01f);
            f.List.PreserveScrollAnchor = false;
            f.SetSize(19, 180f);
            f.List.NotifyItemSizeChanged(19);
            Assert.AreEqual(2000f, f.Position, 0.01f);
            Assert.AreEqual(-80f, -f.VisualStart(20), 0.01f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void KeepingEndIsOptionalAndDoesNotPullHistoryToEnd(bool keepEnd)
        {
            using var f = new Fixture(UGUIVirtualListAxis.Vertical);
            f.List.KeepEndOnSizeChange = keepEnd;
            f.List.ScrollToView(99, UGUIVirtualListAlignment.End);
            f.SetSize(98, 200f);
            f.List.NotifyItemSizeChanged(98);
            Assert.AreEqual(keepEnd ? 9800f : 9700f, f.Position, 0.01f);
            f.ShowMiddle();
            f.SetSize(19, 150f);
            f.List.NotifyItemSizeChanged(19);
            Assert.AreEqual(2050f, f.Position, 0.01f);
        }

        [Test]
        public void ShrinkingNearEndClampsBoundsAndKeepsLastItemVisible()
        {
            using var f = new Fixture(UGUIVirtualListAxis.Vertical);
            f.List.ScrollToView(99, UGUIVirtualListAlignment.End);
            f.SetSize(98, 1f);
            f.List.NotifyItemSizeChanged(98);
            Assert.AreEqual(9601f, f.Position, 0.01f);
            Assert.IsTrue(f.List.TryGetItem(99, out _));
        }

        [Test]
        public void ElasticOverscrollDistanceSurvivesSizeChanges()
        {
            using var f = new Fixture(UGUIVirtualListAxis.Vertical);
            f.Scroll.movementType = ScrollRect.MovementType.Elastic;
            f.SetPosition(-30f);
            f.SetSize(0, 150f);
            f.List.NotifyItemSizeChanged(0);
            Assert.AreEqual(-30f, f.Position, 0.01f);
            f.List.ScrollToView(99, UGUIVirtualListAlignment.End);
            float oldMax = f.Layout.ContentLength - 300;
            f.SetPosition(oldMax + 40f);
            f.SetSize(98, 200f);
            f.List.NotifyItemSizeChanged(98);
            Assert.AreEqual(f.Layout.ContentLength - 300 + 40, f.Position, 0.01f);
        }

        [Test]
        public void TokensRejectRecyclingRefreshDataReplacementWrongOwnerAndDisposal()
        {
            using var f = new Fixture(UGUIVirtualListAxis.Vertical);
            using var other = new Fixture(UGUIVirtualListAxis.Vertical);
            Assert.IsTrue(f.List.TryGetItemToken(0, out var token));
            Assert.IsTrue(token.IsCurrent);
            Assert.IsFalse(other.List.NotifyItemSizeChanged(token));
            f.List.RefreshItem(0);
            Assert.IsFalse(token.IsCurrent);
            Assert.IsNull(token.GameObject);
            Assert.IsFalse(f.List.NotifyItemSizeChanged(token));
            f.List.TryGetItemToken(0, out token);
            f.ShowMiddle();
            Assert.IsFalse(token.IsCurrent);
            f.List.TryGetItemToken(20, out token);
            f.List.SetDatas(f.Data);
            Assert.IsFalse(token.IsCurrent);
            f.List.TryGetItemToken(0, out token);
            f.List.Dispose();
            Assert.IsFalse(token.IsCurrent);
            Assert.IsFalse(f.List.NotifyItemSizeChanged(token));
            Assert.IsFalse(f.List.NotifyItemSizeChanged(0));
            Assert.AreEqual(0, f.List.NotifyVisibleItemSizesChanged());
        }

        [Test]
        public void NotifyInvalidOrUncreatedIndexIsHarmlessAndBadMeasurementIsRejected()
        {
            using var f = new Fixture(UGUIVirtualListAxis.Vertical);
            Assert.IsFalse(f.List.NotifyItemSizeChanged(-1));
            Assert.IsFalse(f.List.NotifyItemSizeChanged(99));
            f.View(0).InvalidMeasurement = true;
            Assert.Throws<InvalidOperationException>(() => f.List.NotifyItemSizeChanged(0));
            Assert.IsFalse(float.IsNaN(f.Layout.ContentLength));
            f.View(0).InvalidMeasurement = false;
            Assert.IsTrue(f.List.NotifyItemSizeChanged(0));
        }

        [Test]
        public void NotificationDuringMeasureIsConsumedWithoutRecursiveRefresh()
        {
            using var f = new Fixture(UGUIVirtualListAxis.Vertical);
            var item = f.View(0);
            int measurements = item.Measurements;
            item.MeasureCallback = () => { item.MeasureCallback = null; f.List.NotifyItemSizeChanged(0); };
            f.List.NotifyItemSizeChanged(0);
            Assert.AreEqual(measurements + 2, item.Measurements);
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void LayoutShiftPreservesInertialVelocity(UGUIVirtualListAxis axis)
        {
            using var f = new Fixture(axis);
            f.ShowMiddle();
            var velocity = axis == UGUIVirtualListAxis.Vertical ? new Vector2(0, 500) : new Vector2(-500, 0);
            f.Scroll.velocity = velocity;
            f.SetSize(19, 180);
            f.List.NotifyItemSizeChanged(19);
            Assert.AreEqual(velocity, f.Scroll.velocity);
        }

        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void DraggingDoesNotSnapBackAfterLayoutCompensation(UGUIVirtualListAxis axis)
        {
            using var f = new Fixture(axis, true);
            f.ShowMiddle();
            var eventObject = new GameObject("Resize test EventSystem", typeof(EventSystem));
            try
            {
                var pointer = new PointerEventData(eventObject.GetComponent<EventSystem>())
                {
                    button = PointerEventData.InputButton.Left,
                    position = new Vector2(100, 100)
                };
                f.Scroll.OnBeginDrag(pointer);
                pointer.position += axis == UGUIVirtualListAxis.Vertical ? new Vector2(0, 30) : new Vector2(-30, 0);
                f.Scroll.OnDrag(pointer);
                f.Scroll.onValueChanged.Invoke(Vector2.zero);
                f.SetSize(19, 180);
                f.List.NotifyItemSizeChanged(19);
                float compensated = f.Position;
                f.Scroll.OnDrag(pointer);
                Assert.AreEqual(compensated, f.Position, 0.01f, "同一指针位置不能把布局补偿覆盖掉");
                f.Scroll.OnEndDrag(pointer);
            }
            finally { UnityEngine.Object.DestroyImmediate(eventObject); }
        }

        [Test]
        public void DraggingVelocityExcludesLayoutShift()
        {
            if (Time.unscaledDeltaTime <= 0f)
                Assert.Ignore("该用例需要有效 unscaledDeltaTime，在 Play Mode 回归环境中执行。");
            using var f = new Fixture(UGUIVirtualListAxis.Vertical, true);
            f.ShowMiddle();
            var eventObject = new GameObject("Velocity test EventSystem", typeof(EventSystem));
            try
            {
                var pointer = new PointerEventData(eventObject.GetComponent<EventSystem>())
                {
                    button = PointerEventData.InputButton.Left,
                    position = new Vector2(100, 100)
                };
                f.Scroll.OnBeginDrag(pointer);
                pointer.position += new Vector2(0, 30);
                f.Scroll.OnDrag(pointer);
                f.Scroll.onValueChanged.Invoke(Vector2.zero);
                f.SetSize(19, 180);
                f.List.NotifyItemSizeChanged(19);
                float expected = 30 / Time.unscaledDeltaTime * Mathf.Clamp01(Time.unscaledDeltaTime * 10f);
                typeof(UGUIVirtualListScrollRect).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Scroll, null);
                Assert.AreEqual(expected, f.Scroll.velocity.y, 0.1f);
                f.Scroll.OnEndDrag(pointer);
            }
            finally { UnityEngine.Object.DestroyImmediate(eventObject); }
        }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly ScrollRect Scroll;
            public readonly UGUIVirtualList List;
            public readonly UGUIVariableLinearLayout Layout;
            public readonly List<float> Data = new();
            private readonly UGUIVirtualListAxis _axis;

            public Fixture(UGUIVirtualListAxis axis, bool dragAware = false, float spacing = 0f)
            {
                _axis = axis;
                Root = new GameObject("Resize List", typeof(RectTransform));
                Scroll = dragAware ? (ScrollRect)Root.AddComponent<UGUIVirtualListScrollRect>() : Root.AddComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                ((RectTransform)viewport.transform).sizeDelta = new Vector2(300, 300);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                Scroll.viewport = (RectTransform)viewport.transform;
                Scroll.content = (RectTransform)content.transform;
                Scroll.horizontal = axis == UGUIVirtualListAxis.Horizontal;
                Scroll.vertical = axis == UGUIVirtualListAxis.Vertical;
                Scroll.movementType = ScrollRect.MovementType.Clamped;
                Layout = new UGUIVariableLinearLayout(axis, new Vector2(100, 100), spacing);
                List = new UGUIVirtualList(Scroll, Layout, new Factory(axis)) { BufferSize = 200 };
                for (int i = 0; i < 100; i++) Data.Add(100f);
                List.SetDatas(Data);
            }
            public void ShowMiddle() => List.ScrollToView(20, UGUIVirtualListAlignment.Start);
            public float Position => _axis == UGUIVirtualListAxis.Vertical ? Scroll.content.anchoredPosition.y : -Scroll.content.anchoredPosition.x;
            public void SetPosition(float value)
            {
                var pos = Scroll.content.anchoredPosition;
                if (_axis == UGUIVirtualListAxis.Vertical) pos.y = value; else pos.x = -value;
                Scroll.content.anchoredPosition = pos;
                Scroll.onValueChanged.Invoke(Vector2.zero);
            }
            public View View(int index)
            {
                Assert.IsTrue(List.TryGetItem(index, out var view), $"Item {index} 应已创建");
                return (View)view;
            }
            public void SetSize(int index, float size) => View(index).RectTransform.SetSizeWithCurrentAnchors(
                _axis == UGUIVirtualListAxis.Vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, size);
            public float VisualStart(int index)
            {
                var pos = View(index).RectTransform.anchoredPosition + Scroll.content.anchoredPosition;
                return _axis == UGUIVirtualListAxis.Vertical ? -pos.y : pos.x;
            }
            public void Dispose() { List.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }

        private sealed class Factory : IUGUIVirtualListItemFactory
        {
            private readonly UGUIVirtualListAxis _axis;
            public Factory(UGUIVirtualListAxis axis) { _axis = axis; }
            public int GetItemType(int index, object data) => 0;
            public IUGUIVirtualListItem Create(int itemType, Transform parent)
            {
                var go = new GameObject("Resize Item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go, _axis);
            }
            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
        }

        private sealed class View : IUGUIVirtualListItem
        {
            private readonly UGUIVirtualListAxis _axis;
            public int Binds;
            public int Measurements;
            public bool InvalidMeasurement;
            public Action MeasureCallback;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            public View(GameObject gameObject, UGUIVirtualListAxis axis) { GameObject = gameObject; _axis = axis; }
            public void Bind(int index, object data)
            {
                Binds++;
                RectTransform.SetSizeWithCurrentAnchors(_axis == UGUIVirtualListAxis.Vertical
                    ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, (float)data);
            }
            public Vector2 Measure()
            {
                Measurements++;
                MeasureCallback?.Invoke();
                return InvalidMeasurement ? new Vector2(float.NaN, 100) : RectTransform.rect.size;
            }
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }
        }
    }
}
