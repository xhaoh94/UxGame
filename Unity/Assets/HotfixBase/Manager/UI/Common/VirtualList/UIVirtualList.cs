using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// UI 虚拟列表：不继承 MonoBehaviour，由 UI 生命周期负责创建和销毁。
    /// 通过布局策略支持固定/可变单轴线性列表及固定网格，共用 Item 类型回调、对象复用和滚动定位。
    /// </summary>
    public sealed partial class UIVirtualList : IDisposable
    {
        private readonly ScrollRect _scrollRect;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly IUIVirtualListLayout _layout;
        private readonly IUIVirtualListItemFactory _factory;
        private List<object> _datas = new();
        // 全量替换时先物化到这份暂存，提交时与 _datas 交换引用，稳态零整表拷贝。
        private List<object> _scratchDatas = new();
        private readonly Dictionary<int, ActiveItem> _activeItems = new();
        private readonly Dictionary<int, Stack<IUIVirtualListItem>> _pools = new();
        private readonly UnityAction<Vector2> _onScroll;
        private readonly List<int> _removeIndices = new();
        private readonly List<int> _missingIndices = new();
        private readonly List<UIVirtualPendingCreate> _pendingCreate = new();
        private readonly HashSet<int> _pendingCreateIndices = new();

        private bool _disposed;
        private bool _disposePending;
        private bool _refreshing;
        private float _bufferSize = 200f;
        private ulong _bindingVersion;
        private Vector2 _lastViewportSize;
        private bool _layoutRetryPending;
        private bool _layoutRetryWarningIssued;
        private bool _followEndForPendingLayout;
        // 单次刷新里的测量限流：防止一个抖动项把整列表拖进 20 轮全量 Sync + Apply。
        private bool _measureDeferred;
        private readonly HashSet<int> _measureDeferredWarned = new();
        private const int MaxMeasurePassesPerItem = 3;
        // 多轮收敛诊断：5 秒限流，避免刷屏。
        private float _diagnoseNextTime;
        private readonly StringBuilder _diagnoseBuilder = new();

        // —— 分批生成 ——
        private int _createVersion;
        private float _nextBatchTime;
        private int _fillFrame = -1;
        private bool _batchTimerArmed;
        private bool _forceNextBatch;
        private bool _initialFillPending;
        private bool _initialBatchSpawned;

        // —— 渲染顺序（Content 子节点顺序 = UGUI 绘制顺序，越大越晚绘制、越压在上层）——
        private UIVirtualListRenderOrder _renderOrder;
        private int _renderOrderCenter = NoRenderOrderCenter;
        private float _renderOrderViewportCenter;
        private bool _renderOrderDirty;
        private readonly List<int> _renderOrderBuffer = new();
        // 滚动早退：可见范围与上次一致且没有待绑/待测项时，跳过整轮刷新。
        private UIVirtualListRange _lastVisibleRange;
        private bool _hasLastVisibleRange;
        // 视口变化会改交叉轴尺寸，即使范围没变也必须重摆一次。
        private bool _viewportChanged;
        // Content 尺寸脏检查：值没变就不写 RectTransform。
        private Vector2 _contentSizeCache = new(float.NaN, float.NaN);
        private readonly Comparison<int> _renderOrderComparison;
        private const int NoRenderOrderCenter = int.MinValue;

        // 提权项按数据索引保存，重排后重新应用；顺序即叠放顺序。
        private readonly List<int> _pinnedFront = new();
        private readonly List<int> _pinnedBack = new();

        // 测量扩展链：按 item 实例缓存，扩展列表变更（版本号）时整体作废。
        private readonly List<IUIVirtualListMeasureExtension> _measureExtensions = new();
        private readonly Dictionary<IUIVirtualListItem, MeasureChain> _measureChains = new();
        private int _measureExtensionVersion;

        // 已测尺寸缓存：按真实数据索引保存“测量时的数据引用 + 交叉轴尺寸”。
        // 命中时滑回视口不再重跑 LayoutRebuilder。
        private struct MeasuredSizeKey
        {
            public object Data;
            public float CrossSize;
            public bool Valid;
        }
        private MeasuredSizeKey[] _measuredSizes = Array.Empty<MeasuredSizeKey>();

        private sealed class MeasureChain
        {
            public int Version;
            public Func<int, Vector2> Call;
        }

        /// <summary>尺寸缓存变化时保持首个可见 Item 及其视口偏移。</summary>
        public bool PreserveScrollAnchor { get; set; } = true;
        /// <summary>
        /// 原本在末尾时，尺寸变化后仍保持末尾。适合聊天列表，默认关闭。
        /// 组件层只暴露一个“原本在末尾则继续贴底”开关，同时赋给本属性与 KeepEndOnDataChange。
        /// </summary>
        public bool KeepEndOnSizeChange { get; set; }

        /// <summary>
        /// 滑出视口再回来时复用已测尺寸，不再对每项重复 LayoutRebuilder。
        /// 命中条件：同一数据引用 + 同一交叉轴尺寸。数据替换/增删/交叉轴变化由核心自动失效。
        /// 异步内容变化仍需 NotifyItemSizeChanged：漏通知会直接错位，没有隐式修正。
        /// 默认开启；关掉即退回“每次绑定都重测”的旧行为。
        /// </summary>
        public bool ReuseMeasuredSize { get; set; } = true;
        public int FirstVisibleIndex => GetVisibleRangeWithoutBuffer().Start;
        public int LastVisibleIndex => GetVisibleRangeWithoutBuffer().End;

        public UIVirtualList(
            ScrollRect scrollRect,
            IUIVirtualListLayout layout,
            IUIVirtualListItemFactory factory)
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
            if (_layout is IUIVirtualListViewportLayout viewportLayout)
                viewportLayout.SetViewportSize(_lastViewportSize);

            _onScroll = OnScroll;
            _renderOrderComparison = CompareRenderOrder;
            _scrollRect.onValueChanged.AddListener(_onScroll);
            if (_scrollRect is UIVirtualListScrollRect virtualScroll)
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

        /// <summary>
        /// 注册测量扩展。必须在第一次测量（SetDatas / Refresh / 滚动 / 树或分组初始化）之前完成；
        /// Dispose 后 core 被丢弃，重建列表需要重新注册。组件层已封装这一时序。
        /// </summary>
        public void RegisterMeasureExtension(IUIVirtualListMeasureExtension extension)
        {
            if (extension == null || _measureExtensions.Contains(extension)) return;
            _measureExtensions.Add(extension);
            InvalidateMeasureChains();
        }

        public bool UnregisterMeasureExtension(IUIVirtualListMeasureExtension extension)
        {
            if (extension == null || !_measureExtensions.Remove(extension)) return false;
            InvalidateMeasureChains();
            return true;
        }

        private void InvalidateMeasureChains()
        {
            _measureExtensionVersion++;
            _measureChains.Clear();
        }

        /// <summary>
        /// 走一次测量链；未注册扩展时保持原来的直接调用路径，不产生委托。
        /// 吸顶副本等不经过 MeasureVisibleItems 的测量也可以复用这个入口。
        /// </summary>
        public Vector2 MeasureItem(int index, IUIVirtualListItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (_measureExtensions.Count == 0) return item.Measure();
            return GetMeasureChain(item)(index);
        }

        /// <summary>
        /// 每个 item 实例只建一次链并缓存：item 池化复用，链随实例复用，扩展列表变更时整体作废。
        /// 链签名带 index 是为了不把渲染索引固化进闭包（同一实例会换索引）。
        /// </summary>
        private Func<int, Vector2> GetMeasureChain(IUIVirtualListItem item)
        {
            if (_measureChains.TryGetValue(item, out var cached) &&
                cached.Version == _measureExtensionVersion)
                return cached.Call;

            Func<int, Vector2> call = _ => item.Measure();
            // 执行顺序：ext[0] → ext[1] → … → item.Measure()。
            // 先注册的在外层先执行；后注册的更靠近原始 Measure。
            for (int i = _measureExtensions.Count - 1; i >= 0; i--)
            {
                var extension = _measureExtensions[i];
                var inner = call;
                call = index => extension.Measure(new UIVirtualListMeasureContext(item, index), inner);
            }

            _measureChains[item] = new MeasureChain
            {
                Version = _measureExtensionVersion,
                Call = call,
            };
            return call;
        }

        /// <summary>
        /// Item 在 Content 下的兄弟顺序，即 UGUI 绘制顺序：序号大的后绘制、压在上层。
        /// 中心排序按视口中心计算，滚动跨越 Item 边界时才会重排。
        /// </summary>
        public UIVirtualListRenderOrder RenderOrder
        {
            get => _renderOrder;
            set
            {
                if (_renderOrder == value)
                    return;
                _renderOrder = value;
                _renderOrderCenter = NoRenderOrderCenter;
                _renderOrderDirty = true;
                if (_refreshing || _disposed || _disposePending)
                    return;
                ApplyRenderOrder();
            }
        }

        /// <summary>
        /// 把指定数据索引的 Item 提到最上层，之后的重排仍会保持，直到 <see cref="ClearPinned"/> 或数据重置。
        /// 索引语义与 ItemClicked 一致：循环列表传真实数据索引，所有周期副本一起提权。
        /// </summary>
        public void BringToFront(int index)
        {
            if (_disposed || _disposePending || index < 0 || index >= Count)
                return;
            _pinnedBack.Remove(index);
            _pinnedFront.Remove(index);
            _pinnedFront.Add(index);
            if (!_refreshing) ApplyPinnedOrder();
        }

        /// <summary>把指定 Item 压到最下层，规则与 <see cref="BringToFront"/> 相同。</summary>
        public void BringToBack(int index)
        {
            if (_disposed || _disposePending || index < 0 || index >= Count)
                return;
            _pinnedFront.Remove(index);
            _pinnedBack.Remove(index);
            _pinnedBack.Add(index);
            if (!_refreshing) ApplyPinnedOrder();
        }

        /// <summary>清除全部提权记录。开启 RenderOrder 时立即恢复自然顺序，Unmanaged 保持现状。</summary>
        public void ClearPinned()
        {
            if (_pinnedFront.Count == 0 && _pinnedBack.Count == 0)
                return;
            _pinnedFront.Clear();
            _pinnedBack.Clear();
            _renderOrderDirty = true;
            if (!_refreshing && !_disposed && !_disposePending)
                ApplyRenderOrder();
        }

        public int Count => _datas.Count;
        public bool IsLoop => _layout is IUIVirtualListLoopLayout loop && loop.LoopEnabled;
        /// <summary>当前渲染的虚拟行数；循环模式下是 数据条数 × 周期数。</summary>
        public int RenderCount => _layout is IUIVirtualListLoopLayout loop && loop.LoopEnabled
            ? loop.VirtualCount : _datas.Count;
        private bool _recenteringLoop;

        /// <summary>循环模式下把虚拟索引映射为真实数据索引；非循环恒等。</summary>
        public int ToDataIndex(int index) => _layout is IUIVirtualListLoopLayout loop && loop.LoopEnabled
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
        public bool HasPendingLayoutWork => _layoutRetryPending || _pendingCreate.Count > 0;
        public event Action<int, object> ItemClicked;

        // —— 分批生成 ——

        /// <summary>
        /// 关闭后完全退回“当帧全部创建”的旧行为，便于对比排查。
        /// 默认关闭：只有宿主每帧调 <see cref="TickPendingCreate"/> 时才该打开（组件会自动开）。
        /// </summary>
        public bool IncrementalEnabled { get; set; }

        /// <summary>一批最多创建几项。</summary>
        public int BatchSize { get; set; } = 4;

        /// <summary>两批之间的最小间隔（毫秒）；0 表示每帧都可以补一批。</summary>
        public float BatchIntervalMs { get; set; } = 33f;

        /// <summary>
        /// 首帧同步创建的数量。
        /// &gt;0：先建这么多再分批；0：首帧一个都不建，等第一个批次间隔后开始；
        /// -1（默认）：跟随每批数量，第一批在首个 tick 立即执行。
        /// </summary>
        public int InitialCount { get; set; } = -1;

        /// <summary>单批耗时上限（毫秒），0 表示不限制。防止个别重 Item 把整批拖到掉帧。</summary>
        public float FrameBudgetMs { get; set; }

        public bool IsFilling => _pendingCreate.Count > 0;
        public int PendingCreateCount => _pendingCreate.Count;

        /// <summary>首次填充的分批队列清空时触发一次。</summary>
        public event Action FillingCompleted;

        public void SetDatas<T>(IEnumerable<T> datas, int scrollToIndex = -1) =>
            SetDatasCore(datas, scrollToIndex, false);

        /// <summary>
        /// 全量替换数据但保持当前滚动位置，适合业务改完数据整体刷新。
        /// 与 UpdateData(index, data) 的区别：那个只更新一项，这个换掉整个集合。
        /// 循环列表的锚点跨周期不可靠，这里退化为回到中间周期。
        /// </summary>
        public void UpdateDatas<T>(IEnumerable<T> datas) =>
            SetDatasCore(datas, -1, true);

        private void SetDatasCore<T>(IEnumerable<T> datas, int scrollToIndex, bool preserveScroll)
        {
            GuardDataMutation();
            var values = PrepareDatasScratch(datas);
            ThrowIfDisposed();
            var loop = _layout as IUIVirtualListLoopLayout;
            bool isLoop = loop != null && loop.LoopEnabled;
            // 锚点必须在回收之前取：它读的是当前可见范围。
            bool canPreserve = preserveScroll && !isLoop;
            var anchor = canPreserve ? CaptureDataAnchor() : default;

            _layoutRetryPending = false;
            _followEndForPendingLayout = false;
            _changingData = true;
            try
            {
                // 回收回调失败时，数据和布局仍保持旧数量，可以修复回调后重试。
                ResetActiveItems();
                // 全量替换后索引语义已变，旧的提权记录不能继续指向别的数据。
                ClearPinned();
                ResetIncrementalFill();
                _layout.SetItemCount(values.Count, true);
                // values 就是刚物化好的那份，直接交换引用，不再整表拷进 _datas。
                var previous = _datas;
                _datas = values;
                _scratchDatas = previous;
                _scratchDatas.Clear();
                // 全量替换后尺寸缓存全部作废（布局本身已 SetItemCount(resetSizes: true)）。
                ClearMeasuredSizes();
                DataRevision++;
            }
            finally { EndDataMutation(); }
            if (_disposed) return;
            NotifyDataChanged();
            DataReset?.Invoke();
            _scrollRect.StopMovement();

            if (canPreserve)
            {
                // 复用增量更新的收尾：按锚点恢复位置、必要时贴底、只刷新不重置。
                FinishDataMutation(anchor);
                return;
            }

            // 从中间周期开始，两侧都有完整周期可滚动。
            if (isLoop) SetScrollPosition(loop.CycleLength);
            else SetScrollPosition(0f);
            RefreshVisible(true);

            if (scrollToIndex >= 0)
                ScrollToView(scrollToIndex, UIVirtualListAlignment.Start);
        }

        public object GetData(int index)
        {
            return DataAt(index);
        }

        public bool TryGetItem(int index, out IUIVirtualListItem item)
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
        public bool TryGetItemToken(int index, out UIVirtualListItemToken token)
        {
            if (!_disposed && !_disposePending && _activeItems.TryGetValue(index, out var active) && !active.NeedsBind)
            {
                token = new UIVirtualListItemToken(this, index, active.Item, active.BindingVersion);
                return true;
            }
            token = default;
            return false;
        }

        internal bool IsItemTokenCurrent(UIVirtualListItemToken token)
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
        public bool NotifyItemSizeChanged(UIVirtualListItemToken token)
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
            // 强制刷新不复用旧测量结果：业务可能在数据引用不变的情况下改了内容。
            ClearMeasuredSizes();
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
                InvalidateMeasuredSize(ToDataIndex(index));
                active.NeedsBind = true;
                RefreshVisible(false);
            }
        }

        public void ScrollToView(
            int index,
            UIVirtualListAlignment alignment = UIVirtualListAlignment.Nearest)
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
                case UIVirtualListAlignment.Start:
                    target = itemStart;
                    break;
                case UIVirtualListAlignment.Center:
                {
                    target = itemStart - (viewportLength - itemLength) * 0.5f;
                    break;
                }
                case UIVirtualListAlignment.End:
                {
                    target = itemStart + itemLength - viewportLength;
                    break;
                }
                case UIVirtualListAlignment.Nearest:
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

        /// <summary>
        /// 导航用：目标已在可见范围时不改滚动位置，否则先把目标带进缓冲区并完成测量。
        /// 索引是虚拟索引；循环列表用真实条数判断会永远拦住第 0 周期之外的目标。
        /// </summary>
        internal void RefreshForNavigation(int index, UIVirtualListAlignment alignment = UIVirtualListAlignment.Nearest)
        {
            if (_disposed || _disposePending || index < 0 || index >= RenderCount) return;
            // 已在视口内就不要预跳：导航器随后会自己算目标，这里改位置会让画面先跳一下再被拉回。
            var visible = GetVisibleRangeWithoutBuffer();
            if (visible.Contains(index)) { RefreshVisible(false); return; }
            var layout = _layout.GetItemLayout(index);
            float start = _layout.IsVertical ? -layout.Position.y : layout.Position.x;
            float length = _layout.IsVertical ? layout.Size.y : layout.Size.x;
            float viewport = GetViewportLength();
            float max = Mathf.Max(0f, _layout.ContentLength - viewport);
            float target = alignment == UIVirtualListAlignment.Start ? start - length
                : alignment == UIVirtualListAlignment.End ? start + length - viewport + length
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
                if (_scrollRect is UIVirtualListScrollRect virtualScroll)
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
            _pinnedFront.Clear();
            _pinnedBack.Clear();
            _datas.Clear();
            _scratchDatas.Clear();
            _pendingCreate.Clear();
            _pendingCreateIndices.Clear();
            _missingIndices.Clear();
            _createVersion++;
            _measureExtensions.Clear();
            _measureChains.Clear();
            _measureExtensionVersion++;
            FillingCompleted = null;
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

        /// <summary>
        /// 循环列表滚动越过首/尾周期时静默平移一个周期，视觉无缝。
        /// 必须在算可见范围之前调用：平移后要重新分配 Item，否则视口里是空的。
        /// </summary>
        private void RecenterLoop()
        {
            if (_recenteringLoop || !(_layout is IUIVirtualListLoopLayout loop) || !loop.LoopEnabled) return;
            float delta = loop.GetRecenterDelta(GetScrollPosition(), GetViewportLength());
            if (Mathf.Abs(delta) < 0.01f) return;
            _recenteringLoop = true;
            try { SetScrollPosition(GetScrollPosition() + delta); }
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
                // 先归位再算 range：循环 list 平移周期后必须按新位置重新分配 Item。
                RecenterLoop();
                if (!force && !_layoutRetryPending && !NeedsVisibleRefresh())
                    return;
                bool settled = false;
                bool lastLayoutChanged = false;
                _layoutRetryPending = false;
                _measureDeferred = false;
                int passUsed = 0;
                // 每轮刷新重新计次：只有“本次刷新里反复抖动”的项才会被限流。
                foreach (var active in _activeItems.Values) active.MeasurePasses = 0;
                // 限制收敛次数，防止业务每次测量都改变尺寸而无限循环。
                for (int pass = 0; pass < 20; pass++)
                {
                    passUsed = pass + 1;
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
                    lastLayoutChanged = layoutChanged;
                    if (_disposePending) break;
                    force = false;
                    // 有个别项在本轮刷新里反复变尺寸：保留待测标记交给下一帧，
                    // 本帧不再继续全量重排。
                    if (_measureDeferred) break;
                    if (layoutChanged)
                    {
                        UpdateContentSize();
                        RestoreScrollAnchor(anchor);
                        // 尺寸变化会移动后面的项：如果新范围已被现有项覆盖且没有待测项，
                        // 本轮已收敛，不必再跑一轮完整的 Sync + Apply。
                        if (!HasPendingMeasurements() && IsRangeCovered())
                        {
                            settled = true;
                            break;
                        }
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
                    // 最后一轮没有尺寸变化时，pass 内已经摆过位置，收尾不必再全量遍历一次。
                    if (!settled || lastLayoutChanged) ApplyItemLayouts();
                    _lastVisibleRange = _layout.GetVisibleRange(
                        GetScrollPosition(), GetViewportLength(), _bufferSize);
                    _hasLastVisibleRange = true;
                    _viewportChanged = false;
                    // Item 位置已落定，按最终布局算绘制顺序。
                    ApplyRenderOrder();
                    ApplyPinnedOrder();
                    _layoutRetryPending = !settled;
                    _followEndForPendingLayout = !settled && (_followEndDuringDataUpdate || _followEndForPendingLayout);
                    if (!_measureDeferred) _measureDeferredWarned.Clear();
                    // 正常只需 1~2 轮（排一次 + 吸收一次关系）；4 轮以上说明有东西在反复变。
                    if (passUsed >= 4) ReportLayoutDiagnostics(passUsed);
                    if (settled)
                        _layoutRetryWarningIssued = false;
                    else if (!_layoutRetryWarningIssued)
                    {
                        _layoutRetryWarningIssued = true;
                        if (_measureDeferred)
                            Debug.LogWarning("虚拟列表有 Item 在单次刷新内反复变尺寸，已保留待测状态并推迟到下一帧；请检查该 Item 的 Measure / 关联是否稳定。");
                        else
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

        /// <summary>滚动帧早退：范围、绑定、测量、绘制顺序都没有待处理时，整轮刷新可以跳过。</summary>
        private bool NeedsVisibleRefresh()
        {
            if (_pendingCreate.Count > 0 || _renderOrderDirty || _viewportChanged)
                return true;
            // 中心排序要跟随视口中心，同一范围内也可能换中心项。
            if (_renderOrder == UIVirtualListRenderOrder.CenterOut ||
                _renderOrder == UIVirtualListRenderOrder.EdgeIn)
                return true;
            if (!_hasLastVisibleRange)
                return true;

            var range = _layout.GetVisibleRange(GetScrollPosition(), GetViewportLength(), _bufferSize);
            if (range.Start != _lastVisibleRange.Start || range.End != _lastVisibleRange.End)
                return true;
            foreach (var active in _activeItems.Values)
                if (active.NeedsBind || active.NeedsMeasure)
                    return true;
            return false;
        }

        private void SyncVisibleItems(UIVirtualListRange range, bool force)
        {
            _removeIndices.Clear();
            foreach (var pair in _activeItems)
            {
                if (!range.Contains(pair.Key))
                    _removeIndices.Add(pair.Key);
            }

            for (int i = 0; i < _removeIndices.Count; i++)
                ReleaseItem(_removeIndices[i]);

            _missingIndices.Clear();
            for (int index = range.Start; index <= range.End && !_disposePending; index++)
            {
                if (index < 0 || index >= RenderCount)
                    continue;

                object data = DataAt(index);
                if (!_activeItems.TryGetValue(index, out var active))
                {
                    _missingIndices.Add(index);
                    continue;
                }

                // 索引与数据引用都没变时复用上次类型；ItemTypeProvider 必须是 (index, data) 的纯函数。
                int itemType;
                if (active.CachedIndex == index && ReferenceEquals(active.CachedData, data))
                {
                    itemType = active.ItemType;
                }
                else
                {
                    itemType = _factory.GetItemType(ToDataIndex(index), data);
                    active.CachedIndex = index;
                    active.CachedData = data;
                }
                if (active.ItemType != itemType)
                {
                    ReleaseItem(index);
                    _missingIndices.Add(index);
                    continue;
                }

                if (force || active.NeedsBind)
                    BindItem(index, active);
            }

            if (_missingIndices.Count == 0)
                return;

            if (!ShouldBatchCreate(_missingIndices.Count))
            {
                for (int i = 0; i < _missingIndices.Count && !_disposePending; i++)
                    CreateItem(_missingIndices[i]);

                // 数据量小到一屏建得完，或者根本不是首次填充：不进队列。
                _initialFillPending = false;
                _pendingCreate.Clear();
                _pendingCreateIndices.Clear();
                return;
            }

            // 首次填充：InitialCount > 0 时先同步建这么多，其余按批次从上到下补；
            // InitialCount = -1 / 0 时不做首帧特批，全部交给批次（区别在首批是否立即执行）。
            int immediate = 0;
            if (!_initialBatchSpawned && InitialCount > 0)
                immediate = Mathf.Clamp(InitialCount, 0, _missingIndices.Count);

            for (int i = 0; i < immediate && !_disposePending; i++)
                CreateItem(_missingIndices[i]);
            _initialBatchSpawned = true;

            // 首帧同步建完后重新计时：否则同一帧的 LateUpdate 会拿上一帧的 delta 直接开批，
            // 变成“首帧 + 首批”一起出。
            if (immediate > 0)
            {
                _nextBatchTime = NowSeconds() + BatchIntervalSeconds;
                _fillFrame = Time.frameCount;
            }

            for (int i = immediate; i < _missingIndices.Count; i++)
                EnqueueCreate(_missingIndices[i]);
        }

        /// <summary>只有数据变更后的首次填充才分批；滚动补齐仍同步，避免滑动时出现空白。</summary>
        private bool ShouldBatchCreate(int missingCount)
        {
            if (!IncrementalEnabled || !_initialFillPending || missingCount <= 0)
                return false;

            // 首帧批已经下过之后一律进队列，否则剩余项会在某一轮里被一次性补完，等于没分批。
            if (_initialBatchSpawned)
                return true;

            // 池里已有可复用对象时没有 Instantiate 峰值，分批反而让缓冲项晚一帧出现。
            // 分页/刷新这类“全量重置但池是热的”场景因此完全不受影响。
            if (HasWarmPool())
                return false;

            // -1：不做首帧特批，全部交给批次。
            if (InitialCount < 0)
                return true;

            return missingCount > InitialCount;
        }

        private bool HasWarmPool()
        {
            foreach (var pool in _pools.Values)
            {
                if (pool.Count > 0)
                    return true;
            }
            return false;
        }

        private void CreateItem(int index)
        {
            int itemType = _factory.GetItemType(ToDataIndex(index), DataAt(index));
            CreateItem(index, itemType);
        }

        private void CreateItem(int index, int itemType)
        {
            var active = new ActiveItem
            {
                Item = GetItem(itemType),
                ItemType = itemType,
                CachedIndex = index,
                CachedData = DataAt(index)
            };
            active.ClickAction = () => InvokeItemClicked(active);
            _activeItems.Add(index, active);
            // 新项默认落在 Content 末尾（最上层），必须重新分配兄弟序号。
            _renderOrderDirty = true;
            BindItem(index, active);
        }

        private void EnqueueCreate(int index)
        {
            if (!_pendingCreateIndices.Add(index))
                return;

            _pendingCreate.Add(new UIVirtualPendingCreate
            {
                Index = index,
                Version = _createVersion
            });
        }

        private void ResetIncrementalFill()
        {
            _pendingCreate.Clear();
            _pendingCreateIndices.Clear();
            _nextBatchTime = NowSeconds() + BatchIntervalSeconds;
            _fillFrame = -1;
            // -1：跟随每批数量，首个 tick 立刻开第一批；>=0：等满一个批次间隔。
            _forceNextBatch = InitialCount < 0;
            _batchTimerArmed = _forceNextBatch;
            _initialFillPending = true;
            _initialBatchSpawned = false;
            _createVersion++;
        }

        private static float NowSeconds() => Time.unscaledTime;

        private float BatchIntervalSeconds => Mathf.Max(0f, BatchIntervalMs) * 0.001f;

        /// <summary>
        /// 宿主每帧调用：凑够一批就建几项。队列按索引升序（从上到下）出队。
        /// 计时从「首帧批之后的第一帧」开始算：打开界面那一帧本身可能卡好几秒，
        /// 从 SetDatas 起算会让第一批立刻被这个卡顿“等够了”。
        /// </summary>
        public bool TickPendingCreate()
        {
            if (_disposed || _disposePending || _refreshing || _changingData)
                return false;
            if (_pendingCreate.Count == 0)
                return false;

            if (!_batchTimerArmed)
            {
                // 首帧批所在帧还没走完，不开始计时。
                if (Time.frameCount <= _fillFrame)
                    return false;

                _nextBatchTime = NowSeconds() + BatchIntervalSeconds;
                _batchTimerArmed = true;
                return false;
            }

            if (!_forceNextBatch && NowSeconds() < _nextBatchTime)
                return false;

            _forceNextBatch = false;
            _nextBatchTime = NowSeconds() + BatchIntervalSeconds;

            SortPendingByIndex();

            int budget = Mathf.Max(1, BatchSize);
            float startTime = FrameBudgetMs > 0f ? Time.realtimeSinceStartup : 0f;
            int created = 0;

            while (created < budget && _pendingCreate.Count > 0)
            {
                var pending = _pendingCreate[0];
                _pendingCreate.RemoveAt(0);
                _pendingCreateIndices.Remove(pending.Index);

                if (pending.Version != _createVersion)
                    continue;
                if (pending.Index < 0 || pending.Index >= RenderCount)
                    continue;
                if (_activeItems.ContainsKey(pending.Index))
                    continue;
                if (!IsIndexInCurrentRange(pending.Index))
                    continue;

                CreateItem(pending.Index);
                created++;

                // 单个 Item 超预算时至少建一个，否则队列永远推不动。
                if (FrameBudgetMs > 0f && (Time.realtimeSinceStartup - startTime) * 1000f >= FrameBudgetMs)
                    break;
            }

            if (created > 0)
                RefreshVisible(false);

            if (_pendingCreate.Count == 0)
            {
                _initialFillPending = false;
                FillingCompleted?.Invoke();
            }

            return created > 0;
        }

        /// <summary>从上到下补：按索引升序，滚动过程中插入的项也回到正确顺序。</summary>
        private void SortPendingByIndex()
        {
            if (_pendingCreate.Count <= 1)
                return;

            _pendingCreate.Sort(PendingIndexComparison);
        }

        private static readonly Comparison<UIVirtualPendingCreate> PendingIndexComparison =
            (a, b) => a.Index.CompareTo(b.Index);

        private bool IsIndexInCurrentRange(int index)
        {
            var range = _layout.GetVisibleRange(GetScrollPosition(), GetViewportLength(), _bufferSize);
            return range.Contains(index);
        }

        private void BindItem(int index, ActiveItem active)
        {
            // 每次绑定都换令牌，包括同一索引被 Refresh 或 SetDatas 重新赋值的情况。
            active.BindingVersion = ++_bindingVersion;
            active.NeedsBind = false;
            // 数据与交叉轴都没变时直接用已测尺寸，避免滑回视口时重跑 LayoutRebuilder。
            active.ReusedSize = _layout.SupportsDynamicSize && TryReuseMeasuredSize(index);
            active.NeedsMeasure = _layout.SupportsDynamicSize && !active.ReusedSize;
            var item = active.Item;
            active.Index = index;
            active.LogicalIndex = ToDataIndex(index);
            active.Token = new UIVirtualListItemToken(this, index, item, active.BindingVersion);
            active.CachedIndex = index;
            active.CachedData = DataAt(index);
            try
            {
                // 绑定前先给定交叉轴尺寸，文本换行及内容布局才能使用正确的宽/高。
                SetItemGeometry(item.RectTransform, _layout.GetItemLayout(index), true);
                item.GameObject.SetActive(true);
                item.SetClickHandler(active.ClickAction);
                item.Bind(active.LogicalIndex, data: DataAt(index));
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

        private void InvokeItemClicked(ActiveItem active)
        {
            if (!_changingData && IsItemTokenCurrent(active.Token))
                ItemClicked?.Invoke(active.LogicalIndex, DataAt(active.Index));
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

                // 同一个项在单次刷新里反复变尺寸：保留待测标记并推迟到下一帧，
                // 避免一个抖动项把整列表拖进 20 轮全量 Sync + Apply。
                if (pair.Value.MeasurePasses >= MaxMeasurePassesPerItem)
                {
                    _measureDeferred = true;
                    if (_measureDeferredWarned.Add(pair.Key))
                    {
                        Debug.LogWarning($"虚拟列表 Item {pair.Key} 在单次刷新内已测量 {MaxMeasurePassesPerItem} 次，尺寸仍在变化，已推迟到下一帧；请检查 Measure() / 关联关系是否稳定。");
                    }
                    continue;
                }
                pair.Value.MeasurePasses++;

                // 先清标记，使 Measure 中产生的新通知能够被下一轮消费。
                pair.Value.NeedsMeasure = false;
                try
                {
                    var measured = MeasureItem(pair.Key, pair.Value.Item);
                    if (_disposePending) break;
                    if (!IsValidSize(measured))
                        throw new InvalidOperationException($"Item {pair.Key} 的测量尺寸必须为有限正数，实际值为 {measured}。");
                    if (_layout.SetItemSize(pair.Key, measured))
                        changed = true;
                    pair.Value.ReusedSize = false;
                    MarkMeasured(pair.Key);
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

        // ------------------------------------------------------------ 已测尺寸复用

        /// <summary>数据引用与交叉轴尺寸都没变时返回 true，可以直接用已测尺寸。</summary>
        private bool TryReuseMeasuredSize(int renderIndex)
        {
            if (!ReuseMeasuredSize) return false;
            int dataIndex = ToDataIndex(renderIndex);
            if (dataIndex < 0 || dataIndex >= _datas.Count || dataIndex >= _measuredSizes.Length)
                return false;
            ref var key = ref _measuredSizes[dataIndex];
            if (!key.Valid) return false;
            if (key.CrossSize != _layout.CrossSize) return false;
            return ReferenceEquals(key.Data, _datas[dataIndex]);
        }

        private void MarkMeasured(int renderIndex)
        {
            int dataIndex = ToDataIndex(renderIndex);
            if (dataIndex < 0 || dataIndex >= _datas.Count) return;
            EnsureMeasuredCapacity(_datas.Count);
            _measuredSizes[dataIndex] = new MeasuredSizeKey
            {
                Data = _datas[dataIndex],
                CrossSize = _layout.CrossSize,
                Valid = true,
            };
        }

        private void InvalidateMeasuredSize(int dataIndex)
        {
            if (dataIndex < 0 || dataIndex >= _measuredSizes.Length) return;
            _measuredSizes[dataIndex].Valid = false;
        }

        private void ClearMeasuredSizes()
        {
            if (_measuredSizes.Length > 0)
                Array.Clear(_measuredSizes, 0, _measuredSizes.Length);
        }

        private void EnsureMeasuredCapacity(int count)
        {
            if (_measuredSizes.Length >= count) return;
            Array.Resize(ref _measuredSizes, Mathf.Max(count, Mathf.Max(4, _measuredSizes.Length * 2)));
        }

        /// <summary>插入数据时同步移动测量缓存，保证 key 跟着同一个数据走。</summary>
        private void InsertMeasuredSizes(int index, int count)
        {
            int oldLength = _measuredSizes.Length;
            EnsureMeasuredCapacity(_datas.Count);
            // 把 index 之后的项整体后移，插入区置空。
            int moved = Mathf.Min(oldLength - index, _measuredSizes.Length - index - count);
            if (moved > 0)
                Array.Copy(_measuredSizes, index, _measuredSizes, index + count, moved);
            int clear = Mathf.Min(count, _measuredSizes.Length - index);
            if (clear > 0)
                Array.Clear(_measuredSizes, index, clear);
        }

        private void RemoveMeasuredSizes(int index, int count)
        {
            if (index < 0 || count <= 0 || index >= _measuredSizes.Length) return;
            int remove = Mathf.Min(count, _measuredSizes.Length - index);
            int tail = _measuredSizes.Length - index - remove;
            if (tail > 0)
                Array.Copy(_measuredSizes, index + remove, _measuredSizes, index, tail);
            Array.Clear(_measuredSizes, _measuredSizes.Length - remove, remove);
            Array.Resize(ref _measuredSizes, Mathf.Max(0, _measuredSizes.Length - remove));
        }

        /// <summary>
        /// 多轮才收敛时的汇总诊断（5 秒限流）。只定位“哪个 Item 在反复测”，不猜原因；
        /// 原因需要去看该 Item 的 Measure / UIRelation 配置。
        /// </summary>
        private void ReportLayoutDiagnostics(int passUsed)
        {
            if (Time.unscaledTime < _diagnoseNextTime) return;
            _diagnoseNextTime = Time.unscaledTime + 5f;

            _diagnoseBuilder.Clear();
            _diagnoseBuilder.Append("虚拟列表本次刷新跑了 ").Append(passUsed).Append(" 轮（正常 1~2 轮）");
            _diagnoseBuilder.Append(_measureDeferred ? "，且有 Item 被限流" : "");
            _diagnoseBuilder.Append("。反复测量的 Item：");

            int count = 0;
            foreach (var pair in _activeItems)
            {
                if (pair.Value.MeasurePasses <= 1) continue;
                if (count == 0) _diagnoseBuilder.Append('#');
                else _diagnoseBuilder.Append(", #");
                _diagnoseBuilder.Append(pair.Key)
                    .Append('(').Append(pair.Value.MeasurePasses).Append(" 次)");
                if (++count >= 8)
                {
                    _diagnoseBuilder.Append(" …");
                    break;
                }
            }
            if (count == 0) _diagnoseBuilder.Append("无");
            _diagnoseBuilder.Append("。请检查这些 Item 的 Measure / UIRelation 配置是否稳定。");
            Debug.LogWarning(_diagnoseBuilder.ToString());
        }

        /// <summary>
        /// 当前可见范围是否已被现有 active 项完整覆盖：没有缺项、没有多余项、没有待绑定项。
        /// 用于尺寸变化后提前结束收敛循环，省掉一轮完整的 Sync + Apply。
        /// </summary>
        private bool IsRangeCovered()
        {
            float viewportLength = _layout.IsVertical ? _viewport.rect.height : _viewport.rect.width;
            if (viewportLength <= 0f) return true;

            var range = _layout.GetVisibleRange(GetScrollPosition(), viewportLength, _bufferSize);
            if (range.IsEmpty) return _activeItems.Count == 0;
            // 数量一致 + 逐个命中，才说明既没有缺项也没有需要回收的多余项。
            if (_activeItems.Count != range.End - range.Start + 1) return false;
            for (int i = range.Start; i <= range.End; i++)
            {
                if (!_activeItems.TryGetValue(i, out var active)) return false;
                if (active.NeedsBind) return false;
            }
            return true;
        }

        private static bool IsValidSize(Vector2 size) => size.x > 0f && size.y > 0f &&
            !float.IsInfinity(size.x) && !float.IsInfinity(size.y);

        private UIVirtualListRange GetVisibleRangeWithoutBuffer()
        {
            if (_disposed || _disposePending || RenderCount == 0 || GetViewportLength() <= 0f)
                return new UIVirtualListRange(-1, -1);
            float start = GetScrollPosition();
            float end = start + GetViewportLength();
            var range = _layout.GetVisibleRange(start, GetViewportLength(), 0f);
            int first = range.Start;
            int last = range.End;
            // 某些布局的粗范围包含行间距或首尾留白；锚点必须是真正与视口相交的 Item。
            while (first <= last && !IntersectsViewport(first, start, end)) first++;
            while (last >= first && !IntersectsViewport(last, start, end)) last--;
            return first > last ? new UIVirtualListRange(-1, -1) : new UIVirtualListRange(first, last);
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
            if (_layout is IUIVirtualListViewportLayout viewportLayout)
                viewportLayout.SetViewportSize(size);
            _lastViewportSize = size;
            _viewportChanged = true;
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
                // 复用项也不能写主轴：要让内容自己决定尺寸，否则漂移会被钳回去、永远测不出来。
                SetItemGeometry(pair.Value.Item.RectTransform, info,
                    pair.Value.NeedsMeasure || pair.Value.ReusedSize);
            }
        }

        private void SetItemGeometry(RectTransform rect, UIVirtualListItemLayout info, bool pendingMeasure)
        {
            // anchor/pivot 是常量，绝大多数时候已经是目标值：先读再写，
            // 避免滚动时每帧对每个 Item 重复写 4 次 RectTransform（每次写都会让布局/网格变脏）。
            var topLeft = new Vector2(0f, 1f);
            if (rect.anchorMin != topLeft) rect.anchorMin = topLeft;
            if (rect.anchorMax != topLeft) rect.anchorMax = topLeft;
            if (rect.pivot != topLeft) rect.pivot = topLeft;
            if (rect.anchoredPosition != info.Position) rect.anchoredPosition = info.Position;

            // 可变布局不能在测量前覆盖 Bind 设置的主轴尺寸，否则只能测到估算值。
            if (!_layout.SupportsDynamicSize || !pendingMeasure || _layout.IsVertical)
                SetAxisSize(rect, RectTransform.Axis.Horizontal, info.Size.x);
            if (!_layout.SupportsDynamicSize || !pendingMeasure || !_layout.IsVertical)
                SetAxisSize(rect, RectTransform.Axis.Vertical, info.Size.y);
        }

        /// <summary>
        /// anchorMin == anchorMax 时 sizeDelta 就是实际尺寸；值没变就不写，
        /// 省掉一次 native 调用以及它引发的布局/网格重建。
        /// </summary>
        private static void SetAxisSize(RectTransform rect, RectTransform.Axis axis, float size)
        {
            float current = axis == RectTransform.Axis.Horizontal ? rect.sizeDelta.x : rect.sizeDelta.y;
            if (Mathf.Abs(current - size) <= 0.01f) return;
            rect.SetSizeWithCurrentAnchors(axis, size);
        }

        private void UpdateContentSize()
        {
            float width, height;
            if (_layout.IsVertical)
            {
                width = Mathf.Max(_viewport.rect.width, _layout.CrossSize);
                height = _layout.ContentLength;
            }
            else
            {
                width = _layout.ContentLength;
                height = Mathf.Max(_viewport.rect.height, _layout.CrossSize);
            }
            // 滚动帧里绝大多数时候长度没变，值相等就不要写 native 尺寸。
            if (Mathf.Abs(width - _contentSizeCache.x) <= 0.01f &&
                Mathf.Abs(height - _contentSizeCache.y) <= 0.01f)
                return;
            _contentSizeCache = new Vector2(width, height);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
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
            if (_scrollRect is UIVirtualListScrollRect virtualScroll)
                virtualScroll.ApplyLayoutShift(anchored - _content.anchoredPosition);
            else
                _content.anchoredPosition = anchored;
        }

        private IUIVirtualListItem GetItem(int itemType)
        {
            if (!_pools.TryGetValue(itemType, out var pool))
            {
                pool = new Stack<IUIVirtualListItem>();
                _pools.Add(itemType, pool);
            }

            IUIVirtualListItem item = pool.Count > 0
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
            _renderOrderDirty = true;
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
                        pool = new Stack<IUIVirtualListItem>();
                        _pools.Add(active.ItemType, pool);
                    }
                    pool.Push(active.Item);
                }
            }
        }

        /// <summary>
        /// 按 RenderOrder 重排 Content 下的兄弟顺序；顺序未变时直接返回。
        /// 中心排序只在视口中心跨越 Item 边界时真正重排，不是每帧。
        /// </summary>
        private void ApplyRenderOrder()
        {
            if (_renderOrder == UIVirtualListRenderOrder.Unmanaged || _activeItems.Count == 0)
                return;

            if (_renderOrder == UIVirtualListRenderOrder.CenterOut ||
                _renderOrder == UIVirtualListRenderOrder.EdgeIn)
            {
                // 比较器里不再读 anchoredPosition：一次排序有 n log n 次比较，native 属性访问太贵。
                _renderOrderViewportCenter = GetScrollPosition() + GetViewportLength() * 0.5f;
                int center = FindViewportCenterIndex();
                if (!_renderOrderDirty && center == _renderOrderCenter)
                    return;
                _renderOrderCenter = center;
            }
            else if (!_renderOrderDirty)
            {
                return;
            }

            _renderOrderDirty = false;
            _renderOrderBuffer.Clear();
            foreach (var pair in _activeItems)
                _renderOrderBuffer.Add(pair.Key);
            if (_renderOrderBuffer.Count <= 1)
                return;

            // 按“从底层到顶层”依次落到 Content 末尾；池里的 inactive 对象被挤到前面，不参与绘制。
            _renderOrderBuffer.Sort(_renderOrderComparison);
            for (int i = 0; i < _renderOrderBuffer.Count; i++)
            {
                if (!_activeItems.TryGetValue(_renderOrderBuffer[i], out var active))
                    continue;
                var rect = active.Item?.RectTransform;
                if (rect != null)
                    rect.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 在 ApplyRenderOrder 之后覆盖个别项。已位于目标位置时跳过，避免滚动期间每帧改动层级。
        /// </summary>
        private void ApplyPinnedOrder()
        {
            if (_pinnedFront.Count == 0 && _pinnedBack.Count == 0)
                return;
            // 先压底再置顶，保证同一次 Refresh 里两者都生效；先调用者更靠下，后调用者更靠上。
            for (int i = _pinnedBack.Count - 1; i >= 0; i--)
                ApplyPinnedItem(_pinnedBack[i], false);
            for (int i = 0; i < _pinnedFront.Count; i++)
                ApplyPinnedItem(_pinnedFront[i], true);
        }

        private void ApplyPinnedItem(int dataIndex, bool front)
        {
            if (!IsLoop)
            {
                if (_activeItems.TryGetValue(dataIndex, out var active))
                    SetPinnedSibling(active.Item, front);
                return;
            }
            // 循环列表一个数据有多个周期副本，一起提权；Recenter 后虚拟索引会变，只能按数据索引匹配。
            foreach (var pair in _activeItems)
                if (ToDataIndex(pair.Key) == dataIndex)
                    SetPinnedSibling(pair.Value.Item, front);
        }

        private void SetPinnedSibling(IUIVirtualListItem item, bool front)
        {
            var rect = item?.RectTransform;
            if (rect == null)
                return;
            int target = front ? _content.childCount - 1 : 0;
            if (rect.GetSiblingIndex() != target)
                rect.SetSiblingIndex(target);
        }

        /// <summary>排序结果即绘制顺序：序号小的先画、压在下面。</summary>
        private int CompareRenderOrder(int a, int b)
        {
            switch (_renderOrder)
            {
                case UIVirtualListRenderOrder.Reverse:
                    return b.CompareTo(a);
                case UIVirtualListRenderOrder.CenterOut:
                case UIVirtualListRenderOrder.EdgeIn:
                {
                    int distanceA = GetRenderDistanceKey(a);
                    int distanceB = GetRenderDistanceKey(b);
                    int compare = _renderOrder == UIVirtualListRenderOrder.CenterOut
                        ? distanceA.CompareTo(distanceB)
                        : distanceB.CompareTo(distanceA);
                    // 对称位置距离相等，必须用索引定序；List.Sort 不稳定，否则层级会来回跳。
                    return compare != 0 ? compare : a.CompareTo(b);
                }
                default:
                    return a.CompareTo(b);
            }
        }

        /// <summary>Item 中心到视口中心的主轴距离，量化到 1/16 像素以消除浮点尾差。</summary>
        private int GetRenderDistanceKey(int index)
        {
            if (index < 0 || index >= RenderCount)
                return int.MaxValue;
            var info = _layout.GetItemLayout(index);
            float center = _layout.IsVertical
                ? -info.Position.y + info.Size.y * 0.5f
                : info.Position.x + info.Size.x * 0.5f;
            return Mathf.RoundToInt(Mathf.Abs(center - _renderOrderViewportCenter) * 16f);
        }

        private int FindViewportCenterIndex()
        {
            int best = -1;
            int bestDistance = int.MaxValue;
            foreach (var pair in _activeItems)
            {
                int distance = GetRenderDistanceKey(pair.Key);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = pair.Key;
                }
            }
            return best;
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
                throw new ObjectDisposedException(nameof(UIVirtualList));
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
            /// <summary>本次绑定是否直接复用了已测尺寸（未重新测量）。</summary>
            public bool ReusedSize;
            /// <summary>本次刷新里已经测了几次；超限后保留待测标记并推迟到下一帧。</summary>
            public int MeasurePasses;
            public ulong BindingVersion;
            public IUIVirtualListItem Item;
            /// <summary>当前渲染索引与业务索引；点击委托常驻，靠这两个字段跟随重绑。</summary>
            public int Index;
            public int LogicalIndex;
            public UIVirtualListItemToken Token;
            /// <summary>按 item 实例常驻，Bind 时不再新建闭包。</summary>
            public Action ClickAction;
            /// <summary>GetItemType 的输入缓存：索引与数据引用都没变时不再问业务。</summary>
            public int CachedIndex = -1;
            public object CachedData;
        }

        /// <summary>待创建项。Version 与列表的 _createVersion 不一致时丢弃。</summary>
        private struct UIVirtualPendingCreate
        {
            public int Index;
            public int Version;
        }
    }

    /// <summary>一次数据绑定的身份。复用、刷新、重新设置数据或销毁后自动失效。</summary>
    public readonly struct UIVirtualListItemToken
    {
        internal readonly UIVirtualList Owner;
        internal readonly IUIVirtualListItem Item;
        internal readonly ulong Version;
        public int Index { get; }
        public bool IsCurrent => Owner != null && Owner.IsItemTokenCurrent(this);
        public GameObject GameObject => IsCurrent ? Item.GameObject : null;

        internal UIVirtualListItemToken(UIVirtualList owner, int index, IUIVirtualListItem item, ulong version)
        {
            Owner = owner;
            Index = index;
            Item = item;
            Version = version;
        }
    }

    /// <summary>
    /// Item 之间的绘制顺序。兄弟序号越大越晚绘制、压在上层。
    /// 中心排序只对单列/单行有意义：等距网格里相邻项不重叠，层级差异看不出来。
    /// </summary>
    public enum UIVirtualListRenderOrder
    {
        /// <summary>不干预，沿用创建/复用顺序。</summary>
        Unmanaged = 0,
        /// <summary>数据顺序：竖向从上到下、横向从左到右，后面的压住前面的。</summary>
        Forward = 1,
        /// <summary>反向：竖向从下到上、横向从右到左。</summary>
        Reverse = 2,
        /// <summary>从中心向两侧：中心项在最底层，两侧压住中心。</summary>
        CenterOut = 3,
        /// <summary>从两侧向中心：两侧在最底层，中心项压住两侧。</summary>
        EdgeIn = 4
    }

    public enum UIVirtualListAxis
    {
        Vertical,
        Horizontal
    }

    public enum UIVirtualListAlignment
    {
        Nearest,
        Start,
        Center,
        End
    }

    public readonly struct UIVirtualListRange
    {
        public readonly int Start;
        public readonly int End;

        public UIVirtualListRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public bool IsEmpty => End < Start;
        public bool Contains(int index) => index >= Start && index <= End;
    }

    public readonly struct UIVirtualListItemLayout
    {
        public readonly Vector2 Position;
        public readonly Vector2 Size;

        public UIVirtualListItemLayout(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }
    }

    public interface IUIVirtualListItem
    {
        GameObject GameObject { get; }
        RectTransform RectTransform { get; }
        void Bind(int index, object data);
        Vector2 Measure();
        void OnRecycle();
        void SetClickHandler(Action callback);
    }

    /// <summary>
    /// 测量上下文。循环列表下 Index 是渲染索引，业务索引用 UIVirtualList.ToDataIndex(Index) 换算。
    /// </summary>
    public readonly struct UIVirtualListMeasureContext
    {
        public IUIVirtualListItem Item { get; }
        public int Index { get; }
        public RectTransform Rect => Item.RectTransform;

        public UIVirtualListMeasureContext(IUIVirtualListItem item, int index)
        {
            Item = item;
            Index = index;
        }
    }

    /// <summary>
    /// 测量链扩展。列表每轮测量都会经过（可变尺寸下同一项可能连续测多轮），实现必须幂等：
    /// next 可能在同一轮内被调用多次（例如关联改了几何后需要重排再看尺寸）。
    /// next 的返回值是权威尺寸，扩展可以重新读几何后返回自己的结果覆盖它。
    /// 执行顺序：先注册的在外层先执行，后注册的更靠近原始 Measure()。
    /// 未注册任何扩展时列表走零额外开销的原始路径。
    /// </summary>
    public interface IUIVirtualListMeasureExtension
    {
        Vector2 Measure(in UIVirtualListMeasureContext context, Func<int, Vector2> next);
    }

    public interface IUIVirtualListItemFactory
    {
        int GetItemType(int index, object data);
        IUIVirtualListItem Create(int itemType, Transform parent);
        void Destroy(IUIVirtualListItem item);
    }

    public interface IUIVirtualListLayout
    {
        bool IsVertical { get; }
        bool SupportsDynamicSize { get; }
        float ContentLength { get; }
        float CrossSize { get; }

        void SetItemCount(int count, bool resetSizes);
        UIVirtualListRange GetVisibleRange(
            float scrollPosition,
            float viewportLength,
            float bufferSize);
        UIVirtualListItemLayout GetItemLayout(int index);
        bool SetItemSize(int index, Vector2 size);
    }

    /// <summary>
    /// 固定尺寸单轴布局，使用 O(1) 位置计算。
    /// </summary>
    public sealed class UIFixedLinearLayout : IUIVirtualListLayout
    {
        private readonly UIVirtualListAxis _axis;
        private readonly Vector2 _itemSize;
        private readonly float _spacing;
        private readonly float _paddingStart;
        private readonly float _paddingEnd;
        private int _count;

        public UIFixedLinearLayout(
            UIVirtualListAxis axis,
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

        public bool IsVertical => _axis == UIVirtualListAxis.Vertical;
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

        public UIVirtualListRange GetVisibleRange(
            float scrollPosition,
            float viewportLength,
            float bufferSize)
        {
            if (_count <= 0)
                return new UIVirtualListRange(0, -1);

            float step = GetStep();
            int start = Mathf.FloorToInt(Mathf.Max(0f, scrollPosition - bufferSize - _paddingStart) / step);
            int end = Mathf.CeilToInt((scrollPosition + viewportLength + bufferSize - _paddingStart) / step) - 1;
            return new UIVirtualListRange(
                Mathf.Clamp(start, 0, _count - 1),
                Mathf.Clamp(end, 0, _count - 1));
        }

        public UIVirtualListItemLayout GetItemLayout(int index)
        {
            float offset = _paddingStart + GetStep() * index;
            return IsVertical
                ? new UIVirtualListItemLayout(
                    new Vector2(0f, -offset),
                    _itemSize)
                : new UIVirtualListItemLayout(
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
    public sealed class UIVariableLinearLayout : IUIVirtualListLayout, IUIVirtualListMutableLayout
    {
        private readonly UIVirtualListAxis _axis;
        private readonly Vector2 _defaultSize;
        private readonly float _spacing;
        private readonly float _paddingStart;
        private readonly float _paddingEnd;
        private readonly List<float> _sizes = new();
        // 项起点缓存：只按需推进到可见位置，尺寸变化不再全量重算。
        private readonly List<float> _starts = new();
        private double _totalSize;
        private int _count;

        public UIVariableLinearLayout(
            UIVirtualListAxis axis,
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

        public bool IsVertical => _axis == UIVirtualListAxis.Vertical;
        public bool SupportsDynamicSize => true;
        public float CrossSize => IsVertical ? _defaultSize.x : _defaultSize.y;
        public float ContentLength
        {
            get
            {
                if (_count <= 0) return _paddingStart + _paddingEnd;
                // 增量总长：O(1)，不再依赖任何全量遍历。
                return _paddingStart + _paddingEnd + (float)_totalSize + _spacing * (_count - 1);
            }
        }

        public void SetItemCount(int count, bool resetSizes)
        {
            _count = Mathf.Max(0, count);
            if (resetSizes || _sizes.Count != _count)
            {
                _sizes.Clear();
                _sizes.Capacity = Mathf.Max(_sizes.Capacity, _count);
                float defaultSize = GetDefaultMainSize();
                for (int i = 0; i < _count; i++)
                    _sizes.Add(defaultSize);
                _totalSize = (double)defaultSize * _count;
                TrimStarts(0);
            }
        }

        public UIVirtualListRange GetVisibleRange(
            float scrollPosition,
            float viewportLength,
            float bufferSize)
        {
            if (_count <= 0)
                return new UIVirtualListRange(0, -1);

            float start = Mathf.Max(0f, scrollPosition - bufferSize);
            float end = scrollPosition + viewportLength + bufferSize;
            int first = FindIndexAtPosition(start);
            int last = FindIndexAtPosition(end);
            return new UIVirtualListRange(
                Mathf.Clamp(first, 0, _count - 1),
                Mathf.Clamp(last, 0, _count - 1));
        }

        public UIVirtualListItemLayout GetItemLayout(int index)
        {
            if (index < 0 || index >= _count)
                return new UIVirtualListItemLayout(Vector2.zero, _defaultSize);

            float offset = GetStart(index);
            Vector2 size = IsVertical
                ? new Vector2(_defaultSize.x, _sizes[index])
                : new Vector2(_sizes[index], _defaultSize.y);

            return IsVertical
                ? new UIVirtualListItemLayout(new Vector2(0f, -offset), size)
                : new UIVirtualListItemLayout(new Vector2(offset, 0f), size);
        }

        public bool SetItemSize(int index, Vector2 size)
        {
            if (index < 0 || index >= _count)
                return false;

            float newSize = Mathf.Max(0.01f, IsVertical ? size.y : size.x);
            float previous = _sizes[index];
            if (Mathf.Abs(previous - newSize) < 0.01f)
                return false;

            _sizes[index] = newSize;
            _totalSize += newSize - previous;
            // _starts[index] 只依赖前面的尺寸，保留它；后面的起点按需重算。
            TrimStarts(index + 1);
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
            _totalSize += (double)defaults[0] * count;
            TrimStarts(index);
        }

        public void RemoveItems(int index, int count)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > _count - index)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;
            for (int i = index; i < index + count; i++)
                _totalSize -= _sizes[i];
            _sizes.RemoveRange(index, count);
            _count -= count;
            TrimStarts(index);
        }

        public void InvalidateItemSize(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            float defaultSize = GetDefaultMainSize();
            _totalSize += defaultSize - _sizes[index];
            _sizes[index] = defaultSize;
            TrimStarts(index + 1);
        }

        private float GetDefaultMainSize()
        {
            return IsVertical ? _defaultSize.y : _defaultSize.x;
        }

        private void EnsureStarts(int count)
        {
            if (count > _count) count = _count;
            if (_starts.Count >= count) return;

            _starts.Capacity = Mathf.Max(_starts.Capacity, _count);
            int i = _starts.Count;
            float position = i == 0
                ? _paddingStart
                : _starts[i - 1] + _sizes[i - 1] + _spacing;
            while (i < count)
            {
                _starts.Add(position);
                position += _sizes[i] + _spacing;
                i++;
            }
        }

        /// <summary>丢弃 count 之后的起点缓存；它们依赖已变化的尺寸。</summary>
        private void TrimStarts(int count)
        {
            if (_starts.Count <= count) return;
            _starts.RemoveRange(count, _starts.Count - count);
        }

        private float GetStart(int index)
        {
            EnsureStarts(index + 1);
            return _starts[index];
        }

        private int FindIndexAtPosition(float position)
        {
            // 先保证缓存覆盖 position，二分范围才完整。
            EnsureStartsToCover(position);

            int low = 0;
            int high = _starts.Count - 1;
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

        /// <summary>从当前水位往后推进，直到 position 落在已计算区间内。</summary>
        private void EnsureStartsToCover(float position)
        {
            while (_starts.Count < _count)
            {
                int last = _starts.Count - 1;
                if (last >= 0 && _starts[last] + _sizes[last] + _spacing > position)
                    return;
                EnsureStarts(_starts.Count + 1);
            }
        }
    }

    /// <summary>
    /// 使用 UI Prefab 的默认 Item 工厂。Prefab 根节点需要挂载 IUIVirtualListItem 实现。
    /// </summary>
    public sealed class UIVirtualListPrefabFactory : IUIVirtualListItemFactory
    {
        private readonly IReadOnlyDictionary<int, GameObject> _prefabs;
        private readonly Func<int, object, int> _itemTypeResolver;
        private readonly Func<int, GameObject, IUIVirtualListItem> _itemResolver;

        public UIVirtualListPrefabFactory(
            IReadOnlyDictionary<int, GameObject> prefabs,
            Func<int, object, int> itemTypeResolver = null,
            Func<int, GameObject, IUIVirtualListItem> itemResolver = null)
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

        public IUIVirtualListItem Create(int itemType, Transform parent)
        {
            if (!_prefabs.TryGetValue(itemType, out var prefab) || prefab == null)
                throw new InvalidOperationException($"没有找到 Item 模板，type={itemType}");

            var instance = UnityEngine.Object.Instantiate(prefab, parent, false);                        
            var item = _itemResolver != null
                ? _itemResolver(itemType, instance)
                : instance.GetComponent<IUIVirtualListItem>();

            if (item == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Item Prefab {prefab.name} 根节点没有实现 IUIVirtualListItem。 ");
            }

            return item;
        }

        public void Destroy(IUIVirtualListItem item)
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
