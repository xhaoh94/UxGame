using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualLoopTests
    {
        [TestCase(UGUIVirtualListAxis.Vertical)]
        [TestCase(UGUIVirtualListAxis.Horizontal)]
        public void VirtualIndicesWrapToSameDataAndPositionsRepeat(UGUIVirtualListAxis axis)
        {
            var inner = new UGUIFlowLayout(axis, new Vector2(100, 100), spacing: new Vector2(4, 4));
            var loop = new UGUIVirtualLoopLayout(inner, 3);
            loop.SetItemCount(5, true);
            Assert.AreEqual(15, loop.VirtualCount);
            Assert.AreEqual(0, loop.ToDataIndex(0));
            Assert.AreEqual(4, loop.ToDataIndex(4));
            Assert.AreEqual(0, loop.ToDataIndex(5));
            Assert.AreEqual(1, loop.ToDataIndex(11));
            Assert.AreEqual(4, loop.ToDataIndex(-1), "负索引对应上一周期的最后一项");
            float cycle = loop.CycleLength;
            Assert.Greater(cycle, 0f);
            Assert.AreEqual(cycle * 3, loop.ContentLength, 0.01f);
            var first = loop.GetItemLayout(0);
            var second = loop.GetItemLayout(5);
            Assert.AreEqual(axis == UGUIVirtualListAxis.Vertical ? -cycle : cycle,
                axis == UGUIVirtualListAxis.Vertical ? second.Position.y : second.Position.x, 0.01f);
            Assert.AreEqual(first.Size, second.Size);
        }

        [Test]
        public void VisibleRangeSpansCycleBoundary()
        {
            var inner = new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 100));
            var loop = new UGUIVirtualLoopLayout(inner, 3);
            loop.SetItemCount(5, true);
            float cycle = loop.CycleLength;
            var range = loop.GetVisibleRange(cycle - 50f, 100f, 0f);
            Assert.LessOrEqual(range.Start, 5);
            Assert.Greater(range.End, 4, "跨周期窗口必须包含下一个周期的行");
            // 起始于周期边界时不应漏掉真实数据索引 0
            var boundary = loop.GetVisibleRange(cycle, 100f, 0f);
            Assert.AreEqual(5, boundary.Start);
            Assert.AreEqual(0, loop.ToDataIndex(boundary.Start));
        }

        [Test]
        public void RecenterKeepsScrollInsideMiddleCycleAndGuardsShortContent()
        {
            var inner = new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(100, 100));
            var loop = new UGUIVirtualLoopLayout(inner, 3);
            loop.SetItemCount(10, true);
            float cycle = loop.CycleLength;
            Assert.AreEqual(cycle, loop.GetRecenterDelta(10f, 200f), 0.01f);
            Assert.AreEqual(-cycle, loop.GetRecenterDelta(cycle * 2f + 10f, 200f), 0.01f);
            Assert.AreEqual(0f, loop.GetRecenterDelta(cycle, 200f));

            // 内容短于视口时不能重定位，否则会跨过整个周期。
            loop.SetItemCount(2, true);
            Assert.AreEqual(0f, loop.GetRecenterDelta(1f, 400f));
        }

        [Test]
        public void CoreUsesLogicalIndexesForRendererAndClick()
        {
            using var f = new Fixture(8, new Vector2(100, 100));
            Assert.IsTrue(f.Core.IsLoop);
            Assert.AreEqual(8, f.Core.Count, "Count 仍是真实数据条数");
            Assert.IsTrue(f.Core.TryGetItem(8, out var secondCycle), "虚拟索引 8 应存在");
            Assert.AreEqual(0, f.RendererIndices[secondCycle]);
            int clicked = -1;
            object clickedData = null;
            f.Core.ItemClicked += (index, data) => { clicked = index; clickedData = data; };
            ((Fixture.View)secondCycle).Click();
            Assert.AreEqual(0, clicked, "点击回调应传真实索引");
            Assert.AreEqual(0, ((int)clickedData));
        }

        [Test]
        public void ScrollingIntoFirstCycleSilentlyShiftsOneCycle()
        {
            using var f = new Fixture(8, new Vector2(100, 100));
            float cycle = ((IUGUIVirtualListLoopLayout)f.CoreLayout).CycleLength;
            Assert.AreEqual(cycle, f.Position, 0.5f, "应从中间周期开始");
            f.SetPosition(30f);
            f.Core.RefreshLayout();
            Assert.AreEqual(cycle + 30f, f.Position, 0.5f, "进入首周期后应回补一个周期长度");
            f.SetPosition(cycle * 2f + 30f);
            f.Core.RefreshLayout();
            Assert.AreEqual(cycle + 30f, f.Position, 0.5f, "进入尾周期后应回退一个周期长度");
        }

        [Test]
        public void LoopRejectsMultipleColumnsAndNonFlatTypes()
        {
            using var f = new Fixture(8, new Vector2(100, 100), allowMultiple: true);
            Assert.IsFalse(f.Core.IsLoop, "多列配置下不启用循环");
        }

        private sealed class Fixture : IDisposable, IUGUIVirtualListItemFactory
        {
            public readonly GameObject Root;
            public readonly ScrollRect Scroll;
            public readonly UGUIVirtualList Core;
            public readonly IUGUIVirtualListLayout CoreLayout;
            public readonly Dictionary<IUGUIVirtualListItem, int> RendererIndices = new Dictionary<IUGUIVirtualListItem, int>();
            public float Position => Scroll.content.anchoredPosition.y;

            public Fixture(int count, Vector2 itemSize, bool allowMultiple = false)
            {
                Root = new GameObject("Loop test", typeof(RectTransform), typeof(UGUIVirtualListScrollRect));
                Scroll = Root.GetComponent<ScrollRect>();
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(Root.transform, false);
                Scroll.viewport = (RectTransform)viewport.transform;
                Scroll.viewport.sizeDelta = new Vector2(300, 300);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                Scroll.content = (RectTransform)content.transform;
                Scroll.vertical = true;
                Scroll.horizontal = false;
                Scroll.movementType = ScrollRect.MovementType.Clamped;
                var inner = new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, itemSize,
                    allowMultiple: allowMultiple);
                IUGUIVirtualListLayout layout = allowMultiple ? inner : (IUGUIVirtualListLayout)new UGUIVirtualLoopLayout(inner, 3);
                CoreLayout = layout;
                Core = new UGUIVirtualList(Scroll, layout, this) { BufferSize = 100 };
                var rows = new List<object>();
                for (int i = 0; i < count; i++) rows.Add(i);
                Core.SetDatas(rows);
            }

            public void SetPosition(float y)
            {
                var p = Scroll.content.anchoredPosition;
                p.y = y;
                Scroll.content.anchoredPosition = p;
            }

            public int GetItemType(int index, object data) => 0;

            public IUGUIVirtualListItem Create(int type, Transform parent)
            {
                var go = new GameObject("Loop row", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new View(go, this);
            }

            public void Destroy(IUGUIVirtualListItem item) => UnityEngine.Object.DestroyImmediate(item.GameObject);
            public void Dispose() { Core.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }

            internal sealed class View : IUGUIVirtualListItem
            {
                private readonly Fixture _owner;
                private Action _click;
                public GameObject GameObject { get; }
                public RectTransform RectTransform => (RectTransform)GameObject.transform;
                public View(GameObject go, Fixture owner) { GameObject = go; _owner = owner; }
                public void Bind(int index, object data) => _owner.RendererIndices[this] = index;
                public Vector2 Measure() => RectTransform.rect.size;
                public void OnRecycle() { }
                public void SetClickHandler(Action callback) { _click = callback; }
                public void Click() => _click?.Invoke();
            }
        }
    }
}
