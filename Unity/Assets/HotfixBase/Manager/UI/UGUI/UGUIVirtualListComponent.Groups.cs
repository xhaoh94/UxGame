using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public sealed partial class UGUIVirtualListComponent
    {
        [SerializeField] private bool _stickyGroupHeaders = true;
        [SerializeField] private bool _showEmptyGroups = true;
        private UGUIVirtualGroupAdapter _groupAdapter;
        private UGUIVirtualStickyHeader _stickyHeader;
        private UGUIVirtualStickyHeader _retiringSticky;

        /// <summary>可选吸顶副本 Renderer；未设置时回退到 ItemRenderer。data.IsSticky 可区分副本。</summary>
        public Action<int, object, GameObject> StickyHeaderRenderer { get; set; }
        public event Action<int, object, GameObject> StickyHeaderRecycled;
        public string CurrentStickyGroupId => _stickyHeader?.CurrentGroupId;
        public UGUIVirtualGroupAdapter GroupAdapter
        {
            get
            {
                RequireGroupedMode();
                Initialize();
                if (_groupAdapter == null) throw new InvalidOperationException("初始化后改变了类型，请 Release 后重新初始化。");
                return _groupAdapter;
            }
        }
        public void SetGroups(IEnumerable<UGUIVirtualGroup> groups)
        {
            GroupAdapter.SetGroups(groups);
            _stickyHeader?.Refresh();
        }
        public bool UpdateGroupHeader(string id, object data)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.UpdateHeader(id, data); }
        public bool UpdateGroupItem(string id, int index, object data)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.UpdateItem(id, index, data); }
        public bool InsertGroup(int index, UGUIVirtualGroup group)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.InsertGroup(index, group); }
        public bool AddGroup(UGUIVirtualGroup group)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.AddGroup(group); }
        public bool RemoveGroup(string id)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.RemoveGroup(id); }
        public bool MoveGroup(string id, int index)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.MoveGroup(id, index); }
        public bool InsertGroupItem(string groupId, int index, object data)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.InsertItem(groupId, index, data); }
        public bool AddGroupItem(string groupId, object data)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.AddItem(groupId, data); }
        public bool RemoveGroupItem(string groupId, int index)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.RemoveItem(groupId, index); }

        public bool ScrollToGroup(string id, UGUIVirtualListAlignment alignment = UGUIVirtualListAlignment.Start)
        { RequireGroupedMode(); return _groupAdapter != null && _groupAdapter.ScrollToGroup(id, alignment); }
        public bool TryGetGroupAdapter(out UGUIVirtualGroupAdapter adapter) { adapter = _groupAdapter; return adapter != null; }
        public bool TryGetStickyHeaderToken(out UGUIVirtualStickyHeaderToken token)
        {
            if (_stickyHeader != null) return _stickyHeader.TryGetToken(out token);
            token = default;
            return false;
        }
        public bool NotifyStickyHeaderSizeChanged(UGUIVirtualStickyHeaderToken token)
            => _stickyHeader != null && _stickyHeader.NotifySizeChanged(token);
        public void RefreshStickyHeader() => _stickyHeader?.Refresh();
        private void RequireGroupedMode()
        {
            if (_listType != UGUIVirtualListType.Grouped)
                throw new InvalidOperationException("请先在 Inspector 中选择分组列表，再使用分组接口。");
        }
        private void OnGroupScrolled(Vector2 _) => TickGrouping();
        private void TickGrouping()
        {
            if (_listType == UGUIVirtualListType.Grouped) _stickyHeader?.Tick();
        }
        private void DisposeOwnedGrouping()
        {
            if (_scrollRect != null) _scrollRect.onValueChanged.RemoveListener(OnGroupScrolled);
            var sticky = _stickyHeader;
            _stickyHeader = null;
            var groups = _groupAdapter;
            _groupAdapter = null;
            if (sticky != null) _retiringSticky = sticky;
            try { sticky?.Dispose(); }
            finally
            {
                groups?.Dispose();
                if (_retiringSticky != null && _retiringSticky.IsDisposalComplete) _retiringSticky = null;
            }
        }
        private Exception DisposeOwnedAdapters()
        {
            Exception failure = null;
            try { DisposeOwnedGrouping(); } catch (Exception ex) { failure = ex; }
            try { DisposeOwnedTree(); } catch (Exception ex) { failure ??= ex; }
            try { DisposeOwnedPaging(); } catch (Exception ex) { failure ??= ex; }
            return failure;
        }
    }
}
