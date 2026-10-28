using System;
using UnityEngine;

namespace Ux
{
    /// <summary>循环布局标记：虚拟索引可按周期取模映射到真实数据索引。</summary>
    public interface IUGUIVirtualListLoopLayout
    {
        bool LoopEnabled { get; }
        int DataCount { get; }
        int VirtualCount { get; }
        float CycleLength { get; }
        int RepeatCount { get; }
        int ToDataIndex(int virtualIndex);
        /// <summary>滚动接近两端时返回需要平移的像素（0 表示不需要），实现无缝循环。</summary>
        float GetRecenterDelta(float scrollPosition, float viewportLength);
    }

    /// <summary>
    /// 循环包装布局：把一个真实周期（DataCount 项）重复 RepeatCount 次，形成可无缝滚动的长列表。
    /// 只支持单轴单列/单行；尺寸缓存按真实索引共享，因此各周期外观一致。
    /// </summary>
    public sealed class UGUIVirtualLoopLayout : IUGUIVirtualListLayout, IUGUIVirtualListLoopLayout, IUGUIVirtualListViewportLayout
    {
        private readonly IUGUIVirtualListLayout _inner;
        private readonly int _repeat;
        private int _dataCount;

        public UGUIVirtualLoopLayout(IUGUIVirtualListLayout inner, int repeat = 3)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (repeat < 3) throw new ArgumentOutOfRangeException(nameof(repeat), "循环至少需要 3 个周期，才能保证两侧有缓冲。");
            _repeat = repeat;
        }

        public bool LoopEnabled => _dataCount > 0;
        public int DataCount => _dataCount;
        public int RepeatCount => _repeat;
        public int VirtualCount => _dataCount * _repeat;
        public bool IsVertical => _inner.IsVertical;
        public bool SupportsDynamicSize => _inner.SupportsDynamicSize;
        public float CrossSize => _inner.CrossSize;

        /// <summary>一个周期的长度（不含首尾留白）。</summary>
        public float CycleLength => _inner.ContentLength;
        public float ContentLength => _dataCount == 0 ? 0f : CycleLength * _repeat;

        public void SetItemCount(int count, bool resetSizes)
        {
            _dataCount = Math.Max(0, count);
            _inner.SetItemCount(_dataCount, resetSizes);
        }

        public int ToDataIndex(int virtualIndex)
        {
            if (_dataCount <= 0) return -1;
            int local = virtualIndex % _dataCount;
            return local < 0 ? local + _dataCount : local;
        }

        public UGUIVirtualListItemLayout GetItemLayout(int index)
        {
            if (_dataCount <= 0) return default;
            int local = ToDataIndex(index);
            int cycle = (index - local) / _dataCount;
            float cycleLength = CycleLength;
            var inner = _inner.GetItemLayout(local);
            if (Mathf.Abs(cycleLength) < 0.0001f) return inner;
            var offset = IsVertical
                ? new Vector2(0f, -cycle * cycleLength)
                : new Vector2(cycle * cycleLength, 0f);
            return new UGUIVirtualListItemLayout(inner.Position + offset, inner.Size);
        }

        public bool SetItemSize(int index, Vector2 size)
        {
            if (_dataCount <= 0) return false;
            return _inner.SetItemSize(ToDataIndex(index), size);
        }

        public UGUIVirtualListRange GetVisibleRange(float scrollPosition, float viewportLength, float bufferSize)
        {
            int total = VirtualCount;
            if (total <= 0 || viewportLength <= 0f) return new UGUIVirtualListRange(0, -1);
            float cycleLength = CycleLength;
            if (cycleLength <= 0.0001f) return new UGUIVirtualListRange(0, Mathf.Min(total - 1, 0));
            float end = scrollPosition + viewportLength + Mathf.Max(0f, bufferSize);
            // 从滚动位置所在周期的局部位置开始查询，再向后扩展到窗口覆盖的实际虚拟索引。
            int cycle = Mathf.FloorToInt(scrollPosition / cycleLength);
            float local = scrollPosition - cycle * cycleLength;
            var baseRange = _inner.GetVisibleRange(local, viewportLength + Mathf.Max(0f, bufferSize), bufferSize);
            int start = Mathf.Clamp(cycle * _dataCount + baseRange.Start, 0, total - 1);
            int last = Mathf.Clamp(cycle * _dataCount + Mathf.Max(baseRange.End, baseRange.Start), 0, total - 1);
            // 窗口跨周期时继续向后延伸。
            while (last + 1 < total && ItemStart(last + 1) < end) last++;
            while (last > start && ItemStart(last) > end) last--;
            return new UGUIVirtualListRange(start, last);
        }

        private float ItemStart(int index)
        {
            var layout = GetItemLayout(index);
            return IsVertical ? -layout.Position.y : layout.Position.x;
        }

        /// <summary>滚动进入首/尾周期时平移一个周期长度，视觉上无缝。</summary>
        public float GetRecenterDelta(float scrollPosition, float viewportLength)
        {
            if (_dataCount <= 0 || _repeat < 3) return 0f;
            float cycleLength = CycleLength;
            if (cycleLength <= 0.0001f) return 0f;
            // 内容太短时平移会导致可见区跨越超过一个周期，直接放弃重定位。
            if (viewportLength + 1f >= cycleLength) return 0f;
            if (scrollPosition < cycleLength) return cycleLength;
            if (scrollPosition > cycleLength * (_repeat - 1)) return -cycleLength;
            return 0f;
        }

        public bool SetViewportSize(Vector2 viewportSize)
        {
            if (_inner is IUGUIVirtualListViewportLayout viewport) return viewport.SetViewportSize(viewportSize);
            return false;
        }
    }
}
