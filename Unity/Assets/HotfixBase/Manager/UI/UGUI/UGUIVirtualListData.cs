using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>可选布局能力：在数据插入/删除时同步移动尺寸缓存，而不是把缓存当作索引固定不变。</summary>
    public interface IUGUIVirtualListMutableLayout
    {
        void InsertItems(int index, int count);
        void RemoveItems(int index, int count);
        void InvalidateItemSize(int index);
    }

    public sealed partial class UGUIVirtualList
    {
        private readonly Dictionary<int, ActiveItem> _remappedItems = new();
        private bool _changingData;
        private bool _followEndDuringDataUpdate;

        /// <summary>增量变更前已位于末尾时，变更后继续保持末尾；不会拉走浏览历史内容的用户。</summary>
        public bool KeepEndOnDataChange { get; set; }

        // 数据适配器用版本号识别独占的数据投影，避免与直接修改 Core 的调用方混用。
        internal ulong DataRevision { get; private set; }
        internal bool IsDisposedOrDisposing => _disposed || _disposePending;
        internal bool CanMutateData => !_disposed && !_disposePending && !_refreshing && !_changingData;
        internal bool IsVerticalLayout => _layout.IsVertical;
        internal void StopScrollMotion() => _scrollRect.StopMovement();
        internal void ValidateDataMutation() => GuardDataMutation();

        public int AddData<T>(T data)
        {
            int index = Count;
            InsertData(index, data);
            return index;
        }

        public void AddDatas<T>(IEnumerable<T> datas) => InsertDatas(Count, datas);
        public void InsertData<T>(int index, T data) => InsertDatas(index, new[] { data });

        public void InsertDatas<T>(int index, IEnumerable<T> datas)
        {
            GuardDataMutation();
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (datas == null)
                throw new ArgumentNullException(nameof(datas));
            // 先物化，避免懒枚举中途失败或枚举原数据时导致部分写入。
            var values = MaterializeDatas(datas);
            ThrowIfDisposed();
            if (values.Count == 0)
                return;
            if (values.Count > int.MaxValue - Count)
                throw new ArgumentOutOfRangeException(nameof(datas));

            var anchor = CaptureDataAnchor();
            _changingData = true;
            try
            {
                if (_layout is IUGUIVirtualListMutableLayout mutable)
                    mutable.InsertItems(index, values.Count);
                else
                    ResetUnsupportedLayout(Count + values.Count);
                _datas.InsertRange(index, values);
                DataRevision++;
                RemapActiveItems(index, values.Count, false);
            }
            finally { EndDataMutation(); }
            NotifyDataChanged();
            DataInserted?.Invoke(index, values.Count);
            if (anchor.Index >= index)
                anchor.Index += values.Count;
            FinishDataMutation(anchor);
        }

        public void RemoveAt(int index) => RemoveRange(index, 1);

        public void RemoveRange(int index, int count)
        {
            GuardDataMutation();
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > Count - index)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;
            var anchor = CaptureDataAnchor();
            _changingData = true;
            try
            {
                // 回收回调仍可读取旧数据；删除项不会被错误地重新绑定到别的数据上。
                _removeIndices.Clear();
                foreach (var pair in _activeItems)
                    if (pair.Key >= index && pair.Key < index + count)
                        _removeIndices.Add(pair.Key);
                for (int i = 0; i < _removeIndices.Count; i++)
                    ReleaseItem(_removeIndices[i]);
                if (_layout is IUGUIVirtualListMutableLayout mutable)
                    mutable.RemoveItems(index, count);
                else
                    ResetUnsupportedLayout(Count - count);
                _datas.RemoveRange(index, count);
                DataRevision++;
                RemapActiveItems(index, count, true);
            }
            finally { EndDataMutation(); }
            NotifyDataChanged();
            DataRemoved?.Invoke(index, count);

            if (anchor.Index >= index + count)
                anchor.Index -= count;
            else if (anchor.Index >= index)
                anchor.Index = Math.Min(index, Count - 1); // 删除锚点时优先选择后继，尾部则选择前项。
            FinishDataMutation(anchor);
        }

        public void UpdateData<T>(int index, T data)
        {
            GuardDataMutation();
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            var anchor = CaptureDataAnchor();
            _changingData = true;
            try
            {
                if (_layout is IUGUIVirtualListMutableLayout mutable)
                    mutable.InvalidateItemSize(index);
                else if (_layout.SupportsDynamicSize)
                    ResetUnsupportedLayout(Count);
                _datas[index] = data;
                DataRevision++;
                if (_activeItems.TryGetValue(index, out var active))
                    active.NeedsBind = true;
            }
            finally { EndDataMutation(); }
            NotifyDataChanged();
            FinishDataMutation(anchor);
        }

        /// <summary>清空数据但保留可复用对象；释放对象和模板请使用 Dispose / 组件 Release。</summary>
        public void Clear() => RemoveRange(0, Count);

        private List<object> MaterializeDatas<T>(IEnumerable<T> datas)
        {
            _changingData = true;
            try
            {
                var result = new List<object>();
                foreach (var data in datas)
                    result.Add(data);
                return result;
            }
            finally { EndDataMutation(); }
        }

        private void EndDataMutation()
        {
            _changingData = false;
            DrainPendingDispose();
        }

        private void GuardDataMutation()
        {
            ThrowIfDisposed();
            if (_refreshing || _changingData)
                throw new InvalidOperationException("不能在 Bind、Measure、回收回调或数据枚举期间增删/替换列表数据；请在回调结束后执行。");
        }

        private void ResetUnsupportedLayout(int count)
        {
            // 自定义布局没有增量接口时采用安全降级，不能把旧尺寸缓存错误地套到新索引。
            _layout.SetItemCount(count, true);
            foreach (var active in _activeItems.Values)
                active.NeedsMeasure = _layout.SupportsDynamicSize;
        }

        private void RemapActiveItems(int index, int count, bool removal)
        {
            // 尾部追加或尾部删除不会移动幸存项的索引，不必重建可见字典。
            if ((!removal && index == Count - count) || (removal && index == Count))
                return;
            _remappedItems.Clear();
            foreach (var pair in _activeItems)
            {
                int target = pair.Key;
                if (removal && target >= index + count)
                    target -= count;
                else if (!removal && target >= index)
                    target += count;
                if (target != pair.Key)
                    pair.Value.NeedsBind = true; // Index 和基于 Index 的类型回调必须重新同步。
                _remappedItems.Add(target, pair.Value);
            }
            _activeItems.Clear();
            foreach (var pair in _remappedItems)
                _activeItems.Add(pair.Key, pair.Value);
            _remappedItems.Clear();
        }

        private ScrollAnchor CaptureDataAnchor()
        {
            float scroll = GetRawScrollPosition();
            float max = Mathf.Max(0f, _layout.ContentLength - GetViewportLength());
            int visibleCount = RenderCount;
            int index = visibleCount > 0 ? Mathf.Clamp(FirstVisibleIndex, 0, visibleCount - 1) : -1;
            return new ScrollAnchor
            {
                Valid = true,
                Index = index,
                Offset = index >= 0 ? GetItemStart(index) - scroll : 0f,
                OldPosition = scroll,
                OldMax = max,
                AtEnd = KeepEndOnDataChange && Mathf.Abs(scroll - max) <= 1f
            };
        }

        private void FinishDataMutation(ScrollAnchor anchor)
        {
            if (_disposed || _disposePending) return;
            UpdateContentSize();
            if (RenderCount == 0)
            {
                _scrollRect.StopMovement();
                SetScrollPosition(0f);
                RefreshVisible(false);
                return;
            }
            if (anchor.Index < 0)
            {
                anchor.Index = 0;
                anchor.Offset = GetItemStart(0) - anchor.OldPosition;
            }
            anchor.Index = Math.Min(anchor.Index, RenderCount - 1);
            RestoreScrollAnchor(anchor);
            // 跟随本次追加的尾部直至可见项完成测量，不要求同时开启“尺寸变化保持末尾”。
            _followEndDuringDataUpdate = anchor.AtEnd;
            try { RefreshVisible(false); }
            finally { _followEndDuringDataUpdate = false; }
        }
    }
}
