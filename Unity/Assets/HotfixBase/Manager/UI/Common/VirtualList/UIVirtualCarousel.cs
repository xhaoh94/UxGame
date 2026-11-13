using System;
using UnityEngine;

namespace Ux
{
    /// <summary>拖拽惯性结束后的对齐策略。</summary>
    public enum UIVirtualCarouselSnapMode
    {
        /// <summary>不吸附，完全交给惯性。</summary>
        None = 0,
        /// <summary>顶部对齐与底部对齐各算一个候选，取位移更小的。</summary>
        Nearest = 1,
        /// <summary>一定是顶部对齐：顶部项露过半就回吸补齐，否则让它滚出、下一项顶上。</summary>
        First = 2,
        /// <summary>一定是底部对齐：底部项露过半就下移补齐，否则上移让它滚出。</summary>
        Last = 3,
        /// <summary>惯性决定走首项（向下滚）还是尾项（向上滚）；无方向时退回最近。</summary>
        Inertia = 4
    }

    /// <summary>
    /// 轮播吸附：拖拽结束后等惯性自然停下，再按实际项边界对齐。只支持竖向单列，
    /// 依赖 UIVirtualListScrollRect 的拖拽事件与 UIVirtualScrollNavigator 的平滑滚动。
    /// </summary>
    public sealed class UIVirtualCarousel : IDisposable
    {
        /// <summary>速度平方低于该值视为惯性结束。ScrollRect 自己也在 magnitude &lt; 1 时清零。</summary>
        private const float IdleVelocitySqr = 1f;
        /// <summary>低于该速度不认方向，退回最近对齐。</summary>
        private const float DirectionVelocityThreshold = 50f;

        private readonly UIVirtualList _list;
        private readonly UIVirtualScrollNavigator _navigator;
        private UIVirtualCarouselSnapMode _snapMode = UIVirtualCarouselSnapMode.Nearest;
        private bool _pendingSnap;
        private int _snapDirection;
        private bool _disposed;

        public UIVirtualCarousel(UIVirtualList list, float snapDuration = 0.2f)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            if (!list.IsVertical) throw new NotSupportedException("轮播吸附仅支持竖向列表。");
            SnapDuration = Mathf.Max(0f, snapDuration);
            _navigator = new UIVirtualScrollNavigator(list);
            _list.ScrollStarted += OnScrollStarted;
            _list.ScrollEnded += OnScrollEnded;
        }

        public float SnapDuration { get; set; }
        /// <summary>对齐策略；None 时拖拽结束不再排队吸附。</summary>
        public UIVirtualCarouselSnapMode SnapMode
        {
            get => _snapMode;
            set
            {
                _snapMode = value;
                if (value == UIVirtualCarouselSnapMode.None) _pendingSnap = false;
            }
        }
        /// <summary>正在吸附，或惯性还没结束、吸附在排队。</summary>
        public bool IsSnapping => _pendingSnap || _navigator.IsActive;

        /// <summary>当前视口顶部所在项的索引；循环列表下是虚拟索引。</summary>
        public int CurrentPage
        {
            get
            {
                if (_disposed || _list.RenderCount == 0) return 0;
                int index = IndexForPosition(_list.ScrollPositionValue);
                return index < 0 ? 0 : index;
            }
        }

        /// <summary>对齐到指定项的顶部。索引是渲染行号，循环列表下传虚拟索引。</summary>
        public bool SnapTo(int index)
        {
            if (_disposed) return false;
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (!_list.CanMutateData) return false;
            if (index >= _list.RenderCount) return false;
            // 主动指定目标时放弃排队中的自动吸附。
            _pendingSnap = false;
            return _navigator.ScrollTo(index, UIVirtualListAlignment.Start, 0f, SnapDuration);
        }

        public bool SnapToNext() => SnapTo(CurrentPage + 1);
        public bool SnapToPrevious() => SnapTo(Mathf.Max(0, CurrentPage - 1));

        /// <summary>宿主每帧调用；deltaSeconds 供测试自定义时间源。</summary>
        public void Tick(float? deltaSeconds = null)
        {
            if (_disposed) return;
            _navigator.Tick(deltaSeconds);
            if (!_pendingSnap || _navigator.IsActive) return;
            // 等惯性自然衰减完再吸附，否则会把速度截断、方向还可能相反。
            if (HasInertia()) return;
            _pendingSnap = false;
            SnapToResolvedPage();
        }

        private bool HasInertia() => _list.ScrollVelocity.sqrMagnitude > IdleVelocitySqr;

        private void OnScrollStarted()
        {
            if (_disposed) return;
            // 用户重新拖拽：放弃排队中的吸附，并让出滚动控制权。
            _pendingSnap = false;
            _navigator.Cancel();
        }

        private void OnScrollEnded()
        {
            if (_disposed || !_list.CanMutateData) return;
            if (_snapMode == UIVirtualCarouselSnapMode.None)
            {
                _pendingSnap = false;
                return;
            }
            if (_navigator.IsActive) return;
            if (HasInertia())
            {
                // 方向要在松手这一帧记下来：等惯性归零时 velocity 已经是 0 了。
                Vector2 velocity = _list.ScrollVelocity;
                _snapDirection = Mathf.Abs(velocity.y) >= DirectionVelocityThreshold
                    ? (int)Mathf.Sign(velocity.y)
                    : 0;
                _pendingSnap = true;
                return;
            }
            _snapDirection = 0;
            SnapToResolvedPage();
        }

        private void SnapToResolvedPage()
        {
            if (_disposed || !_list.CanMutateData) return;
            if (_snapMode == UIVirtualCarouselSnapMode.None) return;
            float position = _list.ScrollPositionValue;
            float target = ResolvePageTarget(position);
            float max = Mathf.Max(0f, _list.ContentLength - _list.ViewportSize.y);
            target = Mathf.Clamp(target, 0f, max);
            if (Mathf.Abs(target - position) < 0.5f) return;
            int index = IndexForPosition(target);
            if (index < 0) return;
            float itemStart = -_list.GetItemLayout(index).Position.y;
            _navigator.ScrollTo(index, UIVirtualListAlignment.Start, itemStart - target, SnapDuration);
        }

        private float ResolvePageTarget(float position)
        {
            float viewport = _list.ViewportSize.y;
            switch (_snapMode)
            {
                case UIVirtualCarouselSnapMode.First:
                    return ResolveTopAlign(position);
                case UIVirtualCarouselSnapMode.Last:
                    return ResolveBottomAlign(position, viewport);
                case UIVirtualCarouselSnapMode.Inertia:
                    // 惯性只决定走哪套策略：向上滚（看后面的项）用首项对齐，向下滚用尾项对齐。
                    if (_snapDirection < 0) return ResolveTopAlign(position);
                    if (_snapDirection > 0) return ResolveBottomAlign(position, viewport);
                    break;
            }
            // 最近：顶部对齐与底部对齐各算一个候选，取位移更小的那个。
            float top = ResolveTopAlign(position);
            float bottom = ResolveBottomAlign(position, viewport);
            return Mathf.Abs(top - position) <= Mathf.Abs(bottom - position) ? top : bottom;
        }

        /// <summary>顶部对齐：顶部项露过半就补齐，否则让它滚出、下一项顶上。按实际项边界算，兼容可变高度。</summary>
        private float ResolveTopAlign(float position)
        {
            int index = IndexForPosition(position);
            if (index < 0) return position;
            float start = -_list.GetItemLayout(index).Position.y;
            if (index + 1 >= _list.RenderCount) return start;
            float nextStart = -_list.GetItemLayout(index + 1).Position.y;
            return position - start <= nextStart - position ? start : nextStart;
        }

        /// <summary>底部对齐：底部项露过半就补齐，否则上移让它滚出、上一项成为底部项。</summary>
        private float ResolveBottomAlign(float position, float viewport)
        {
            float bottom = position + viewport;
            int index = IndexForPosition(bottom);
            if (index < 0) return position;
            var current = _list.GetItemLayout(index);
            float end = -current.Position.y + current.Size.y;
            if (index <= 0) return end - viewport;
            var previous = _list.GetItemLayout(index - 1);
            float previousEnd = -previous.Position.y + previous.Size.y;
            return end - bottom <= bottom - previousEnd ? end - viewport : previousEnd - viewport;
        }

        /// <summary>找到包含指定滚动位置的行；空内容返回 -1。</summary>
        private int IndexForPosition(float position)
        {
            // 必须用渲染行数：循环列表的 GetItemLayout 接受虚拟索引，用真实条数会永远停在最后一个索引。
            int count = _list.RenderCount;
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
            _pendingSnap = false;
            try { _list.ScrollStarted -= OnScrollStarted; } catch { }
            try { _list.ScrollEnded -= OnScrollEnded; } catch { }
            _navigator.Dispose();
        }
    }
}
