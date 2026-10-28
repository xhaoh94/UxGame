using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ux
{
    public enum UGUIVirtualPageState { Idle, Loading, Error, Completed, Disposed }
    public enum UGUIVirtualPageItemKind { Data, LoadMore, Loading, Error, End }

    /// <summary>一页业务结果。页号从 0 开始，是否还有下一页由服务端/加载函数明确提供。</summary>
    public sealed class UGUIVirtualPage
    {
        public IReadOnlyList<object> Items { get; }
        public bool HasMore { get; }
        public UGUIVirtualPage(IEnumerable<object> items, bool hasMore)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            Items = new List<object>(items).AsReadOnly();
            HasMore = hasMore;
        }
        public static UGUIVirtualPage Create<T>(IEnumerable<T> items, bool hasMore)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var copy = new List<object>();
            foreach (var item in items) copy.Add(item);
            return new UGUIVirtualPage(copy, hasMore);
        }
    }

    /// <summary>游标分页结果。HasMore=true 时必须返回与上一次不同的非空 NextCursor。</summary>
    public sealed class UGUIVirtualCursorPage
    {
        public IReadOnlyList<object> Items { get; }
        public string NextCursor { get; }
        public bool HasMore { get; }
        public UGUIVirtualCursorPage(IEnumerable<object> items, string nextCursor, bool hasMore)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            Items = new List<object>(items).AsReadOnly();
            NextCursor = nextCursor;
            HasMore = hasMore;
        }
        public static UGUIVirtualCursorPage Create<T>(IEnumerable<T> items, string nextCursor, bool hasMore)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var rows = new List<object>();
            foreach (var item in items) rows.Add(item);
            return new UGUIVirtualCursorPage(rows, nextCursor, hasMore);
        }
    }

    /// <summary>分页 Renderer 接收的行。状态行不是业务数据，DataIndex=-1。</summary>
    public sealed class UGUIVirtualPageItem
    {
        public UGUIVirtualPageItemKind Kind { get; internal set; }
        public bool IsStatus => Kind != UGUIVirtualPageItemKind.Data;
        public int DataIndex { get; }
        public object Data { get; }
        public string Message { get; internal set; }
        public Exception Error { get; internal set; }
        internal UGUIVirtualPageItem(int index, object data)
        { DataIndex = index; Data = data; Kind = UGUIVirtualPageItemKind.Data; }
        internal UGUIVirtualPageItem() { DataIndex = -1; Kind = UGUIVirtualPageItemKind.LoadMore; }
    }

    /// <summary>
    /// 纯 C# 分页数据源：业务行后保留一条状态行。只在主线程改 UI，不注册 Unity 生命周期。
    /// 调用方在更新中 Tick；释放/刷新取消旧代请求，未响应取消的旧结果也不会写回。
    /// </summary>
    public sealed class UGUIVirtualPagedAdapter : IDisposable
    {
        private readonly UGUIVirtualList _list;
        private readonly int _ownerThread;
        private readonly int _pageSize;
        private Func<int, int, CancellationToken, Task<UGUIVirtualPage>> _loader;
        private Func<string, int, CancellationToken, Task<UGUIVirtualCursorPage>> _cursorLoader;
        private string _cursor;
        private readonly List<UGUIVirtualPageItem> _rows = new();
        private UGUIVirtualPageItem _footer = new UGUIVirtualPageItem();
        private Operation _operation;
        private ulong _generation;
        private ulong _expectedRevision;
        private bool _attached;
        private bool _changing;
        private bool _disposed;

        private UGUIVirtualPagedAdapter(UGUIVirtualList list, int pageSize)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
            _pageSize = pageSize;
            _ownerThread = Thread.CurrentThread.ManagedThreadId;
            SetStatus(UGUIVirtualPageState.Idle);
        }

        public UGUIVirtualPagedAdapter(UGUIVirtualList list,
            Func<int, int, CancellationToken, Task<UGUIVirtualPage>> loader, int pageSize = 20)
            : this(list, pageSize)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary>游标分页时的当前游标；页号分页恒为 null。</summary>
        public string Cursor => _cursor;
        public bool IsCursorMode => _cursorLoader != null;

        public UGUIVirtualPagedAdapter(UGUIVirtualList list,
            Func<string, int, CancellationToken, Task<UGUIVirtualCursorPage>> cursorLoader, int pageSize = 20)
            : this(list, pageSize)
        {
            _cursorLoader = cursorLoader ?? throw new ArgumentNullException(nameof(cursorLoader));
        }

        public UGUIVirtualPageState State { get; private set; }
        public int LoadedCount => _rows.Count;
        public int NextPageIndex { get; private set; }
        public int PageSize => _pageSize;
        public bool IsLoading => _operation != null;
        public bool IsDisposed => _disposed;
        public UGUIVirtualPageItem Footer => _footer;
        public Exception LastError { get; private set; }
        public Exception LastPresentationError { get; private set; }
        public Exception LastCancellationError { get; private set; }
        public bool OwnsCurrentData => !_disposed && _attached && !_list.IsDisposedOrDisposing &&
            (_changing || _list.DataRevision == _expectedRevision);

        /// <summary>接管 Core 并显示“点击加载更多”状态项，不立即请求业务数据。</summary>
        public void Initialize()
        {
            CheckThread();
            if (_disposed) throw new ObjectDisposedException(nameof(UGUIVirtualPagedAdapter));
            if (_attached) { CheckOwnership(); return; }
            _list.ValidateDataMutation();
            _changing = true;
            ulong revision = _list.DataRevision;
            try
            {
                _attached = true;
                _list.SetDatas(new[] { _footer });
                _expectedRevision = _list.DataRevision;
            }
            catch
            {
                if (_list.DataRevision == revision) _attached = false;
                else _expectedRevision = _list.DataRevision;
                throw;
            }
            finally { _changing = false; }
        }

        public Task<bool> LoadNextPageAsync() => StartRequest(false);
        public Task<bool> RetryAsync() => StartRequest(true);

        /// <summary>取消旧请求并清空已加载结果，从第 0 页重新请求；显式刷新可以重新接管 Core。</summary>
        public Task<bool> RefreshAsync()
        {
            CheckThread();
            if (!Alive() || _changing || !_list.CanMutateData) return Task.FromResult(false);
            _list.ValidateDataMutation();
            var previousRows = new List<UGUIVirtualPageItem>(_rows);
            var previousCursor = _cursor;
            var previousFooter = _footer;
            var previousState = State;
            var previousError = LastError;
            int previousPage = NextPageIndex;
            bool wasAttached = _attached;
            ulong revision = _list.DataRevision;
            _changing = true;
            try
            {
                CancelCurrent();
                // 取消回调可能同步释放适配器或改变 Core；只以取消完成后的版本判断本次提交。
                if (!Alive()) return Task.FromResult(false);
                revision = _list.DataRevision;
                _rows.Clear();
                NextPageIndex = 0;
                _cursor = null;
                LastError = null;
                LastPresentationError = null;
                _footer = new UGUIVirtualPageItem();
                SetStatus(UGUIVirtualPageState.Idle);
                _attached = true;
                _list.SetDatas(new[] { _footer });
                _expectedRevision = _list.DataRevision;
            }
            catch (Exception ex)
            {
                LastPresentationError = ex;
                if (_list.DataRevision == revision)
                {
                    _rows.Clear();
                    _rows.AddRange(previousRows);
                    _footer = previousFooter;
                    _cursor = previousCursor;
                    NextPageIndex = previousPage;
                    _attached = wasAttached;
                    LastError = previousError;
                    SetStatus(previousState == UGUIVirtualPageState.Loading ? UGUIVirtualPageState.Idle : previousState);
                    if (wasAttached) _list.QueueItemRebind(LoadedCount);
                }
                else _expectedRevision = _list.DataRevision;
                throw;
            }
            finally { _changing = false; }
            return StartRequest(false);
        }

        /// <summary>每帧调用。先提交延期结果，再按实际可见数据索引预加载；只返回本帧新启动的任务。</summary>
        public Task<bool> Tick(bool autoLoad = true, int preloadItems = 5)
        {
            CheckThread();
            if (preloadItems < 0) throw new ArgumentOutOfRangeException(nameof(preloadItems));
            if (!Alive() || _changing) return null;
            if (_operation != null && _operation.Ready && _list.CanMutateData) Pump(_operation);
            if (!Alive() || !autoLoad || _operation != null || State != UGUIVirtualPageState.Idle || !_attached || LastPresentationError != null)
                return null;
            if (!_list.CanMutateData) return null;
            if (_expectedRevision != _list.DataRevision)
            {
                LastError = new InvalidOperationException("分页数据投影被直接修改，请 RefreshAsync 重新接管。");
                State = UGUIVirtualPageState.Error;
                return null;
            }
            int last = _list.LastVisibleIndex;
            if (last < 0 || last < Math.Max(0, LoadedCount - 1 - preloadItems)) return null;
            return StartRequest(false);
        }

        private Task<bool> StartRequest(bool retry)
        {
            CheckThread();
            if (!Alive()) return Task.FromResult(false);
            if (_operation != null) return _operation.Completion.Task;
            if (_changing || !_list.CanMutateData) return Task.FromResult(false);
            if (State == UGUIVirtualPageState.Completed || (State == UGUIVirtualPageState.Error && !retry) ||
                (retry && State != UGUIVirtualPageState.Error)) return Task.FromResult(false);
            if (!_attached) Initialize();
            if (!Alive()) return Task.FromResult(false);
            CheckOwnership();
            var operation = new Operation(_generation, NextPageIndex);
            _operation = operation;
            LastError = null;
            LastPresentationError = null;
            SetStatus(UGUIVirtualPageState.Loading);
            _changing = true;
            try { _list.RefreshItem(LoadedCount); }
            catch (Exception ex)
            {
                LastPresentationError = LastError = ex;
                SetStatus(UGUIVirtualPageState.Error);
                _list.QueueItemRebind(LoadedCount);
                operation.LoaderFinished = true;
                Finish(operation, false, ex);
                return operation.Completion.Task;
            }
            finally { _changing = false; }
            if (Current(operation)) _ = Fetch(operation);
            else
            {
                operation.LoaderFinished = true;
                operation.Completion.TrySetResult(false);
                DisposeSource(operation);
            }
            return operation.Completion.Task;
        }

        private async Task Fetch(Operation operation)
        {
            try
            {
                Task<UGUIVirtualPage> task;
                if (_cursorLoader != null)
                {
                    var cursorLoader = _cursorLoader;
                    string cursor = _cursor;
                    var cursorTask = cursorLoader(cursor, _pageSize, operation.Source.Token);
                    if (cursorTask == null) throw new InvalidOperationException("游标分页加载函数返回了 null Task。");
                    task = WrapCursor(cursorTask, cursor);
                }
                else
                {
                    var loader = _loader;
                    task = loader(operation.Page, _pageSize, operation.Source.Token);
                }
                if (task == null) throw new InvalidOperationException("分页加载函数返回了 null Task。");
                if (!task.IsCompleted && SynchronizationContext.Current == null)
                {
                    // 没有主线程调度环境时不能把 UI 继续执行到线程池；同时观察被放弃任务的错误。
                    _ = ObserveAbandoned(task);
                    try { operation.Source.Cancel(); }
                    catch (Exception ex) { LastCancellationError = ex; }
                    throw new InvalidOperationException("异步分页需要 Unity 主线程 SynchronizationContext，请在 Start/页面生命周期中调用。");
                }
                operation.Result = await task;
                if (!Current(operation)) return;
                CheckThread();
                if (operation.Result == null) throw new InvalidOperationException("分页加载函数返回了 null 结果。");
                if (operation.Result.Items.Count == 0 && operation.Result.HasMore)
                    throw new InvalidOperationException("空页不能同时声明 HasMore=true，否则可能无限加载。");
            }
            catch (Exception ex)
            {
                if (Current(operation)) operation.Error = ex;
            }
            finally
            {
                operation.LoaderFinished = true;
                if (Current(operation))
                {
                    operation.Ready = true;
                    if (Thread.CurrentThread.ManagedThreadId == _ownerThread && _list.CanMutateData && !_changing) Pump(operation);
                }
                else operation.Completion.TrySetResult(false);
                DisposeSource(operation);
            }
        }

        private async Task<UGUIVirtualPage> WrapCursor(Task<UGUIVirtualCursorPage> task, string previousCursor)
        {
            var page = await task;
            if (page == null) throw new InvalidOperationException("游标分页加载函数返回了 null 结果。");
            if (page.HasMore && string.Equals(page.NextCursor, previousCursor, StringComparison.Ordinal))
                throw new InvalidOperationException("HasMore=true 时 NextCursor 必须变化，否则可能无限加载。");
            _pendingCursor = page.NextCursor;
            return new UGUIVirtualPage(page.Items, page.HasMore);
        }

        private string _pendingCursor;

        private void Pump(Operation operation)
        {
            if (!Current(operation) || !operation.Ready || _changing || !_list.CanMutateData) return;
            Exception presentation = null;
            bool success = false;
            _changing = true;
            try
            {
                // 不能因 _changing=true 而忽略实际版本不匹配。
                if (_expectedRevision != _list.DataRevision)
                {
                    LastError = new InvalidOperationException("分页期间底层数据被直接修改，请 RefreshAsync 重新接管数据源。");
                    State = UGUIVirtualPageState.Error;
                }
                else if (operation.Error != null)
                {
                    LastError = operation.Error;
                    SetStatus(UGUIVirtualPageState.Error);
                    _list.RefreshItem(LoadedCount);
                }
                else
                {
                    int oldCount = _rows.Count;
                    int oldPage = NextPageIndex;
                    var added = new List<UGUIVirtualPageItem>();
                    foreach (var data in operation.Result.Items)
                        added.Add(new UGUIVirtualPageItem(oldCount + added.Count, data));
                    if (oldPage == int.MaxValue) throw new InvalidOperationException("分页页号已超过支持范围。");
                    ulong revision = _list.DataRevision;
                    bool anchorIsFooter = _list.FirstVisibleIndex >= oldCount;
                    bool preserve = _list.PreserveScrollAnchor;
                    bool follow = _list.KeepEndOnDataChange;
                    _rows.AddRange(added);
                    NextPageIndex++;
                    if (_cursorLoader != null) _cursor = _pendingCursor;
                    SetStatus(operation.Result.HasMore ? UGUIVirtualPageState.Idle : UGUIVirtualPageState.Completed);
                    _list.KeepEndOnDataChange = false;
                    if (anchorIsFooter) _list.PreserveScrollAnchor = false;
                    try
                    {
                        if (added.Count > 0) _list.InsertDatas(oldCount, added);
                        else _list.RefreshItem(oldCount);
                        _expectedRevision = _list.DataRevision;
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        if (added.Count > 0 && _list.DataRevision == revision)
                        {
                            _rows.RemoveRange(oldCount, added.Count);
                            NextPageIndex = oldPage;
                            LastError = ex;
                            SetStatus(UGUIVirtualPageState.Error);
                        }
                        else _expectedRevision = _list.DataRevision; // 已提交的页不可因渲染异常重复追加。
                        _list.QueueItemRebind(LoadedCount);
                        throw;
                    }
                    finally
                    {
                        _list.PreserveScrollAnchor = preserve;
                        _list.KeepEndOnDataChange = follow;
                    }
                }
            }
            catch (Exception ex)
            {
                LastPresentationError = presentation = ex;
                if (State == UGUIVirtualPageState.Loading)
                {
                    LastError = ex;
                    SetStatus(UGUIVirtualPageState.Error);
                    _list.QueueItemRebind(LoadedCount);
                }
            }
            finally { _changing = false; }
            if (!Current(operation)) operation.Completion.TrySetResult(false);
            else Finish(operation, success, presentation);
        }

        /// <summary>修复 Renderer 后重试当前视图，不重新请求已经提交的页。</summary>
        public bool RefreshPresentation()
        {
            CheckThread();
            if (!Alive() || _changing || !_list.CanMutateData) return false;
            CheckOwnership();
            _list.RefreshLayout();
            LastPresentationError = null;
            return true;
        }

        private void Finish(Operation operation, bool success, Exception presentation)
        {
            if (ReferenceEquals(_operation, operation)) _operation = null;
            if (presentation != null) operation.Completion.TrySetException(presentation);
            else operation.Completion.TrySetResult(success);
            DisposeSource(operation);
        }

        private void SetStatus(UGUIVirtualPageState state)
        {
            State = state;
            _footer.Error = state == UGUIVirtualPageState.Error ? LastError : null;
            switch (state)
            {
                case UGUIVirtualPageState.Loading: _footer.Kind = UGUIVirtualPageItemKind.Loading; _footer.Message = "正在加载…"; break;
                case UGUIVirtualPageState.Error: _footer.Kind = UGUIVirtualPageItemKind.Error; _footer.Message = "加载失败，点击重试"; break;
                case UGUIVirtualPageState.Completed: _footer.Kind = UGUIVirtualPageItemKind.End; _footer.Message = LoadedCount == 0 ? "暂无数据" : "没有更多了"; break;
                default: _footer.Kind = UGUIVirtualPageItemKind.LoadMore; _footer.Message = "点击加载更多"; break;
            }
        }

        private bool Current(Operation operation) => !_disposed && !_list.IsDisposedOrDisposing &&
            operation.Generation == _generation && ReferenceEquals(_operation, operation);
        private bool Alive()
        {
            if (_disposed) return false;
            if (_list.IsDisposedOrDisposing) { Dispose(); return false; }
            return true;
        }
        private void CheckOwnership()
        {
            if (_attached && _expectedRevision != _list.DataRevision)
                throw new InvalidOperationException("分页数据投影被直接修改，请 RefreshAsync 重新接管。");
        }
        private void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _ownerThread)
                throw new InvalidOperationException("分页适配器必须在创建它的 Unity 主线程调用。");
        }
        private void CancelCurrent()
        {
            _generation++;
            var old = _operation;
            _operation = null;
            if (old == null) return;
            lock (old)
            {
                old.Cancelling = true;
                try { if (!old.SourceDisposed) old.Source.Cancel(); }
                catch (Exception ex) { LastCancellationError = ex; }
                finally { old.Cancelling = false; }
                old.Completion.TrySetResult(false);
            }
            DisposeSource(old);
        }
        private static void DisposeSource(Operation operation)
        {
            lock (operation)
            {
                if (!operation.SourceDisposed && !operation.Cancelling && operation.LoaderFinished && operation.Completion.Task.IsCompleted)
                { operation.SourceDisposed = true; operation.Source.Dispose(); }
            }
        }
        private static async Task ObserveAbandoned(Task task)
        { try { await task.ConfigureAwait(false); } catch { /* 没有 UI 上下文，不能从后台写回。 */ } }

        public void Dispose()
        {
            CheckThread();
            if (_disposed) return;
            _disposed = true;
            CancelCurrent();
            State = UGUIVirtualPageState.Disposed;
            _loader = null;
            _rows.Clear();
        }

        private sealed class Operation
        {
            public readonly ulong Generation;
            public readonly int Page;
            public readonly CancellationTokenSource Source = new CancellationTokenSource();
            public readonly TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>();
            public UGUIVirtualPage Result;
            public string NextCursor;
            public Exception Error;
            public volatile bool Ready;
            public volatile bool LoaderFinished;
            public bool SourceDisposed;
            public bool Cancelling;
            public Operation(ulong generation, int page) { Generation = generation; Page = page; }
        }
    }
}
