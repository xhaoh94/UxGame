using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>可选布局能力：在数据插入/删除时同步移动尺寸缓存，而不是把缓存当作索引固定不变。</summary>
    public interface IUIVirtualListMutableLayout
    {
        void InsertItems(int index, int count);
        void RemoveItems(int index, int count);
        void InvalidateItemSize(int index);
    }

    public sealed partial class UIVirtualList
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
            int inserted = values.Count;
            if (inserted == 0)
                return;
            if (inserted > int.MaxValue - Count)
                throw new ArgumentOutOfRangeException(nameof(datas));

            var anchor = CaptureDataAnchor();
            _changingData = true;
            try
            {
                if (_layout is IUIVirtualListMutableLayout mutable)
                    mutable.InsertItems(index, inserted);
                else
                    ResetUnsupportedLayout(Count + inserted);
                _datas.InsertRange(index, values);
                DataRevision++;
                // 测量缓存跟着数据一起平移，避免把 A 的“已测”标到 B 上。
                InsertMeasuredSizes(index, inserted);
                RemapActiveItems(index, inserted, false);
                RemapPinnedIndices(index, inserted, false);
            }
            finally { EndDataMutation(); }
            NotifyDataChanged();
            DataInserted?.Invoke(index, inserted);
            if (anchor.Index >= index)
                anchor.Index += inserted;
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
                if (_layout is IUIVirtualListMutableLayout mutable)
                    mutable.RemoveItems(index, count);
                else
                    ResetUnsupportedLayout(Count - count);
                _datas.RemoveRange(index, count);
                DataRevision++;
                RemoveMeasuredSizes(index, count);
                RemapActiveItems(index, count, true);
                RemapPinnedIndices(index, count, true);
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
                if (_layout is IUIVirtualListMutableLayout mutable)
                    mutable.InvalidateItemSize(index);
                else if (_layout.SupportsDynamicSize)
                    ResetUnsupportedLayout(Count);
                _datas[index] = data;
                DataRevision++;
                InvalidateMeasuredSize(index);
                if (_activeItems.TryGetValue(index, out var active))
                    active.NeedsBind = true;
            }
            finally { EndDataMutation(); }
            NotifyDataChanged();
            FinishDataMutation(anchor);
        }

        /// <summary>清空数据但保留可复用对象；释放对象和模板请使用 Dispose / 组件 Release。</summary>
        public void Clear() => RemoveRange(0, Count);

        private List<object> PrepareDatasScratch<T>(IEnumerable<T> datas)
        {
            if (datas == null)
            {
                _scratchDatas.Clear();
                return _scratchDatas;
            }
            return MaterializeDatas(datas);
        }

        private List<object> MaterializeDatas<T>(IEnumerable<T> datas)
        {
            _changingData = true;
            try
            {
                // 复用暂存列表：SetDatas 提交时与 _datas 交换引用，稳态下不再分配新表。
                var result = _scratchDatas;
                result.Clear();
                // 按下标物化，避开 IEnumerable<T> 枚举器装箱与重复扩容。
                if (datas is IReadOnlyList<T> list)
                {
                    result.Capacity = Mathf.Max(result.Capacity, list.Count);
                    for (int i = 0; i < list.Count; i++)
                        result.Add(list[i]);
                    return result;
                }
                if (datas is ICollection<T> collection)
                    result.Capacity = Mathf.Max(result.Capacity, collection.Count);
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
            ClearMeasuredSizes();
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

        /// <summary>提权记录按数据索引保存，增删数据后必须跟着偏移，否则会指向别的项。</summary>
        private void RemapPinnedIndices(int index, int count, bool removal)
        {
            if (_pinnedFront.Count == 0 && _pinnedBack.Count == 0)
                return;
            RemapPinnedList(_pinnedFront, index, count, removal);
            RemapPinnedList(_pinnedBack, index, count, removal);
        }

        private static void RemapPinnedList(List<int> indices, int index, int count, bool removal)
        {
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                int value = indices[i];
                if (!removal)
                {
                    if (value >= index)
                        indices[i] = value + count;
                    continue;
                }
                if (value >= index && value < index + count)
                    indices.RemoveAt(i);
                else if (value >= index + count)
                    indices[i] = value - count;
            }
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
