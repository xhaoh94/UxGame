using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 轮播吸附：拖拽结束后按固定页长对齐到最近的整页。只支持竖向单列，
    /// 依赖 UGUIVirtualListScrollRect 的拖拽结束事件与 UGUIVirtualScrollNavigator 的平滑滚动。
    /// </summary>
    public sealed class UGUIVirtualCarousel : IDisposable
    {
        private readonly UGUIVirtualList _list;
        private readonly UGUIVirtualScrollNavigator _navigator;
        private float _pageLength;
        private bool _disposed;

        public UGUIVirtualCarousel(UGUIVirtualList list, float pageLength, float snapDuration = 0.2f)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            if (!list.IsVertical) throw new NotSupportedException("轮播吸附仅支持竖向列表。");
            SetPageLength(pageLength);
            SnapDuration = Mathf.Max(0f, snapDuration);
            _navigator = new UGUIVirtualScrollNavigator(list);
            _list.ScrollEnded += OnScrollEnded;
        }

        /// <summary>一页的滚动长度（通常是 Item 宽度/高度或视口长度）。</summary>
        public float PageLength => _pageLength;
        public float SnapDuration { get; set; }
        public bool IsSnapping => _navigator.IsActive;

        public void SetPageLength(float length)
        {
            if (length <= 0f || float.IsNaN(length) || float.IsInfinity(length))
                throw new ArgumentOutOfRangeException(nameof(length));
            _pageLength = length;
        }

        /// <summary>当前所在页（按滚动位置四舍五入，可能超出最后一项，调用方自行裁剪）。</summary>
        public int CurrentPage
        {
            get
            {
                if (_pageLength <= 0f) return 0;
                return Mathf.Max(0, Mathf.RoundToInt(_list.ScrollPositionValue / _pageLength));
            }
        }

        public bool SnapTo(int page)
        {
            if (_disposed) return false;
            if (page < 0) throw new ArgumentOutOfRangeException(nameof(page));
            if (!_list.CanMutateData) return false;
            float target = Mathf.Clamp(page * _pageLength, 0f,
                Mathf.Max(0f, _list.ContentLength - _list.ViewportSize.y));
            int index = IndexForPosition(target);
            if (index < 0) return false;
            // 通过导航器的 padding 反算目标像素位置，允许页长与 Item 长度不同。
            float itemStart = -_list.GetItemLayout(index).Position.y;
            return _navigator.ScrollTo(index, UGUIVirtualListAlignment.Start, itemStart - target, SnapDuration);
        }

        public bool SnapToNext() => SnapTo(CurrentPage + 1);
        public bool SnapToPrevious() => SnapTo(Mathf.Max(0, CurrentPage - 1));

        /// <summary>宿主每帧调用；deltaSeconds 供测试自定义时间源。</summary>
        public void Tick(float? deltaSeconds = null)
        {
            if (!_disposed) _navigator.Tick(deltaSeconds);
        }

        private void OnScrollEnded()
        {
            if (_disposed || !_list.CanMutateData) return;
            if (_navigator.IsActive) return;
            float position = _list.ScrollPositionValue;
            float target = Mathf.Round(position / _pageLength) * _pageLength;
            float max = Mathf.Max(0f, _list.ContentLength - _list.ViewportSize.y);
            target = Mathf.Clamp(target, 0f, max);
            if (Mathf.Abs(target - position) < 0.5f) return;
            int index = IndexForPosition(target);
            if (index < 0) return;
            float itemStart = -_list.GetItemLayout(index).Position.y;
            _navigator.ScrollTo(index, UGUIVirtualListAlignment.Start, itemStart - target, SnapDuration);
        }

        /// <summary>找到包含指定滚动位置的行；空内容返回 -1。</summary>
        private int IndexForPosition(float position)
        {
            int count = _list.Count;
            if (count == 0) return -1;
            int low = 0, high = count - 1, result = count - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                var layout = _list.GetItemLayout(middle);
                float start = -layout.Position.y;
                if (start + layout.Size.y > position)
                {
                    result = middle;
                    high = middle - 1;
                }
                else low = middle + 1;
            }
            return result;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _list.ScrollEnded -= OnScrollEnded; } catch { }
            _navigator.Dispose();
        }
    }
}
