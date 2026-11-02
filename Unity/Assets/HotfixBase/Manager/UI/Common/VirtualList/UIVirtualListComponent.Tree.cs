using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public enum UIVirtualListType
    {
        Flat = 0,
        Tree = 1,
        Paged = 2,
        Grouped = 3
    }

    public sealed partial class UIVirtualListComponent
    {
        [SerializeField] private UIVirtualListType _listType;
        [SerializeField, HideInInspector] private bool _listTypeConfigured;
        [Tooltip("树形模式中，点击有子节点的行自动展开/折叠；ItemClicked 在切换之后触发。")]
        [SerializeField] private bool _treeToggleOnClick = true;
        private UIVirtualTreeAdapter _treeAdapter;

        public UIVirtualListType ListType => _listType;
        public bool IsListTypeConfigured => _listTypeConfigured;
        public bool ToggleTreeOnClick => _treeToggleOnClick;
        public UIVirtualListAxis EffectiveAxis => _listType == UIVirtualListType.Tree || _listType == UIVirtualListType.Grouped
            ? UIVirtualListAxis.Vertical : _axis;

        /// <summary>树模式由组件拥有此适配器，调用方不要单独 Dispose；释放时使用组件 Release。</summary>
        public UIVirtualTreeAdapter TreeAdapter
        {
            get
            {
                RequireTreeMode();
                Initialize();
                if (_treeAdapter == null)
                    throw new InvalidOperationException("初始化后改变了列表类型，请先 Release 再重新初始化。");
                return _treeAdapter;
            }
        }

        /// <summary>
        /// 配置列表类型。已有非空数据时先 Release；允许空列表初始化后的类型切换以兼容旧 Demo。
        /// 不会自动生成任何业务树数据。
        /// </summary>
        public void ConfigureListType(UIVirtualListType type)
        {
            if (_destroyed) throw new ObjectDisposedException(nameof(UIVirtualListComponent));
            if (_initializing || _releasing || (_retiringList != null && !_retiringList.IsDisposalComplete) ||
                (_retiringSticky != null && !_retiringSticky.IsDisposalComplete))
                throw new InvalidOperationException("列表正在初始化或释放，不能修改列表类型。");
            if (type != UIVirtualListType.Flat && type != UIVirtualListType.Tree && type != UIVirtualListType.Paged && type != UIVirtualListType.Grouped)
                throw new ArgumentOutOfRangeException(nameof(type));
            if (_listType == type) { _listTypeConfigured = true; return; }
            if (_list != null)
            {
                _list.ValidateDataMutation();
                if (_list.Count != 0)
                    throw new InvalidOperationException("已有数据时不能直接切换列表类型，请先 Release。");
                Release();
            }
            _listType = type;
            _listTypeConfigured = true;
        }

        public void SetTreeRoots(IEnumerable<UIVirtualTreeNode> roots, bool preserveExpansion = false)
            => TreeAdapter.SetRoots(roots, preserveExpansion);

        // 单节点入口不自动初始化，防止页面释放后晚到的回调重新创建空列表。
        public bool ExpandTreeNode(string id) { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.Expand(id); }
        public bool CollapseTreeNode(string id) { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.Collapse(id); }
        public bool ToggleTreeNode(string id) { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.Toggle(id); }
        public bool AddTreeRoot(UIVirtualTreeNode node)
        { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.AddRoot(node); }
        public bool InsertTreeRoot(int index, UIVirtualTreeNode node)
        { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.InsertRoot(index, node); }
        public bool AddTreeNode(string parentId, UIVirtualTreeNode node)
        { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.AddChild(parentId, node); }
        public bool InsertTreeNode(string parentId, int index, UIVirtualTreeNode node)
        { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.InsertChild(parentId, index, node); }
        public bool RemoveTreeNode(string id)
        { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.RemoveNode(id); }
        public bool MoveTreeNode(string id, string parentId, int index)
        { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.MoveNode(id, parentId, index); }

        public bool SetTreeNodeExpanded(string id, bool expanded)
        { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.SetExpanded(id, expanded); }
        public bool UpdateTreeNodeData(string id, object data)
        { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.UpdateNodeData(id, data); }
        public bool RefreshTreeNode(string id) { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.RefreshNode(id); }
        public bool ScrollToTreeNode(string id, bool expandAncestors = true,
            UIVirtualListAlignment alignment = UIVirtualListAlignment.Nearest)
        { RequireTreeMode(); return _treeAdapter != null && _treeAdapter.ScrollToNode(id, expandAncestors, alignment); }
        public bool TryGetTreeAdapter(out UIVirtualTreeAdapter tree)
        {
            tree = _treeAdapter;
            return tree != null;
        }
        public int GetTreeVisibleIndex(string id) => _treeAdapter?.GetVisibleIndex(id) ?? -1;
        public bool TryGetTreeItem(string id, out UIVirtualTreeItem item)
        {
            if (_treeAdapter != null) return _treeAdapter.TryGetItem(id, out item);
            item = null;
            return false;
        }

        private void RequireTreeMode()
        {
            if (_listType != UIVirtualListType.Tree)
                throw new InvalidOperationException("请先在 Inspector 的列表类型中选择树形列表，再使用树接口。");
        }

        private void RequireFlatMode()
        {
            if (_listType != UIVirtualListType.Flat)
                throw new InvalidOperationException("普通数据接口只能用于普通列表；树形、分页和分组请使用各自的数据源接口。");
        }

        private void DisposeOwnedTree()
        {
            var tree = _treeAdapter;
            _treeAdapter = null;
            tree?.Dispose();
        }
    }
}
