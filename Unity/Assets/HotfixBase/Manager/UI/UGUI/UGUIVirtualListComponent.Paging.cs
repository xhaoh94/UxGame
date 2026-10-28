using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    public sealed partial class UGUIVirtualListComponent
    {
        [Min(1)] [SerializeField] private int _pageSize = 20;
        [SerializeField] private bool _autoLoadPages = true;
        [Min(0)] [SerializeField] private int _pagePreloadItems = 5;
        [SerializeField] private bool _pageStatusClick = true;
        private UGUIVirtualPagedAdapter _pagedAdapter;
        private bool _bindingPageLoader;

        public int LoadedPageDataCount => _pagedAdapter?.LoadedCount ?? 0;
        public UGUIVirtualPageState PageState => _pagedAdapter?.State ?? UGUIVirtualPageState.Idle;
        public string PageCursor => _pagedAdapter?.Cursor;
        public bool IsCursorPaging => _pagedAdapter != null && _pagedAdapter.IsCursorMode;
        public bool TryGetPagedAdapter(out UGUIVirtualPagedAdapter adapter)
        { adapter = _pagedAdapter; return adapter != null; }

        /// <summary>绑定分页函数，页号从 0 开始。需要重新打开时再次绑定；不会从 Inspector 生成业务数据。</summary>
        public Task<bool> SetPageLoader(Func<int, int, CancellationToken, Task<UGUIVirtualPage>> loader, bool loadFirstPage = true)
        {
            RequirePagedMode();
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            if (_bindingPageLoader) throw new InvalidOperationException("不能在绑定/释放分页加载器的回调中重入设置加载器。");
            _bindingPageLoader = true;
            try
            {
                Initialize();
                _list.ValidateDataMutation();
                var core = _list;
                DisposeOwnedPaging();
                // 旧请求的取消回调可以关闭页面，不能继续用已被清空的 _list 绑定新加载器。
                if (_destroyed || _releasing || !ReferenceEquals(_list, core) || core.IsDisposedOrDisposing)
                    return Task.FromResult(false);
                var adapter = new UGUIVirtualPagedAdapter(core, loader, _pageSize);
                _pagedAdapter = adapter;
                adapter.Initialize();
                if (!ReferenceEquals(_pagedAdapter, adapter) || adapter.IsDisposed || _list == null)
                    return Task.FromResult(false);
                return loadFirstPage ? adapter.LoadNextPageAsync() : Task.FromResult(false);
            }
            finally { _bindingPageLoader = false; }
        }

        /// <summary>游标分页：首次传入空串/nil，之后由适配器持有 NextCursor 继续请求。</summary>
        public Task<bool> SetCursorPageLoader(
            Func<string, int, System.Threading.CancellationToken, Task<UGUIVirtualCursorPage>> loader,
            bool loadFirstPage = true)
        {
            RequirePagedMode();
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            if (_bindingPageLoader) throw new InvalidOperationException("不能在绑定/释放分页加载器的回调中重入设置加载器。");
            _bindingPageLoader = true;
            try
            {
                Initialize();
                _list.ValidateDataMutation();
                var core = _list;
                DisposeOwnedPaging();
                if (_destroyed || _releasing || !ReferenceEquals(_list, core) || core.IsDisposedOrDisposing)
                    return Task.FromResult(false);
                var adapter = new UGUIVirtualPagedAdapter(core, loader, _pageSize);
                _pagedAdapter = adapter;
                adapter.Initialize();
                if (!ReferenceEquals(_pagedAdapter, adapter) || adapter.IsDisposed || _list == null)
                    return Task.FromResult(false);
                return loadFirstPage ? adapter.LoadNextPageAsync() : Task.FromResult(false);
            }
            finally { _bindingPageLoader = false; }
        }

        // 请求入口不自动 Initialize，避免释放后晚到的 UI 回调重新创建列表。
        public Task<bool> LoadNextPageAsync()
        { RequirePagedMode(); return _pagedAdapter?.LoadNextPageAsync() ?? Task.FromResult(false); }
        public Task<bool> RetryPageAsync()
        { RequirePagedMode(); return _pagedAdapter?.RetryAsync() ?? Task.FromResult(false); }
        public Task<bool> RefreshPagesAsync()
        { RequirePagedMode(); return _pagedAdapter?.RefreshAsync() ?? Task.FromResult(false); }
        public bool RefreshPagePresentation()
        { RequirePagedMode(); return _pagedAdapter != null && _pagedAdapter.RefreshPresentation(); }

        private void RequirePagedMode()
        {
            if (_listType != UGUIVirtualListType.Paged)
                throw new InvalidOperationException("请先在 Inspector 中选择分页列表，再绑定分页加载器。");
        }
        private void TickPaging()
        {
            if (_listType != UGUIVirtualListType.Paged || _pagedAdapter == null) return;
            ObservePageTask(_pagedAdapter.Tick(_autoLoadPages, _autoLoadPages ? _pagePreloadItems : 0));
        }
        private async void ObservePageTask(Task<bool> task)
        {
            if (task == null) return;
            try { await task; }
            catch (Exception ex) { if (!_destroyed) Debug.LogException(ex, this); }
        }
        private void DisposeOwnedPaging()
        {
            var adapter = _pagedAdapter;
            _pagedAdapter = null;
            adapter?.Dispose();
        }
    }
}
