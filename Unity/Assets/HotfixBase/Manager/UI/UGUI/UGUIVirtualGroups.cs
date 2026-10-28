using System;
using System.Collections.Generic;

namespace Ux
{
    public sealed class UGUIVirtualGroup
    {
        public string Id { get; }
        public object HeaderData { get; internal set; }
        public IReadOnlyList<object> Items => _items;
        private readonly List<object> _items = new List<object>();

        public UGUIVirtualGroup(string id, object headerData, IEnumerable<object> items = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("分组 Id 不能为空。", nameof(id));
            Id = id;
            HeaderData = headerData;
            if (items != null) _items.AddRange(items);
        }

        public static UGUIVirtualGroup Create<T>(string id, object headerData, IEnumerable<T> items = null)
        {
            if (items == null) return new UGUIVirtualGroup(id, headerData);
            var rows = new List<object>();
            foreach (var item in items) rows.Add(item);
            return new UGUIVirtualGroup(id, headerData, rows);
        }

        internal void InsertInternal(int index, object item) => _items.Insert(index, item);
        internal void RemoveAtInternal(int index) => _items.RemoveAt(index);
        internal void SetInternal(int index, object item) => _items[index] = item;
    }

    public sealed class UGUIVirtualGroupItem
    {
        public string GroupId { get; }
        public int GroupIndex { get; }
        public int ItemIndex { get; }
        public bool IsHeader => ItemIndex < 0;
        public bool IsSticky { get; }
        public object Data { get; internal set; }
        internal ulong Revision { get; set; }
        internal UGUIVirtualGroupItem(string id, int group, int item, object data, bool sticky = false)
        { GroupId = id; GroupIndex = group; ItemIndex = item; Data = data; IsSticky = sticky; }
        internal UGUIVirtualGroupItem StickyCopy() => new UGUIVirtualGroupItem(GroupId, GroupIndex, -1, Data, true) { Revision = Revision };
    }

    public readonly struct UGUIVirtualStickyHeaderInfo
    {
        public readonly UGUIVirtualGroupItem Header;
        public readonly int HeaderIndex;
        public readonly int NextHeaderIndex;
        public bool IsVisible => Header != null;
        internal UGUIVirtualStickyHeaderInfo(UGUIVirtualGroupItem header, int index, int next)
        { Header = header; HeaderIndex = index; NextHeaderIndex = next; }
    }

    /// <summary>纯 C# 分组投影。每组一条 Header 加若干业务行，不创建任何 GameObject。</summary>
    public sealed class UGUIVirtualGroupAdapter : IDisposable
    {
        private readonly UGUIVirtualList _list;
        private readonly bool _showEmpty;
        private readonly List<UGUIVirtualGroup> _models = new List<UGUIVirtualGroup>();
        private State _state = new State();
        private ulong _expectedRevision;
        private bool _attached;
        private bool _changing;
        private bool _disposed;

        public UGUIVirtualGroupAdapter(UGUIVirtualList list, bool showEmptyGroups = true)
        { _list = list ?? throw new ArgumentNullException(nameof(list)); _showEmpty = showEmptyGroups; }

        public int GroupCount => _state.Groups.Count;
        public int VisibleGroupCount => _state.Headers.Count;
        public int DataCount => _state.DataCount;
        public int RowCount => _state.Rows.Count;
        public bool OwnsCurrentData => !_disposed && _attached && !_list.IsDisposedOrDisposing &&
            (_changing || _expectedRevision == _list.DataRevision);

        public void SetGroups(IEnumerable<UGUIVirtualGroup> groups)
        {
            Guard(false);
            _changing = true;
            try
            {
                var models = new List<UGUIVirtualGroup>();
                if (groups != null) foreach (var group in groups) models.Add(group);
                var prepared = Build(models, null);
                var previous = _state;
                bool attached = _attached;
                ulong revision = _list.DataRevision;
                _state = prepared;
                _attached = true;
                try { _list.SetDatas(prepared.Rows); _expectedRevision = _list.DataRevision; }
                catch
                {
                    if (!_disposed)
                    {
                        if (_list.DataRevision == revision) { _state = previous; _attached = attached; }
                        else _expectedRevision = _list.DataRevision;
                    }
                    throw;
                }
                _models.Clear();
                _models.AddRange(models);
            }
            finally { _changing = false; }
        }

        public int GetHeaderIndex(string id) => !_disposed && id != null && _state.Groups.TryGetValue(id, out var group) ? group.HeaderIndex : -1;

        public bool TryGetHeader(string id, out UGUIVirtualGroupItem header)
        {
            header = null;
            if (_disposed || id == null || !_state.Groups.TryGetValue(id, out var group)) return false;
            header = group.Header;
            return true;
        }

        public bool UpdateHeader(string id, object data)
        {
            Guard();
            if (id == null || !_state.Groups.TryGetValue(id, out var group)) return false;
            return Update(group.Header, group.HeaderIndex, data, () => group.Model.HeaderData = data, () => group.Model.HeaderData);
        }

        public bool UpdateItem(string id, int itemIndex, object data)
        {
            Guard();
            if (id == null || !_state.Groups.TryGetValue(id, out var group)) return false;
            if (itemIndex < 0 || itemIndex >= group.Items.Count) throw new ArgumentOutOfRangeException(nameof(itemIndex));
            var model = group.Model;
            var previous = model.Items[itemIndex];
            return Update(group.Items[itemIndex], group.HeaderIndex + 1 + itemIndex, data,
                () => model.SetInternal(itemIndex, data), () => previous);
        }

        private bool Update(UGUIVirtualGroupItem row, int index, object data, Action applyModel, Func<object> undo)
        {
            object previous = row.Data;
            ulong oldRevision = row.Revision;
            ulong revision = _list.DataRevision;
            _changing = true;
            try
            {
                applyModel();
                row.Data = data;
                row.Revision++;
                if (index >= 0) _list.UpdateData(index, row);
                _expectedRevision = _list.DataRevision;
                return true;
            }
            catch
            {
                if (_list.DataRevision == revision) { undo(); row.Data = previous; row.Revision = oldRevision; }
                else _expectedRevision = _list.DataRevision;
                throw;
            }
            finally { _changing = false; }
        }

        /// <summary>在指定位置插入分组（含其全部条目）。</summary>
        public bool InsertGroup(int index, UGUIVirtualGroup group)
        {
            Guard();
            if (group == null) throw new ArgumentNullException(nameof(group));
            if (_state.Groups.ContainsKey(group.Id)) throw new ArgumentException($"分组 Id 重复：{group.Id}", nameof(group));
            if (index < 0 || index > _models.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Mutate(() =>
            {
                _models.Insert(index, group);
                for (int i = index + 1; i < _models.Count; i++)
                    AdjustGroupIndex(_state, _models[i].Id, +1);
            });
        }

        public bool AddGroup(UGUIVirtualGroup group) => InsertGroup(_models.Count, group);

        /// <summary>删除整个分组及其条目。</summary>
        public bool RemoveGroup(string id)
        {
            Guard();
            int index = IndexOfModel(id);
            if (index < 0) return false;
            return Mutate(() =>
            {
                _models.RemoveAt(index);
                for (int i = index; i < _models.Count; i++)
                    AdjustGroupIndex(_state, _models[i].Id, -1);
            });
        }

        /// <summary>移动分组位置（同层重排）。</summary>
        public bool MoveGroup(string id, int index)
        {
            Guard();
            int oldIndex = IndexOfModel(id);
            if (oldIndex < 0) return false;
            if (index < 0 || index >= _models.Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (oldIndex == index) return false;
            return Mutate(() =>
            {
                var group = _models[oldIndex];
                _models.RemoveAt(oldIndex);
                _models.Insert(index, group);
                for (int i = 0; i < _models.Count; i++)
                    AdjustGroupIndex(_state, _models[i].Id, i);
            });
        }

        /// <summary>在分组内插入业务项。</summary>
        public bool InsertItem(string groupId, int index, object data)
        {
            Guard();
            int groupIndex = IndexOfModel(groupId);
            if (groupIndex < 0) return false;
            var model = _models[groupIndex];
            if (index < 0 || index > model.Items.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Mutate(() => model.InsertInternal(index, data));
        }

        public bool AddItem(string groupId, object data)
        {
            int groupIndex = IndexOfModel(groupId);
            return groupIndex >= 0 && InsertItem(groupId, _models[groupIndex].Items.Count, data);
        }

        public bool RemoveItem(string groupId, int index)
        {
            Guard();
            int groupIndex = IndexOfModel(groupId);
            if (groupIndex < 0) return false;
            var model = _models[groupIndex];
            if (index < 0 || index >= model.Items.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Mutate(() => model.RemoveAtInternal(index));
        }

        public bool ScrollToGroup(string id, UGUIVirtualListAlignment alignment = UGUIVirtualListAlignment.Start)
        {
            Guard();
            int index = GetHeaderIndex(id);
            if (index < 0) return false;
            _list.ScrollToView(index, alignment);
            return true;
        }

        /// <summary>寻找已经越过视口起点的最后一个组头；首组未到顶时不显示吸顶副本。</summary>
        public UGUIVirtualStickyHeaderInfo GetStickyHeader(float scrollPosition)
        {
            if (!OwnsCurrentData || _state.Headers.Count == 0) return default;
            int low = 0, high = _state.Headers.Count - 1, selected = -1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                int index = _state.Headers[middle];
                var position = _list.GetItemLayout(index).Position;
                float start = _list.IsVertical ? -position.y : position.x;
                if (start <= scrollPosition) { selected = middle; low = middle + 1; }
                else high = middle - 1;
            }
            if (selected < 0) return default;
            int current = _state.Headers[selected];
            return new UGUIVirtualStickyHeaderInfo(_state.Rows[current], current,
                selected + 1 < _state.Headers.Count ? _state.Headers[selected + 1] : -1);
        }

        public static float CalculatePushOffset(float nextHeaderDistance, float headerLength)
            => Math.Min(0f, nextHeaderDistance - headerLength);

        private int IndexOfModel(string id)
        {
            if (id == null) return -1;
            for (int i = 0; i < _models.Count; i++) if (_models[i].Id == id) return i;
            return -1;
        }

        private static void AdjustGroupIndex(State state, string id, int delta)
        {
            if (state.Groups.TryGetValue(id, out var group)) group.GroupIndex += delta;
        }

        private bool Mutate(Action change)
        {
            var previous = _state;
            ulong revision = _list.DataRevision;
            _changing = true;
            try
            {
                change();
                var next = Build(_models, previous);
                ApplyVisibleDiff(next);
                _expectedRevision = _list.DataRevision;
                return true;
            }
            catch
            {
                if (_list.DataRevision == revision) _state = previous;
                else _expectedRevision = _list.DataRevision;
                throw;
            }
            finally { _changing = false; }
        }

        private void ApplyVisibleDiff(State next)
        {
            var oldRows = _state.Rows;
            var newRows = next.Rows;
            int prefix = 0;
            int max = Math.Min(oldRows.Count, newRows.Count);
            while (prefix < max && ReferenceEquals(oldRows[prefix], newRows[prefix])) prefix++;
            int suffix = 0;
            while (suffix < max - prefix &&
                   ReferenceEquals(oldRows[oldRows.Count - 1 - suffix], newRows[newRows.Count - 1 - suffix]))
                suffix++;
            int removeCount = oldRows.Count - prefix - suffix;
            int insertCount = newRows.Count - prefix - suffix;
            _state = next;
            if (removeCount > 0) _list.RemoveRange(prefix, removeCount);
            if (insertCount > 0) _list.InsertDatas(prefix, newRows.GetRange(prefix, insertCount));
        }

        private State Build(List<UGUIVirtualGroup> models, State previous)
        {
            var result = new State();
            if (models == null) return result;
            int groupIndex = 0;
            foreach (var model in models)
            {
                if (model == null) throw new ArgumentException("分组不能为 null。", nameof(models));
                if (result.Groups.ContainsKey(model.Id)) throw new ArgumentException($"分组 Id 重复：{model.Id}", nameof(models));
                GroupEntry oldGroup = null;
                if (previous != null) previous.Groups.TryGetValue(model.Id, out oldGroup);
                bool visible = _showEmpty || model.Items.Count > 0;
                var entry = new GroupEntry { Model = model, GroupIndex = groupIndex, HeaderIndex = visible ? result.Rows.Count : -1 };
                UGUIVirtualGroupItem header;
                if (oldGroup != null && ReferenceEquals(oldGroup.Model, model) && oldGroup.Header.ItemIndex < 0 &&
                    ReferenceEquals(oldGroup.Header.Data, model.HeaderData))
                    header = oldGroup.Header;
                else
                {
                    header = new UGUIVirtualGroupItem(model.Id, groupIndex, -1, model.HeaderData)
                    {
                        Revision = oldGroup != null && ReferenceEquals(oldGroup.Model, model) ? oldGroup.Header.Revision + 1 : 0
                    };
                }
                entry.Header = header;
                result.Groups.Add(model.Id, entry);
                if (visible)
                {
                    result.Headers.Add(result.Rows.Count);
                    result.Rows.Add(header);
                }
                for (int i = 0; i < model.Items.Count; i++)
                {
                    UGUIVirtualGroupItem row;
                    if (oldGroup != null && i < oldGroup.Items.Count && ReferenceEquals(oldGroup.Model, model) &&
                        ReferenceEquals(oldGroup.Items[i].Data, model.Items[i]) && oldGroup.Items[i].ItemIndex == i)
                        row = oldGroup.Items[i];
                    else
                        row = new UGUIVirtualGroupItem(model.Id, groupIndex, i, model.Items[i])
                        {
                            Revision = oldGroup != null && i < oldGroup.Items.Count &&
                                ReferenceEquals(oldGroup.Model, model) ? oldGroup.Items[i].Revision + 1 : 0
                        };
                    entry.Items.Add(row);
                    result.Rows.Add(row);
                    result.DataCount++;
                }
                groupIndex++;
            }
            return result;
        }

        private void Guard(bool ownership = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UGUIVirtualGroupAdapter));
            if (_changing) throw new InvalidOperationException("不能在分组的渲染/回收或枚举回调中重入修改分组。");
            _list.ValidateDataMutation();
            if (ownership && (!_attached || _expectedRevision != _list.DataRevision))
                throw new InvalidOperationException("分组投影已被直接修改，请重新 SetGroups 接管数据。");
        }

        public void Dispose()
        {
            _disposed = true;
            _attached = false;
            _state = new State();
            _models.Clear();
        }

        private sealed class GroupEntry
        {
            public UGUIVirtualGroup Model;
            public int GroupIndex;
            public int HeaderIndex;
            public UGUIVirtualGroupItem Header;
            public readonly List<UGUIVirtualGroupItem> Items = new List<UGUIVirtualGroupItem>();
        }

        private sealed class State
        {
            public int DataCount;
            public readonly List<UGUIVirtualGroupItem> Rows = new List<UGUIVirtualGroupItem>();
            public readonly List<int> Headers = new List<int>();
            public readonly Dictionary<string, GroupEntry> Groups = new Dictionary<string, GroupEntry>(StringComparer.Ordinal);
        }
    }
}
