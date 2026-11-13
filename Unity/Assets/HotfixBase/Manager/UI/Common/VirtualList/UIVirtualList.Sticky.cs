using System;
using UnityEngine;

namespace Ux
{
    public sealed partial class UIVirtualList
    {
        public bool IsVertical => _layout.IsVertical;
        public bool SupportsDynamicItemSize => _layout.SupportsDynamicSize;
        public float ScrollPosition { get { ThrowIfDisposed(); return GetRawScrollPosition(); } }
        /// <summary>当前滚动速度（像素/秒）。吸附需要等它衰减到 0，否则会截断惯性。</summary>
        public Vector2 ScrollVelocity => _disposed || _disposePending || _scrollRect == null
            ? Vector2.zero : _scrollRect.velocity;
        public Vector2 ViewportSize { get { ThrowIfDisposed(); return _viewport.rect.size; } }
        internal RectTransform ViewportTransform => _viewport;
        public UIVirtualListItemLayout GetItemLayout(int index)
        {
            ThrowIfDisposed();
            // 索引是渲染行号：循环列表下 0..RenderCount-1，业务真实索引只是它的子集。
            if (index < 0 || index >= RenderCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _layout.GetItemLayout(index);
        }

        // 吸顶副本也属于渲染回调，沿用普通 Item 的数据修改/延迟释放保护。
        internal void BeginExternalRender()
        {
            ValidateDataMutation();
            _refreshing = true;
        }
        internal void EndExternalRender()
        {
            _refreshing = false;
            foreach (var item in _activeItems.Values)
                if (item.NeedsBind || item.NeedsMeasure) { _layoutRetryPending = true; break; }
            DrainPendingDispose();
        }
    }
}
