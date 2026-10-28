using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// UGUI 虚拟列表：不继承 MonoBehaviour，由 UI 生命周期负责创建和销毁。
    /// 通过布局策略支持固定/可变单轴线性列表及固定网格，共用 Item 类型回调、对象复用和滚动定位。
    /// </summary>
    public sealed partial class UGUIVirtualList : IDisposable
    {
        private readonly ScrollRect _scrollRect;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly IUGUIVirtualListLayout _layout;
        private readonly IUGUIVirtualListItemFactory _factory;
        private readonly List<object> _datas = new();
        private readonly Dictionary<int, ActiveItem> _activeItems = new();
        private readonly Dictionary<int, Stack<IUGUIVirtualListItem>> _pools = new();
        private readonly UnityAction<Vector2> _onScroll;
        private readonly List<int> _removeIndices = new();

        private bool _disposed;
        private bool _disposePending;
        private bool _refreshing;
        private float _bufferSize = 200f;
        private ulong _bindingVersion;
        private Vector2 _lastViewportSize;
        private bool _layoutRetryPending;
        private bool _layoutRetryWarningIssued;
        private bool _followEndForPendingLayout;

        /// <summary>尺寸缓存变化时保持首个可见 Item 及其视口偏移。</summary>
        public bool PreserveScrollAnchor { get; set; } = true;
        /// <summary>原本在末尾时，尺寸变化后仍保持末尾。适合聊天列表，默认关闭。</summary>
        public bool KeepEndOnSizeChange { get; set; }
        public int FirstVisibleIndex => GetVisibleRangeWithoutBuffer().Start;
        public int LastVisibleIndex => GetVisibleRangeWithoutBuffer().End;

        public UGUIVirtualList(
            ScrollRect scrollRect,
            IUGUIVirtualListLayout layout,
            IUGUIVirtualListItemFactory factory)
        {
            _scrollRect = scrollRect ?? throw new ArgumentNullException(nameof(scrollRect));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _viewport = scrollRect.viewport;
            _content = scrollRect.content;

            if (_viewport == null)
                throw new ArgumentException("ScrollRect.viewport 未设置。", nameof(scrollRect));
            if (_content == null)
                throw new ArgumentException("ScrollRect.content 未设置。", nameof(scrollRect));

            // 虚拟列表直接设置 anchoredPosition，要求 Content 使用左上角锚点。
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(0f, 1f);
            _content.pivot = new Vector2(0f, 1f);
            _lastViewportSize = _viewport.rect.size;
            if (_layout is IUGUIVirtualListViewportLayout viewportLayout)
                viewportLayout.SetViewportSize(_lastViewportSize);

            _onScroll = OnScroll;
            _scrollRect.onValueChanged.AddListener(_onScroll);
            if (_scrollRect is UGUIVirtualListScrollRect virtualScroll)
            {
                virtualScroll.DragStarted += OnUserDragStarted;
                virtualScroll.DragEnded += OnUserDragEnded;
            }
        }

        /// <summary>预留缓冲区，单位为 UI 像素。</summary>
        public float BufferSize
        {
            get => _bufferSize;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new ArgumentOutOfRangeException(nameof(value), "缓冲距离必须是有限数值。");
                _bufferSize = Mathf.Max(0f, value);
            }
        }

        public int Count => _datas.Count;
        public bool IsLoop => _layout is IUGUIVirtualListLoopLayout loop && loop.LoopEnabled;
        /// <summary>当前渲染的虚拟行数；循环模式下是 数据条数 × 周期数。</summary>
        public int RenderCount => _layout is IUGUIVirtualListLoopLayout loop && loop.LoopEnabled
            ? loop.VirtualCount : _datas.Count;
        private bool _recenteringLoop;

        /// <summary>循环模式下把虚拟索引映射为真实数据索引；非循环恒等。</summary>
        public int ToDataIndex(int index) => _layout is IUGUIVirtualListLoopLayout loop && loop.LoopEnabled
            ? loop.ToDataIndex(index)
            : (index >= 0 && index < _datas.Count ? index : -1);

        private object DataAt(int index)
        {
            int dataIndex = ToDataIndex(index);
            return dataIndex >= 0 && dataIndex < _datas.Count ? _datas[dataIndex] : null;
        }
        public float ContentLength => _layout.ContentLength;
        public float ScrollPositionValue => _disposed ? 0f : GetRawScrollPosition();
        public event Action ScrollStarted;
        public event Action ScrollEnded;
        public event Action DataChanged;
        public event Action<int, int> DataInserted;
        public event Action<int, int> DataRemoved;
        public event Action DataReset;
        /// <summary>只重绑已标记的可见项，不重置尺寸缓存。</summary>
        internal void BeginSelectionRender() => ValidateDataMutation();
        internal void EndSelectionRender()
        {
            // QueueItemRebind 已标记差异项，这里只触发一次排版。
            if (_disposed || _disposePending) return;
            RefreshVisible(false);
        }
        public bool IsDisposalComplete { get; private set; }
        public bool HasPendingLayoutWork => _layoutRetryPending;
        public event Action<int, object> ItemClicked;

        public void SetDatas<T>(IEnumerable<T> datas, int scrollToIndex = -1)
        {
            GuardDataMutation();
            var values = datas == null ? new List<object>() : MaterializeDatas(datas);
            ThrowIfDisposed();
            _layoutRetryPending = false;
            _followEndForPendingLayout = false;
            _changingData = true;
            try
            {
                // 回收回调失败时，数据和布局仍保持旧数量，可以修复回调后重试。
                ResetActiveItems();
                _layout.SetItemCount(values.Count, true);
                _datas.Clear();
                _datas.AddRange(values);
                DataRevision++;
            }
            finally { EndDataMutation(); }
            if (_disposed) return;
            NotifyDataChanged();
            DataReset?.Invoke();
            _scrollRect.StopMovement();
            if (_layout is IUGUIVirtualListLoopLayout loop && loop.LoopEnabled)
            {
                // 从中间周期开始，两侧都有完整周期可滚动。
                SetScrollPosition(loop.CycleLength);
            }
            else SetScrollPosition(0f);
            RefreshVisible(true);

            if (scrollToIndex >= 0)
                ScrollToView(scrollToIndex, UGUIVirtualListAlignment.Start);
        }

        public object GetData(int index)
        {
            return DataAt(index);
        }

        public bool TryGetItem(int index, out IUGUIVirtualListItem item)
        {
            if (_activeItems.TryGetValue(index, out var active))
            {
                item = active.Item;
                return true;
            }

            item = null;
            return false;
        }

        /// <summary>获取本次绑定令牌。异步回调应先检查 IsCurrent，再修改对象并发出尺寸通知。</summary>
        public bool TryGetItemToken(int index, out UGUIVirtualListItemToken token)
        {
            if (!_disposed && !_disposePending && _activeItems.TryGetValue(index, out var active) && !active.NeedsBind)
            {
                token = new UGUIVirtualListItemToken(this, index, active.Item, active.BindingVersion);
                return true;
            }
            token = default;
            return false;
        }

        internal bool IsItemTokenCurrent(UGUIVirtualListItemToken token)
        {
            return !_disposed && !_disposePending && ReferenceEquals(token.Owner, this) &&
                _activeItems.TryGetValue(token.Index, out var active) &&
                !active.NeedsBind && active.BindingVersion == token.Version && ReferenceEquals(active.Item, token.Item) &&
                (!(active.Item is UnityEngine.Object unityObject) || unityObject != null) && active.Item.GameObject != null;
        }

        /// <summary>只重新测量当前已创建的 Item，不调用 Bind。不可见、已释放或固定布局返回 false。</summary>
        public bool NotifyItemSizeChanged(int index)
        {
            if (_disposed || _disposePending || !_layout.SupportsDynamicSize || !_activeItems.TryGetValue(index, out var active) || active.NeedsBind)
                return false;
            active.NeedsMeasure = true;
            RefreshVisible(false);
            return true;
        }

        /// <summary>令牌过期时忽略通知，防止异步任务更新被复用/重新绑定的对象。</summary>
        public bool NotifyItemSizeChanged(UGUIVirtualListItemToken token)
        {
            return IsItemTokenCurrent(token) && NotifyItemSizeChanged(token.Index);
        }

        /// <summary>一批对象已修改后统一测量、补偿一次，避免逐条通知引发重复排版。</summary>
        public int NotifyVisibleItemSizesChanged()
        {
            if (_disposed || _disposePending || !_layout.SupportsDynamicSize)
                return 0;
            foreach (var active in _activeItems.Values)
                active.NeedsMeasure = true;
            int count = _activeItems.Count;
            RefreshVisible(false);
            return count;
        }

        public void Refresh()
        {
            ThrowIfDisposed();
            RefreshVisible(true);
        }

        /// <summary>仅刷新布局和可见范围，不重新绑定已有 Item。</summary>
        public void RefreshLayout()
        {
            ThrowIfDisposed();
            RefreshVisible(false);
        }

        /// <summary>宿主在 LateUpdate 调用；消费视口变化或上一帧未收敛的布局，空闲时不排版。</summary>
        public bool RefreshViewportIfChanged()
        {
            if (_disposed || _disposePending || (!_layoutRetryPending && _viewport.rect.size == _lastViewportSize))
                return false;
            RefreshVisible(false);
            return true;
        }

        // 适配器在提交增量变更前标记父行，渲染异常后该行也会在下次刷新中正确重绑。
        internal void QueueItemRebind(int index)
        {
            if (_activeItems.TryGetValue(index, out var active)) active.NeedsBind = true;
        }

        public void RefreshItem(int index)
        {
            ThrowIfDisposed();
            if (index < 0 || index >= RenderCount)
                return;

            if (_activeItems.TryGetValue(index, out var active))
            {
                active.NeedsBind = true;
                RefreshVisible(false);
            }
        }

        public void ScrollToView(
            int index,
            UGUIVirtualListAlignment alignment = UGUIVirtualListAlignment.Nearest)
        {
            ThrowIfDisposed();
            if (index < 0 || index >= RenderCount)
                return;

            _followEndForPendingLayout = false;
            var itemLayout = _layout.GetItemLayout(index);
            float viewportLength = _layout.IsVertical
                ? _viewport.rect.height
                : _viewport.rect.width;
            float current = GetScrollPosition();
            float target = current;
            float itemStart = _layout.IsVertical ? -itemLayout.Position.y : itemLayout.Position.x;
            float itemLength = _layout.IsVertical ? itemLayout.Size.y : itemLayout.Size.x;

            switch (alignment)
            {
                case UGUIVirtualListAlignment.Start:
                    target = itemStart;
                    break;
                case UGUIVirtualListAlignment.Center:
                {
                    target = itemStart - (viewportLength - itemLength) * 0.5f;
                    break;
                }
                case UGUIVirtualListAlignment.End:
                {
                    target = itemStart + itemLength - viewportLength;
                    break;
                }
                case UGUIVirtualListAlignment.Nearest:
                {
                    float itemEnd = itemStart + itemLength;
                    if (itemStart < current)
                        target = itemStart;
                    else if (itemEnd > current + viewportLength)
                        target = itemEnd - viewportLength;
                    break;
                }
            }

            float max = Mathf.Max(0f, _layout.ContentLength - viewportLength);
            target = Mathf.Clamp(target, 0f, max);
            _scrollRect.StopMovement();
            SetScrollPosition(target);
            RefreshVisible(true);
        }

        internal void NotifyDataChanged() => DataChanged?.Invoke();

        /// <summary>导航用：先把目标 Item 带入缓冲区并完成测量。 Nearest 且已在可见范围时不移动。</summary>
        internal void RefreshForNavigation(int index, UGUIVirtualListAlignment alignment = UGUIVirtualListAlignment.Nearest)
        {
            if (_disposed || _disposePending || index < 0 || index >= _datas.Count) return;
            if (alignment == UGUIVirtualListAlignment.Nearest)
            {
                var visible = GetVisibleRangeWithoutBuffer();
                if (visible.Contains(index)) { RefreshVisible(false); return; }
            }
            var layout = _layout.GetItemLayout(index);
            float start = _layout.IsVertical ? -layout.Position.y : layout.Position.x;
            float length = _layout.IsVertical ? layout.Size.y : layout.Size.x;
            float viewport = GetViewportLength();
            float max = Mathf.Max(0f, _layout.ContentLength - viewport);
            float target = alignment == UGUIVirtualListAlignment.Start ? start - length
                : alignment == UGUIVirtualListAlignment.End ? start + length - viewport + length
                : start - (viewport - length) * 0.5f;
            SetScrollPositionInternal(Mathf.Clamp(target, 0f, max));
        }

        internal void SetScrollPositionInternal(float position)
        {
            if (float.IsNaN(position) || float.IsInfinity(position)) return;
            SetScrollPosition(position);
            RefreshVisible(false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_refreshing || _changingData)
            {
                _disposePending = true;
                return;
            }
            _disposePending = false;
            _disposed = true;
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.RemoveListener(_onScroll);
                if (_scrollRect is UGUIVirtualListScrollRect virtualScroll)
                {
                    virtualScroll.DragStarted -= OnUserDragStarted;
                    virtualScroll.DragEnded -= OnUserDragEnded;
                }
            }
            Exception failure = null;
            _removeIndices.Clear();
            foreach (var pair in _activeItems) _removeIndices.Add(pair.Key);
            for (int i = 0; i < _removeIndices.Count; i++)
            {
                try { ReleaseItem(_removeIndices[i]); }
                catch (Exception ex) { failure ??= ex; }
            }
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var item = pool.Pop();
                    try { _factory.Destroy(item); }
                    catch (Exception ex) { failure ??= ex; }
                }
            }
            _activeItems.Clear();
            _remappedItems.Clear();
            _pools.Clear();
            _datas.Clear();
            DataRevision++;
            ItemClicked = null;
            _layoutRetryPending = false;
            _followEndForPendingLayout = false;
            IsDisposalComplete = true;
            if (failure != null)
                throw new InvalidOperationException("列表释放回调发生异常；其余项和池已继续清理。", failure);
        }

        private void DrainPendingDispose()
        {
            if (_disposePending && !_refreshing && !_changingData)
                Dispose();
        }

        private void OnScroll(Vector2 _)
        {            if (!_disposed)
            {
                if (_followEndForPendingLayout && Mathf.Abs(GetRawScrollPosition() -
                    Mathf.Max(0f, _layout.ContentLength - GetViewportLength())) > 1f)
                    _followEndForPendingLayout = false;
                RefreshVisible(false);
            }
        }

        /// <summary>循环列表滚动越过首/尾周期时静默平移一个周期，视觉无缝。</summary>
        private void RecenterLoop()
        {
            if (_recenteringLoop || !(_layout is IUGUIVirtualListLoopLayout loop) || !loop.LoopEnabled) return;
            float delta = loop.GetRecenterDelta(GetScrollPosition(), GetViewportLength());
            if (Mathf.Abs(delta) < 0.01f) return;
            _recenteringLoop = true;
            try
            {
                SetScrollPosition(GetScrollPosition() + delta);
                RefreshVisible(true);
            }
            finally { _recenteringLoop = false; }
        }

        private void OnUserDragStarted() => ScrollStarted?.Invoke();
        private void OnUserDragEnded() => ScrollEnded?.Invoke();

        private void RefreshVisible(bool force)
        {
            if (_refreshing || _changingData || _disposed || _disposePending)
                return;

            _refreshing = true;
            try
            {
                UpdateViewportLayout();
                bool settled = false;
                _layoutRetryPending = false;
                // 限制收敛次数，防止业务每次测量都改变尺寸而无限循环。
                for (int pass = 0; pass < 20; pass++)
                {
                    UpdateContentSize();
                    float scrollPosition = GetScrollPosition();
                    float viewportLength = _layout.IsVertical
                        ? _viewport.rect.height
                        : _viewport.rect.width;
                    var range = _layout.GetVisibleRange(
                        scrollPosition,
                        viewportLength,
                        _bufferSize);

                    var anchor = CaptureScrollAnchor();
                    SyncVisibleItems(range, force);
                    if (_disposePending) break;
                    ApplyItemLayouts();

                    bool layoutChanged = MeasureVisibleItems();
                    if (_disposePending) break;
                    force = false;
                    if (layoutChanged)
                    {
                        UpdateContentSize();
                        RestoreScrollAnchor(anchor);
                    }
                    if (!layoutChanged && !HasPendingMeasurements())
                    {
                        settled = true;
                        break;
                    }
                }

                if (!_disposePending)
                {
                    UpdateContentSize();
                    ApplyItemLayouts();
                    RecenterLoop();
                    _layoutRetryPending = !settled;
                    _followEndForPendingLayout = !settled && (_followEndDuringDataUpdate || _followEndForPendingLayout);
                    if (settled)
                        _layoutRetryWarningIssued = false;
                    else if (!_layoutRetryWarningIssued)
                    {
                        _layoutRetryWarningIssued = true;
                        Debug.LogWarning("虚拟列表布局在 20 轮内未收敛，已保留待处理状态。宿主下一帧继续；请检查尺寸估算和 Measure 是否稳定。");
                    }
                }
            }
            finally
            {
                _refreshing = false;
                DrainPendingDispose();
            }
        }

        private void SyncVisibleItems(UGUIVirtualListRange range, bool force)
        {
            _removeIndices.Clear();
            foreach (var pair in _activeItems)
            {
                if (!range.Contains(pair.Key))
                    _removeIndices.Add(pair.Key);
            }

            for (int i = 0; i < _removeIndices.Count; i++)
                ReleaseItem(_removeIndices[i]);

            for (int index = range.Start; index <= range.End && !_disposePending; index++)
            {
                if (index < 0 || index >= RenderCount)
                    continue;

                object data = DataAt(index);
                int itemType = _factory.GetItemType(ToDataIndex(index), data);
                if (!_activeItems.TryGetValue(index, out var active))
                {
                    active = new ActiveItem
                    {
                        Item = GetItem(itemType),
                        ItemType = itemType
                    };
                    _activeItems.Add(index, active);
                    BindItem(index, active);
                }
                else if (active.ItemType != itemType)
                {
                    ReleaseItem(index);
                    active = new ActiveItem
                    {
                        Item = GetItem(itemType),
                        ItemType = itemType
                    };
                    _activeItems.Add(index, active);
                    BindItem(index, active);
                }
                else if (force || active.NeedsBind)
                {
                    BindItem(index, active);
                }
            }
        }

        private void BindItem(int index, ActiveItem active)
        {
            // 每次绑定都换令牌，包括同一索引被 Refresh 或 SetDatas 重新赋值的情况。
            active.BindingVersion = ++_bindingVersion;
            active.NeedsBind = false;
            active.NeedsMeasure = _layout.SupportsDynamicSize;
            var item = active.Item;
            var token = new UGUIVirtualListItemToken(this, index, item, active.BindingVersion);
            try
            {
                // 绑定前先给定交叉轴尺寸，文本换行及内容布局才能使用正确的宽/高。
                SetItemGeometry(item.RectTransform, _layout.GetItemLayout(index), true);
                item.GameObject.SetActive(true);
                int logicalIndex = ToDataIndex(index);
                item.SetClickHandler(() =>
                {
                    if (!_changingData && IsItemTokenCurrent(token))
                        ItemClicked?.Invoke(logicalIndex, DataAt(index));
                });
                item.Bind(logicalIndex, data: DataAt(index));
            }
            catch
            {
                // Bind 中可以捕获令牌，但失败的绑定不能继续接收旧点击/异步结果。
                active.BindingVersion = ++_bindingVersion;
                active.NeedsBind = true;
                active.NeedsMeasure = false;
                throw;
            }
        }

        private bool MeasureVisibleItems()
        {
            if (!_layout.SupportsDynamicSize)
                return false;

            bool changed = false;
            foreach (var pair in _activeItems)
            {
                if (_disposePending) break;
                if (!pair.Value.NeedsMeasure)
                    continue;

                // 先清标记，使 Measure 中产生的新通知能够被下一轮消费。
                pair.Value.NeedsMeasure = false;
                try
                {
                    var measured = pair.Value.Item.Measure();
                    if (_disposePending) break;
                    if (!IsValidSize(measured))
                        throw new InvalidOperationException($"Item {pair.Key} 的测量尺寸必须为有限正数，实际值为 {measured}。");
                    if (_layout.SetItemSize(pair.Key, measured))
                        changed = true;
                }
                catch
                {
                    // 提交数据后的测量错误也必须可以通过 RefreshLayout 重试。
                    pair.Value.NeedsMeasure = true;
                    throw;
                }
            }

            return changed;
        }

        private bool HasPendingMeasurements()
        {
            if (!_layout.SupportsDynamicSize)
                return false;
            foreach (var active in _activeItems.Values)
                if (active.NeedsMeasure)
                    return true;
            return false;
        }

        private static bool IsValidSize(Vector2 size) => size.x > 0f && size.y > 0f &&
            !float.IsInfinity(size.x) && !float.IsInfinity(size.y);

        private UGUIVirtualListRange GetVisibleRangeWithoutBuffer()
        {
            if (_disposed || _disposePending || RenderCount == 0 || GetViewportLength() <= 0f)
                return new UGUIVirtualListRange(-1, -1);
            float start = GetScrollPosition();
            float end = start + GetViewportLength();
            var range = _layout.GetVisibleRange(start, GetViewportLength(), 0f);
            int first = range.Start;
            int last = range.End;
            // 某些布局的粗范围包含行间距或首尾留白；锚点必须是真正与视口相交的 Item。
            while (first <= last && !IntersectsViewport(first, start, end)) first++;
            while (last >= first && !IntersectsViewport(last, start, end)) last--;
            return first > last ? new UGUIVirtualListRange(-1, -1) : new UGUIVirtualListRange(first, last);
        }

        private bool IntersectsViewport(int index, float start, float end)
        {
            var info = _layout.GetItemLayout(index);
            float itemStart = _layout.IsVertical ? -info.Position.y : info.Position.x;
            float itemLength = _layout.IsVertical ? info.Size.y : info.Size.x;
            return itemStart < end && itemStart + itemLength > start;
        }

        private float GetViewportLength() => _layout.IsVertical ? _viewport.rect.height : _viewport.rect.width;
        private float GetRawScrollPosition() => _layout.IsVertical ? _content.anchoredPosition.y : -_content.anchoredPosition.x;
        private float GetItemStart(int index)
        {
            var position = _layout.GetItemLayout(index).Position;
            return _layout.IsVertical ? -position.y : position.x;
        }

        private void UpdateViewportLayout()
        {
            Vector2 size = _viewport.rect.size;
            if (size == _lastViewportSize)
                return;
            float oldLength = _layout.IsVertical ? _lastViewportSize.y : _lastViewportSize.x;
            var anchor = CaptureScrollAnchor(true, oldLength);
            if (_layout is IUGUIVirtualListViewportLayout viewportLayout)
                viewportLayout.SetViewportSize(size);
            _lastViewportSize = size;
            UpdateContentSize();
            RestoreScrollAnchor(anchor);
        }

        private ScrollAnchor CaptureScrollAnchor(bool viewportChange = false, float oldViewportLength = -1f)
        {
            bool keepEnd = KeepEndOnSizeChange || _followEndDuringDataUpdate || _followEndForPendingLayout;
            if ((!_layout.SupportsDynamicSize && !viewportChange) || _datas.Count == 0 || (!PreserveScrollAnchor && !keepEnd))
                return default;
            float scroll = GetRawScrollPosition();
            float length = oldViewportLength >= 0f ? oldViewportLength : GetViewportLength();
            float max = Mathf.Max(0f, _layout.ContentLength - length);
            var range = GetVisibleRangeWithoutBuffer();
            int index = Mathf.Clamp(range.Start, 0, RenderCount - 1);
            return new ScrollAnchor
            {
                Valid = true,
                Index = index,
                Offset = GetItemStart(index) - scroll,
                OldPosition = scroll,
                OldMax = max,
                AtEnd = keepEnd && Mathf.Abs(scroll - max) <= 1f
            };
        }

        private void RestoreScrollAnchor(ScrollAnchor anchor)
        {
            if (!anchor.Valid)
                return;
            float max = Mathf.Max(0f, _layout.ContentLength - GetViewportLength());
            float target = PreserveScrollAnchor ? GetItemStart(anchor.Index) - anchor.Offset : anchor.OldPosition;
            if (anchor.AtEnd)
                target = max;
            else if (_scrollRect.movementType == ScrollRect.MovementType.Elastic)
            {
                // 保留原本的弹性越界距离，不把正在回弹的内容强行拉回边缘。
                if (anchor.OldPosition < 0f)
                    target = anchor.OldPosition;
                else if (anchor.OldPosition > anchor.OldMax)
                    target = max + anchor.OldPosition - anchor.OldMax;
            }
            if (_scrollRect.movementType == ScrollRect.MovementType.Clamped ||
                (anchor.OldPosition >= 0f && anchor.OldPosition <= anchor.OldMax &&
                 _scrollRect.movementType != ScrollRect.MovementType.Unrestricted))
                target = Mathf.Clamp(target, 0f, max);
            SetScrollPosition(target);
        }

        private void ApplyItemLayouts()
        {
            foreach (var pair in _activeItems)
            {
                var info = _layout.GetItemLayout(pair.Key);
                SetItemGeometry(pair.Value.Item.RectTransform, info, pair.Value.NeedsMeasure);
            }
        }

        private void SetItemGeometry(RectTransform rect, UGUIVirtualListItemLayout info, bool pendingMeasure)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = info.Position;
            // 可变布局不能在测量前覆盖 Bind 设置的主轴尺寸，否则只能测到估算值。
            if (!_layout.SupportsDynamicSize || !pendingMeasure || _layout.IsVertical)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, info.Size.x);
            if (!_layout.SupportsDynamicSize || !pendingMeasure || !_layout.IsVertical)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, info.Size.y);
        }

        private void UpdateContentSize()
        {
            if (_layout.IsVertical)
            {
                _content.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    Mathf.Max(_viewport.rect.width, _layout.CrossSize));
                _content.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    _layout.ContentLength);
            }
            else
            {
                _content.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    _layout.ContentLength);
                _content.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    Mathf.Max(_viewport.rect.height, _layout.CrossSize));
            }
        }

        private float GetScrollPosition()
        {
            return _layout.IsVertical
                ? Mathf.Max(0f, _content.anchoredPosition.y)
                : Mathf.Max(0f, -_content.anchoredPosition.x);
        }

        private void SetScrollPosition(float position)
        {
            var anchored = _content.anchoredPosition;
            if (_layout.IsVertical)
                anchored.y = position;
            else
                anchored.x = -position;
            if (_scrollRect is UGUIVirtualListScrollRect virtualScroll)
                virtualScroll.ApplyLayoutShift(anchored - _content.anchoredPosition);
            else
                _content.anchoredPosition = anchored;
        }

        private IUGUIVirtualListItem GetItem(int itemType)
        {
            if (!_pools.TryGetValue(itemType, out var pool))
            {
                pool = new Stack<IUGUIVirtualListItem>();
                _pools.Add(itemType, pool);
            }

            IUGUIVirtualListItem item = pool.Count > 0
                ? pool.Pop()
                : _factory.Create(itemType, _content);

            item.GameObject.transform.SetParent(_content, false);
            return item;
        }

        private void ReleaseItem(int index)
        {
            if (!_activeItems.TryGetValue(index, out var active))
                return;

            _activeItems.Remove(index);
            // 场景卸载时，Unity 可能先销毁子对象再回调宿主 OnDestroy。
            if (active.Item is UnityEngine.Object native && native == null)
                return;
            var gameObject = active.Item.GameObject;
            if (gameObject == null)
                return;
            try
            {
                active.Item.SetClickHandler(null);
                active.Item.OnRecycle();
            }
            finally
            {
                // 业务回收回调即使失败，也不能让对象脱离 active 和 pool 两边而永久遗留在场景中。
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                    if (!_pools.TryGetValue(active.ItemType, out var pool))
                    {
                        pool = new Stack<IUGUIVirtualListItem>();
                        _pools.Add(active.ItemType, pool);
                    }
                    pool.Push(active.Item);
                }
            }
        }

        private void ResetActiveItems()
        {
            _removeIndices.Clear();
            foreach (var pair in _activeItems)
                _removeIndices.Add(pair.Key);
            for (int i = 0; i < _removeIndices.Count; i++)
                ReleaseItem(_removeIndices[i]);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed || _disposePending)
                throw new ObjectDisposedException(nameof(UGUIVirtualList));
        }

        private struct ScrollAnchor
        {
            public bool Valid;
            public bool AtEnd;
            public int Index;
            public float Offset;
            public float OldPosition;
            public float OldMax;
        }

        private sealed class ActiveItem
        {
            public int ItemType;
            public bool NeedsMeasure;
            public bool NeedsBind;
            public ulong BindingVersion;
            public IUGUIVirtualListItem Item;
        }
    }

    /// <summary>一次数据绑定的身份。复用、刷新、重新设置数据或销毁后自动失效。</summary>
    public readonly struct UGUIVirtualListItemToken
    {
        internal readonly UGUIVirtualList Owner;
        internal readonly IUGUIVirtualListItem Item;
        internal readonly ulong Version;
        public int Index { get; }
        public bool IsCurrent => Owner != null && Owner.IsItemTokenCurrent(this);
        public GameObject GameObject => IsCurrent ? Item.GameObject : null;

        internal UGUIVirtualListItemToken(UGUIVirtualList owner, int index, IUGUIVirtualListItem item, ulong version)
        {
            Owner = owner;
            Index = index;
            Item = item;
            Version = version;
        }
    }

    public enum UGUIVirtualListAxis
    {
        Vertical,
        Horizontal
    }

    public enum UGUIVirtualListAlignment
    {
        Nearest,
        Start,
        Center,
        End
    }

    public readonly struct UGUIVirtualListRange
    {
        public readonly int Start;
        public readonly int End;

        public UGUIVirtualListRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public bool IsEmpty => End < Start;
        public bool Contains(int index) => index >= Start && index <= End;
    }

    public readonly struct UGUIVirtualListItemLayout
    {
        public readonly Vector2 Position;
        public readonly Vector2 Size;

        public UGUIVirtualListItemLayout(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }
    }

    public interface IUGUIVirtualListItem
    {
        GameObject GameObject { get; }
        RectTransform RectTransform { get; }
        void Bind(int index, object data);
        Vector2 Measure();
        void OnRecycle();
        void SetClickHandler(Action callback);
    }

    public interface IUGUIVirtualListItemFactory
    {
        int GetItemType(int index, object data);
        IUGUIVirtualListItem Create(int itemType, Transform parent);
        void Destroy(IUGUIVirtualListItem item);
    }

    public interface IUGUIVirtualListLayout
    {
        bool IsVertical { get; }
        bool SupportsDynamicSize { get; }
        float ContentLength { get; }
        float CrossSize { get; }

        void SetItemCount(int count, bool resetSizes);
        UGUIVirtualListRange GetVisibleRange(
            float scrollPosition,
            float viewportLength,
            float bufferSize);
        UGUIVirtualListItemLayout GetItemLayout(int index);
        bool SetItemSize(int index, Vector2 size);
    }

    /// <summary>
    /// 固定尺寸单轴布局，使用 O(1) 位置计算。
    /// </summary>
    public sealed class UGUIFixedLinearLayout : IUGUIVirtualListLayout
    {
        private readonly UGUIVirtualListAxis _axis;
        private readonly Vector2 _itemSize;
        private readonly float _spacing;
        private readonly float _paddingStart;
        private readonly float _paddingEnd;
        private int _count;

        public UGUIFixedLinearLayout(
            UGUIVirtualListAxis axis,
            Vector2 itemSize,
            float spacing = 0f,
            float paddingStart = 0f,
            float paddingEnd = 0f)
        {
            _axis = axis;
            _itemSize = new Vector2(Mathf.Max(0.01f, itemSize.x), Mathf.Max(0.01f, itemSize.y));
            _spacing = Mathf.Max(0f, spacing);
            _paddingStart = Mathf.Max(0f, paddingStart);
            _paddingEnd = Mathf.Max(0f, paddingEnd);
        }

        public bool IsVertical => _axis == UGUIVirtualListAxis.Vertical;
        public bool SupportsDynamicSize => false;
        public float CrossSize => IsVertical ? _itemSize.x : _itemSize.y;
        public float ContentLength
        {
            get
            {
                if (_count <= 0)
                    return _paddingStart + _paddingEnd;
                return _paddingStart + _paddingEnd + GetStep() * _count - _spacing;
            }
        }

        public void SetItemCount(int count, bool resetSizes)
        {
            _count = Mathf.Max(0, count);
        }

        public UGUIVirtualListRange GetVisibleRange(
            float scrollPosition,
            float viewportLength,
            float bufferSize)
        {
            if (_count <= 0)
                return new UGUIVirtualListRange(0, -1);

            float step = GetStep();
            int start = Mathf.FloorToInt(Mathf.Max(0f, scrollPosition - bufferSize - _paddingStart) / step);
            int end = Mathf.CeilToInt((scrollPosition + viewportLength + bufferSize - _paddingStart) / step) - 1;
            return new UGUIVirtualListRange(
                Mathf.Clamp(start, 0, _count - 1),
                Mathf.Clamp(end, 0, _count - 1));
        }

        public UGUIVirtualListItemLayout GetItemLayout(int index)
        {
            float offset = _paddingStart + GetStep() * index;
            return IsVertical
                ? new UGUIVirtualListItemLayout(
                    new Vector2(0f, -offset),
                    _itemSize)
                : new UGUIVirtualListItemLayout(
                    new Vector2(offset, 0f),
                    _itemSize);
        }

        public bool SetItemSize(int index, Vector2 size)
        {
            return false;
        }

        private float GetStep()
        {
            return (IsVertical ? _itemSize.y : _itemSize.x) + _spacing;
        }
    }

    /// <summary>
    /// 可变尺寸单轴布局。初始使用默认尺寸，Item 绑定后读取实际尺寸并更新缓存。
    /// </summary>
    public sealed class UGUIVariableLinearLayout : IUGUIVirtualListLayout, IUGUIVirtualListMutableLayout
    {
        private readonly UGUIVirtualListAxis _axis;
        private readonly Vector2 _defaultSize;
        private readonly float _spacing;
        private readonly float _paddingStart;
        private readonly float _paddingEnd;
        private readonly List<float> _sizes = new();
        private readonly List<float> _starts = new();
        private float _contentLength;
        private int _count;
        private bool _positionsDirty = true;

        public UGUIVariableLinearLayout(
            UGUIVirtualListAxis axis,
            Vector2 defaultSize,
            float spacing = 0f,
            float paddingStart = 0f,
            float paddingEnd = 0f)
        {
            _axis = axis;
            _defaultSize = new Vector2(
                Mathf.Max(0.01f, defaultSize.x),
                Mathf.Max(0.01f, defaultSize.y));
            _spacing = Mathf.Max(0f, spacing);
            _paddingStart = Mathf.Max(0f, paddingStart);
            _paddingEnd = Mathf.Max(0f, paddingEnd);
        }

        public bool IsVertical => _axis == UGUIVirtualListAxis.Vertical;
        public bool SupportsDynamicSize => true;
        public float CrossSize => IsVertical ? _defaultSize.x : _defaultSize.y;
        public float ContentLength
        {
            get
            {
                EnsurePositions();
                return _contentLength;
            }
        }

        public void SetItemCount(int count, bool resetSizes)
        {
            _count = Mathf.Max(0, count);
            if (resetSizes || _sizes.Count != _count)
            {
                _sizes.Clear();
                for (int i = 0; i < _count; i++)
                    _sizes.Add(GetDefaultMainSize());
            }

            _positionsDirty = true;
        }

        public UGUIVirtualListRange GetVisibleRange(
            float scrollPosition,
            float viewportLength,
            float bufferSize)
        {
            EnsurePositions();
            if (_count <= 0)
                return new UGUIVirtualListRange(0, -1);

            float start = Mathf.Max(0f, scrollPosition - bufferSize);
            float end = scrollPosition + viewportLength + bufferSize;
            int first = FindIndexAtPosition(start);
            int last = FindIndexAtPosition(end);
            return new UGUIVirtualListRange(
                Mathf.Clamp(first, 0, _count - 1),
                Mathf.Clamp(last, 0, _count - 1));
        }

        public UGUIVirtualListItemLayout GetItemLayout(int index)
        {
            EnsurePositions();
            if (index < 0 || index >= _count)
                return new UGUIVirtualListItemLayout(Vector2.zero, _defaultSize);

            float offset = _starts[index];
            Vector2 size = IsVertical
                ? new Vector2(_defaultSize.x, _sizes[index])
                : new Vector2(_sizes[index], _defaultSize.y);

            return IsVertical
                ? new UGUIVirtualListItemLayout(new Vector2(0f, -offset), size)
                : new UGUIVirtualListItemLayout(new Vector2(offset, 0f), size);
        }

        public bool SetItemSize(int index, Vector2 size)
        {
            if (index < 0 || index >= _count)
                return false;

            float newSize = Mathf.Max(0.01f, IsVertical ? size.y : size.x);
            if (Mathf.Abs(_sizes[index] - newSize) < 0.01f)
                return false;

            _sizes[index] = newSize;
            _positionsDirty = true;
            return true;
        }

        public void InsertItems(int index, int count)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > int.MaxValue - _count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;
            var defaults = new float[count];
            for (int i = 0; i < count; i++) defaults[i] = GetDefaultMainSize();
            _sizes.InsertRange(index, defaults);
            _count += count;
            _positionsDirty = true;
        }

        public void RemoveItems(int index, int count)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > _count - index)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;
            _sizes.RemoveRange(index, count);
            _count -= count;
            _positionsDirty = true;
        }

        public void InvalidateItemSize(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _sizes[index] = GetDefaultMainSize();
            _positionsDirty = true;
        }

        private float GetDefaultMainSize()
        {
            return IsVertical ? _defaultSize.y : _defaultSize.x;
        }

        private void EnsurePositions()
        {
            if (!_positionsDirty)
                return;

            _starts.Clear();
            _starts.Capacity = Mathf.Max(_starts.Capacity, _count);

            float position = _paddingStart;
            for (int i = 0; i < _count; i++)
            {
                _starts.Add(position);
                position += _sizes[i] + _spacing;
            }

            _contentLength = _count > 0
                ? position - _spacing + _paddingEnd
                : _paddingStart + _paddingEnd;
            _positionsDirty = false;
        }

        private int FindIndexAtPosition(float position)
        {
            int low = 0;
            int high = _count - 1;
            int result = _count - 1;

            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                float end = _starts[middle] + _sizes[middle] + _spacing;
                if (end > position)
                {
                    result = middle;
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 使用 UGUI Prefab 的默认 Item 工厂。Prefab 根节点需要挂载 IUGUIVirtualListItem 实现。
    /// </summary>
    public sealed class UGUIVirtualListPrefabFactory : IUGUIVirtualListItemFactory
    {
        private readonly IReadOnlyDictionary<int, GameObject> _prefabs;
        private readonly Func<int, object, int> _itemTypeResolver;
        private readonly Func<int, GameObject, IUGUIVirtualListItem> _itemResolver;

        public UGUIVirtualListPrefabFactory(
            IReadOnlyDictionary<int, GameObject> prefabs,
            Func<int, object, int> itemTypeResolver = null,
            Func<int, GameObject, IUGUIVirtualListItem> itemResolver = null)
        {
            _prefabs = prefabs ?? throw new ArgumentNullException(nameof(prefabs));
            _itemTypeResolver = itemTypeResolver;
            _itemResolver = itemResolver;
        }

        public int GetItemType(int index, object data)
        {
            if (_itemTypeResolver != null)
                return _itemTypeResolver(index, data);

            if (_prefabs.Count == 1)
            {
                foreach (var pair in _prefabs)
                    return pair.Key;
            }

            throw new InvalidOperationException(
                "多模板列表需要传入 itemTypeResolver。 ");
        }

        public IUGUIVirtualListItem Create(int itemType, Transform parent)
        {
            if (!_prefabs.TryGetValue(itemType, out var prefab) || prefab == null)
                throw new InvalidOperationException($"没有找到 Item 模板，type={itemType}");

            var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            var item = _itemResolver != null
                ? _itemResolver(itemType, instance)
                : instance.GetComponent<IUGUIVirtualListItem>();

            if (item == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Item Prefab {prefab.name} 根节点没有实现 IUGUIVirtualListItem。 ");
            }

            return item;
        }

        public void Destroy(IUGUIVirtualListItem item)
        {
            if (item == null || (item is UnityEngine.Object native && native == null))
                return;
            var gameObject = item.GameObject;
            if (gameObject == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(gameObject);
            else
                UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
