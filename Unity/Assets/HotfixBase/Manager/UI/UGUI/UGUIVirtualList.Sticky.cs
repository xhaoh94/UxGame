using System;
using UnityEngine;

namespace Ux
{
    public sealed partial class UGUIVirtualList
    {
        public bool IsVertical => _layout.IsVertical;
        public bool SupportsDynamicItemSize => _layout.SupportsDynamicSize;
        public float ScrollPosition { get { ThrowIfDisposed(); return GetRawScrollPosition(); } }
        public Vector2 ViewportSize { get { ThrowIfDisposed(); return _viewport.rect.size; } }
        internal RectTransform ViewportTransform => _viewport;
        public UGUIVirtualListItemLayout GetItemLayout(int index)
        {
            ThrowIfDisposed();
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
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
