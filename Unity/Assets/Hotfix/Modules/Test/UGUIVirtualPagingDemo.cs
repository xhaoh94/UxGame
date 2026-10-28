using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.UI
{
    public sealed class UGUIVirtualPagingDemo : MonoBehaviour
    {
        [SerializeField] private UGUIVirtualListComponent _list;
        [Min(0)] [SerializeField] private int _totalItems = 95;
        [Min(0)] [SerializeField] private int _delayMilliseconds = 600;
        [Tooltip("从 0 开始；-1 不模拟失败。")]
        [SerializeField] private int _failPage = 2;
        [SerializeField] private bool _failOnce = true;
        private readonly HashSet<int> _failed = new();
        private readonly Dictionary<GameObject, Text> _texts = new();
        private UGUIVirtualPagedAdapter _adapter;
        private bool _subscribed;

        private async void Start()
        {
            if (_list == null) _list = GetComponent<UGUIVirtualListComponent>();
            if (_list == null || _list.ListType != UGUIVirtualListType.Paged)
            {
                Debug.LogWarning("请先选择分页列表，或使用 Create UGUI Paging Demo 菜单。", this);
                return;
            }
            _list.ItemTypeProvider = GetItemType;
            _list.ItemRenderer = BindItem;
            _list.ItemClicked += OnItemClicked;
            _list.ItemRecycled += OnItemRecycled;
            _subscribed = true;
            try
            {
                var first = _list.SetPageLoader(LoadPage);
                _list.TryGetPagedAdapter(out _adapter);
                await first;
            }
            catch (Exception ex) { if (this != null) Debug.LogException(ex, this); }
        }

        private async Task<UGUIVirtualPage> LoadPage(int page, int pageSize, CancellationToken token)
        {
            await Task.Delay(_delayMilliseconds, token);
            token.ThrowIfCancellationRequested();
            if (page == _failPage && (!_failOnce || _failed.Add(page)))
                throw new InvalidOperationException("Demo 模拟网络失败，点击尾部状态项可重试同一页。");
            int first = checked(page * pageSize);
            var rows = new List<DemoData>();
            for (int i = first; i < Math.Min(_totalItems, first + pageSize); i++)
            {
                string text = $"分页数据 ID {i}（第 {page + 1} 页）";
                for (int j = 0; j < i % 4; j++) text += " 这段内容用于测试不定高分页。";
                rows.Add(new DemoData(i, text));
            }
            return UGUIVirtualPage.Create(rows, first + rows.Count < _totalItems);
        }

        private int GetItemType(int index, object data) => ((UGUIVirtualPageItem)data).IsStatus ? 1 : 0;
        private void BindItem(int index, object data, GameObject view)
        {
            var row = (UGUIVirtualPageItem)data;
            if (!_texts.TryGetValue(view, out var text) || text == null)
            {
                text = view.GetComponentInChildren<Text>(true);
                if (text == null) throw new InvalidOperationException("分页模板需要 UGUI Text 子节点。");
                _texts[view] = text;
            }
            text.text = row.IsStatus ? row.Message : ((DemoData)row.Data).Text;
            text.alignment = row.IsStatus ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            text.color = row.Kind == UGUIVirtualPageItemKind.Error ? new Color(1f, 0.6f, 0.4f) : Color.white;
            text.verticalOverflow = row.IsStatus || _list.LayoutMode != UGUIVirtualListLayoutMode.VariableLinear
                ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
            view.name = row.IsStatus ? "Page Footer " + row.Kind : "Page Item " + row.DataIndex;
            if (_list.LayoutMode == UGUIVirtualListLayoutMode.VariableLinear)
                ((RectTransform)view.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    row.IsStatus ? 64f : Mathf.Max(76f, Mathf.Ceil(text.preferredHeight) + 20f));
        }
        private void OnItemClicked(int index, object data)
        {
            var row = (UGUIVirtualPageItem)data;
            if (!row.IsStatus) Debug.Log($"[UGUIVirtualPagingDemo] 点击业务数据 {row.DataIndex}, id={((DemoData)row.Data).Id}", this);
        }
        private void OnItemRecycled(int index, object data, GameObject view)
        {
            if (_texts.TryGetValue(view, out var text) && text != null) text.text = string.Empty;
        }

        [ContextMenu("测试/请求下一页")]
        public void LoadNext() => Run(() => _list.LoadNextPageAsync());
        [ContextMenu("测试/重试失败页")]
        public void Retry() => Run(() => _list.RetryPageAsync());
        [ContextMenu("测试/刷新并从第0页重新加载")]
        public void Reload()
        {
            _failed.Clear();
            Run(() => _list.RefreshPagesAsync());
        }
        [ContextMenu("测试/滚动到末尾触发预加载")]
        public void ScrollToFooter()
        {
            if (Ready()) _list.ScrollToView(_list.Count - 1, UGUIVirtualListAlignment.End);
        }
        private bool Ready() => Application.isPlaying && _list != null && _list.IsInitialized && _adapter != null && !_adapter.IsDisposed;
        private async void Run(Func<Task<bool>> request)
        {
            if (!Ready()) return;
            try { await request(); }
            catch (Exception ex) { if (this != null) Debug.LogException(ex, this); }
        }
        private void OnDestroy()
        {
            bool owns = _subscribed && _list != null && _list.TryGetPagedAdapter(out var current) && ReferenceEquals(current, _adapter);
            if (_subscribed && _list != null)
            {
                _list.ItemClicked -= OnItemClicked;
                _list.ItemRecycled -= OnItemRecycled;
                if (_list.ItemRenderer == BindItem) _list.ItemRenderer = null;
                if (_list.ItemTypeProvider == GetItemType) _list.ItemTypeProvider = null;
            }
            _adapter = null;
            _texts.Clear();
            if (owns && _list != null) _list.Release();
        }
        private sealed class DemoData
        {
            public readonly int Id;
            public readonly string Text;
            public DemoData(int id, string text) { Id = id; Text = text; }
        }
    }
}
