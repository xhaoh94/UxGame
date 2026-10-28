using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>树的业务模型。Id 和子节点结构在构造后固定；数据可通过适配器更新。</summary>
    public sealed class UGUIVirtualTreeNode
    {
        public string Id { get; }
        public object Data { get; set; }
        public IReadOnlyList<UGUIVirtualTreeNode> Children => _children;
        public bool InitiallyExpanded { get; }
        private readonly List<UGUIVirtualTreeNode> _children = new List<UGUIVirtualTreeNode>();

        public UGUIVirtualTreeNode(string id, object data,
            IEnumerable<UGUIVirtualTreeNode> children = null, bool expanded = false)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("树节点 Id 不能为空。", nameof(id));
            Id = id;
            Data = data;
            InitiallyExpanded = expanded;
            var kids = new List<UGUIVirtualTreeNode>();
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child == null)
                        throw new ArgumentException("子节点不能为 null。", nameof(children));
                    kids.Add(child);
                }
            }
            _children.AddRange(kids);
        }

        internal void InsertChildInternal(int index, UGUIVirtualTreeNode child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (index < 0 || index > _children.Count) throw new ArgumentOutOfRangeException(nameof(index));
            _children.Insert(index, child);
        }

        internal void RemoveChildInternal(UGUIVirtualTreeNode child) => _children.Remove(child);
        internal int IndexOfChild(UGUIVirtualTreeNode child) => _children.IndexOf(child);
    }

    /// <summary>交给 ItemRenderer / ItemTypeProvider 的行数据，不是业务数据本身。</summary>
    public sealed class UGUIVirtualTreeItem
    {
        public UGUIVirtualTreeNode Node { get; }
        public string Id => Node.Id;
        public object Data => Node.Data;
        public int Depth { get; }
        public UGUIVirtualTreeItem Parent { get; }
        public string ParentId => Parent?.Id;
        public bool HasChildren => Node.Children.Count > 0;
        public bool IsExpanded { get; internal set; }

        internal UGUIVirtualTreeItem(UGUIVirtualTreeNode node, UGUIVirtualTreeItem parent, int depth, bool expanded)
        {
            Node = node;
            Parent = parent;
            Depth = depth;
            IsExpanded = HasChildren && expanded;
        }
    }

    /// <summary>
    /// 纯 C# 树适配器：树模型扁平化为当前展开的行，底层沿用增量虚拟列表。
    /// 不拥有列表、不创建 GameObject、不注册点击事件；同一个 Core 同时只能由一个数据源管理。
    /// </summary>
    public sealed class UGUIVirtualTreeAdapter : IDisposable
    {
        private readonly UGUIVirtualList _list;
        private TreeState _state = new TreeState();
        private ulong _expectedRevision;
        private bool _hasRoots;
        private bool _changing;
        private bool _disposed;

        public UGUIVirtualTreeAdapter(UGUIVirtualList list)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
        }

        public int TotalNodeCount => _state.Items.Count;
        public int VisibleCount => _state.Visible.Count;
        public IReadOnlyList<UGUIVirtualTreeItem> VisibleItems => _state.ReadOnlyVisible;
        public IReadOnlyList<UGUIVirtualTreeItem> Roots => _state.ReadOnlyRoots;
        public bool OwnsCurrentData => !_disposed && _hasRoots && !_list.IsDisposedOrDisposing &&
            (_changing || _expectedRevision == _list.DataRevision);

        public bool TryGetItem(string id, out UGUIVirtualTreeItem item)
        {
            item = null;
            return !_disposed && id != null && _state.Items.TryGetValue(id, out item);
        }

        public int GetVisibleIndex(string id)
        {
            return !_disposed && id != null && _state.Indices.TryGetValue(id, out var index) ? index : -1;
        }

        /// <summary>全量替换森林。preserveExpansion 按 Id 保留展开状态，但仍会替换所有行并回到起点。</summary>
        public void SetRoots(IEnumerable<UGUIVirtualTreeNode> roots, bool preserveExpansion = false)
        {
            Guard(false);
            _changing = true;
            try
            {
                var newRoots = new List<UGUIVirtualTreeNode>();
                if (roots != null) foreach (var node in roots) newRoots.Add(node);
                var prepared = BuildState(newRoots, preserveExpansion ? _state : null);
                var previous = _state;
                bool hadRoots = _hasRoots;
                ulong revision = _list.DataRevision;
                _state = prepared;
                _hasRoots = true;
                try
                {
                    _list.SetDatas(prepared.Visible);
                    _expectedRevision = _list.DataRevision;
                }
                catch
                {
                    if (!_disposed)
                    {
                        if (_list.DataRevision == revision)
                        {
                            _state = previous;
                            _hasRoots = hadRoots;
                        }
                        else _expectedRevision = _list.DataRevision;
                    }
                    throw;
                }
                if (!_disposed)
                {
                    _rootModels.Clear();
                    _rootModels.AddRange(newRoots);
                }
            }
            finally { _changing = false; }
        }

        public bool Expand(string id) => SetExpanded(id, true);
        public bool Collapse(string id) => SetExpanded(id, false);
        public bool Toggle(string id)
        {
            Guard();
            return TryGetItem(id, out var item) && SetExpanded(id, !item.IsExpanded);
        }

        public bool SetExpanded(string id, bool expanded)
        {
            Guard();
            if (!TryGetItem(id, out var item) || !item.HasChildren || item.IsExpanded == expanded)
                return false;
            int index = GetVisibleIndex(id);
            // 隐藏子分支的展开意图也保留，直到其祖先重新展开。
            if (index < 0)
            {
                item.IsExpanded = expanded;
                return true;
            }

            _changing = true;
            try
            {
                int start = index + 1;
                int count = 0;
                int firstVisible = _list.FirstVisibleIndex;
                List<UGUIVirtualTreeItem> changed;
                if (expanded)
                    changed = CollectVisibleChildren(_state, item);
                else
                {
                    while (start + count < _state.Visible.Count && _state.Visible[start + count].Depth > item.Depth)
                        count++;
                    changed = _state.Visible.GetRange(start, count);
                }
                bool anchorRemoved = !expanded && firstVisible >= start && firstVisible < start + count;
                ulong revision = _list.DataRevision;
                item.IsExpanded = expanded;
                if (expanded) _state.Visible.InsertRange(start, changed);
                else _state.Visible.RemoveRange(start, count);
                Reindex(_state);
                try
                {
                    _list.QueueItemRebind(index);
                    // 展开/折叠是可见投影变化，不是聊天追加；不要因原列表的末尾策略跳到整棵子树末端。
                    bool follow = _list.KeepEndOnDataChange;
                    _list.KeepEndOnDataChange = false;
                    try
                    {
                        if (expanded) _list.InsertDatas(start, changed);
                        else _list.RemoveRange(start, count);
                    }
                    finally { _list.KeepEndOnDataChange = follow; }
                    _expectedRevision = _list.DataRevision;
                }
                catch
                {
                    if (!_disposed)
                    {
                        if (_list.DataRevision == revision)
                        {
                            item.IsExpanded = !expanded;
                            if (expanded) _state.Visible.RemoveRange(start, changed.Count);
                            else _state.Visible.InsertRange(start, changed);
                            Reindex(_state);
                        }
                        else _expectedRevision = _list.DataRevision;
                    }
                    throw;
                }
                if (!_disposed && !_list.IsDisposedOrDisposing)
                {
                    if (anchorRemoved)
                        _list.ScrollToView(index, UGUIVirtualListAlignment.Nearest);
                }
                return true;
            }
            finally { _changing = false; }
        }

        /// <summary>替换节点数据。隐藏节点不创建实例；已展开状态和 Id 不变。</summary>
        public bool UpdateNodeData(string id, object data)
        {
            Guard();
            if (!TryGetItem(id, out var item)) return false;
            object previous = item.Node.Data;
            ulong revision = _list.DataRevision;
            _changing = true;
            try
            {
                item.Node.Data = data;
                int index = GetVisibleIndex(id);
                if (index >= 0) _list.UpdateData(index, item);
                _expectedRevision = _list.DataRevision;
                return true;
            }
            catch
            {
                if (_list.DataRevision == revision) item.Node.Data = previous;
                else _expectedRevision = _list.DataRevision;
                throw;
            }
            finally { _changing = false; }
        }

        public bool RefreshNode(string id)
        {
            Guard();
            return TryGetItem(id, out var item) && UpdateNodeData(id, item.Data);
        }

        /// <summary>按 Id 定位隐藏节点，可按需先逐级展开其祖先。</summary>
        public bool ScrollToNode(string id, bool expandAncestors = true,
            UGUIVirtualListAlignment alignment = UGUIVirtualListAlignment.Nearest)
        {
            Guard();
            if (!TryGetItem(id, out var item)) return false;
            if (expandAncestors)
            {
                var path = new Stack<UGUIVirtualTreeItem>();
                for (var parent = item.Parent; parent != null; parent = parent.Parent) path.Push(parent);
                while (path.Count > 0) Expand(path.Pop().Id);
            }
            int index = GetVisibleIndex(id);
            if (index < 0) return false;
            _list.ScrollToView(index, alignment);
            return true;
        }

        /// <summary>在根层指定位置插入节点（含子树）。</summary>
        public bool InsertRoot(int index, UGUIVirtualTreeNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (!OwnsCurrentData && _hasRoots) throw new InvalidOperationException("树投影已被直接修改，请重新 SetRoots。");
            if (index < 0 || index > _rootModels.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Mutate(() => _rootModels.Insert(index, node));
        }

        public bool AddRoot(UGUIVirtualTreeNode node) => InsertRoot(_rootModels.Count, node);

        /// <summary>删除节点及其子树。</summary>
        public bool RemoveNode(string id)
        {
            if (!TryGetItem(id, out var item)) return false;
            return Mutate(() => DetachNode(item.Node));
        }

        /// <summary>向父节点插入子节点；parentId 为 null 表示根层。</summary>
        public bool InsertChild(string parentId, int index, UGUIVirtualTreeNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (parentId == null) return InsertRoot(index, node);
            if (!TryGetItem(parentId, out var parent)) return false;
            if (index < 0 || index > parent.Node.Children.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Mutate(() => parent.Node.InsertChildInternal(index, node));
        }

        public bool AddChild(string parentId, UGUIVirtualTreeNode node)
        {
            if (parentId == null) return AddRoot(node);
            if (!TryGetItem(parentId, out var parent)) return false;
            return InsertChild(parentId, parent.Node.Children.Count, node);
        }

        /// <summary>把节点（含子树）移动到新父节点的指定位置；parentId 为 null 表示根层。</summary>
        public bool MoveNode(string id, string parentId, int index)
        {
            if (!TryGetItem(id, out var item)) return false;
            if (id == parentId) return false;
            UGUIVirtualTreeItem newParent = null;
            if (parentId != null && !TryGetItem(parentId, out newParent)) return false;
            for (var p = newParent; p != null; p = p.Parent)
                if (ReferenceEquals(p.Node, item.Node)) return false;
            int limit = newParent == null ? _rootModels.Count : newParent.Node.Children.Count;
            if (index < 0 || index > limit) throw new ArgumentOutOfRangeException(nameof(index));
            return Mutate(() =>
            {
                int oldIndex = newParent != null && ReferenceEquals(item.Parent?.Node, newParent.Node)
                    ? newParent.Node.IndexOfChild(item.Node) : -1;
                DetachNode(item.Node);
                int target = index;
                if (oldIndex >= 0 && oldIndex < index) target--;
                if (newParent == null) _rootModels.Insert(target, item.Node);
                else newParent.Node.InsertChildInternal(target, item.Node);
            });
        }

        private readonly List<UGUIVirtualTreeNode> _rootModels = new List<UGUIVirtualTreeNode>();

        private void DetachNode(UGUIVirtualTreeNode node)
        {
            if (_rootModels.Remove(node)) return;
            if (_state.Items.TryGetValue(node.Id, out var item) && item.Parent != null)
                item.Parent.Node.RemoveChildInternal(node);
        }

        /// <summary>结构变更后重建投影，只对真正变化的行做增删，未变化行保留实例与绑定。</summary>
        private bool Mutate(Action change)
        {
            Guard();
            var previousState = _state;
            ulong revision = _list.DataRevision;
            _changing = true;
            try
            {
                change();
                var next = BuildState(_rootModels, _state);
                ApplyVisibleDiff(next);
                _expectedRevision = _list.DataRevision;
                return true;
            }
            catch
            {
                if (_list.DataRevision == revision) _state = previousState;
                else _expectedRevision = _list.DataRevision;
                throw;
            }
            finally { _changing = false; }
        }

        private void ApplyVisibleDiff(TreeState next)
        {
            var oldVisible = _state.Visible;
            var newVisible = next.Visible;
            int prefix = 0;
            int max = Math.Min(oldVisible.Count, newVisible.Count);
            while (prefix < max && ReferenceEquals(oldVisible[prefix], newVisible[prefix])) prefix++;
            int suffix = 0;
            while (suffix < max - prefix &&
                   ReferenceEquals(oldVisible[oldVisible.Count - 1 - suffix], newVisible[newVisible.Count - 1 - suffix]))
                suffix++;
            int removeCount = oldVisible.Count - prefix - suffix;
            int insertCount = newVisible.Count - prefix - suffix;
            _state = next;
            if (removeCount > 0) _list.RemoveRange(prefix, removeCount);
            if (insertCount > 0) _list.InsertDatas(prefix, newVisible.GetRange(prefix, insertCount));
        }

        /// <summary>释放树索引，不清空/销毁底层列表；UI 生命周期仍由调用方管理。</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _state = new TreeState();
            _hasRoots = false;
        }

        private void Guard(bool requireOwnership = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UGUIVirtualTreeAdapter));
            if (_changing) throw new InvalidOperationException("不能在树的渲染/回收或数据枚举回调中重入修改树，请延后执行。");
            _list.ValidateDataMutation();
            if (requireOwnership && (!_hasRoots || _expectedRevision != _list.DataRevision))
                throw new InvalidOperationException("底层列表数据已被直接修改或树尚未初始化，请重新 SetRoots 接管数据源。");
        }

        private static TreeState BuildState(IEnumerable<UGUIVirtualTreeNode> roots, TreeState previous)
        {
            var state = new TreeState();
            var input = new List<UGUIVirtualTreeNode>();
            if (roots != null) foreach (var node in roots) input.Add(node);
            var stack = new Stack<BuildEntry>();
            for (int i = input.Count - 1; i >= 0; i--) stack.Push(new BuildEntry(input[i], null, 0));
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current.Node == null) throw new ArgumentException("根节点不能为 null。", nameof(roots));
                if (state.Items.ContainsKey(current.Node.Id))
                    throw new ArgumentException($"树节点 Id 重复，或同一节点被多个父项引用：{current.Node.Id}", nameof(roots));
                if (current.Depth == int.MaxValue) throw new ArgumentException("树的层级过深。", nameof(roots));
                UGUIVirtualTreeItem old = null;
                previous?.Items.TryGetValue(current.Node.Id, out old);
                bool expanded = old != null ? old.IsExpanded : current.Node.InitiallyExpanded;
                UGUIVirtualTreeItem item;
                // 结构编辑时尽量复用行实例：父子节点、层级都不变才复用，
                // 这样未受影响的行不会因为一次增删而全部重绑。
                if (old != null && ReferenceEquals(old.Node, current.Node) &&
                    ReferenceEquals(old.Parent?.Node, current.Parent?.Node) &&
                    old.Depth == current.Depth)
                {
                    item = old;
                    item.IsExpanded = item.HasChildren && expanded;
                }
                else
                    item = new UGUIVirtualTreeItem(current.Node, current.Parent, current.Depth, expanded);
                state.Items.Add(item.Id, item);
                if (item.Parent == null) state.Roots.Add(item);
                for (int i = current.Node.Children.Count - 1; i >= 0; i--)
                    stack.Push(new BuildEntry(current.Node.Children[i], item, current.Depth + 1));
            }
            var visible = new Stack<UGUIVirtualTreeItem>();
            for (int i = state.Roots.Count - 1; i >= 0; i--) visible.Push(state.Roots[i]);
            CollectVisible(state, visible, state.Visible);
            Reindex(state);
            return state;
        }

        private static List<UGUIVirtualTreeItem> CollectVisibleChildren(TreeState state, UGUIVirtualTreeItem parent)
        {
            var stack = new Stack<UGUIVirtualTreeItem>();
            for (int i = parent.Node.Children.Count - 1; i >= 0; i--)
                stack.Push(state.Items[parent.Node.Children[i].Id]);
            var result = new List<UGUIVirtualTreeItem>();
            CollectVisible(state, stack, result);
            return result;
        }

        private static void CollectVisible(TreeState state, Stack<UGUIVirtualTreeItem> stack, List<UGUIVirtualTreeItem> output)
        {
            while (stack.Count > 0)
            {
                var item = stack.Pop();
                output.Add(item);
                if (!item.IsExpanded) continue;
                for (int i = item.Node.Children.Count - 1; i >= 0; i--)
                    stack.Push(state.Items[item.Node.Children[i].Id]);
            }
        }

        private static void Reindex(TreeState state)
        {
            state.Indices.Clear();
            for (int i = 0; i < state.Visible.Count; i++) state.Indices.Add(state.Visible[i].Id, i);
        }

        private readonly struct BuildEntry
        {
            public readonly UGUIVirtualTreeNode Node;
            public readonly UGUIVirtualTreeItem Parent;
            public readonly int Depth;
            public BuildEntry(UGUIVirtualTreeNode node, UGUIVirtualTreeItem parent, int depth)
            { Node = node; Parent = parent; Depth = depth; }
        }

        private sealed class TreeState
        {
            public readonly Dictionary<string, UGUIVirtualTreeItem> Items = new Dictionary<string, UGUIVirtualTreeItem>(StringComparer.Ordinal);
            public readonly Dictionary<string, int> Indices = new Dictionary<string, int>(StringComparer.Ordinal);
            public readonly List<UGUIVirtualTreeItem> Roots = new List<UGUIVirtualTreeItem>();
            public readonly List<UGUIVirtualTreeItem> Visible = new List<UGUIVirtualTreeItem>();
            public readonly IReadOnlyList<UGUIVirtualTreeItem> ReadOnlyRoots;
            public readonly IReadOnlyList<UGUIVirtualTreeItem> ReadOnlyVisible;
            public TreeState() { ReadOnlyRoots = Roots.AsReadOnly(); ReadOnlyVisible = Visible.AsReadOnly(); }
        }
    }
}
