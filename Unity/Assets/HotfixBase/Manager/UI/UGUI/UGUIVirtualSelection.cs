using System;
using System.Collections.Generic;

namespace Ux
{
    public enum UGUIVirtualSelectionMode
    {
        None = 0,
        Single = 1,
        Multiple = 2
    }

    /// <summary>
    /// 选择状态管理器。默认按数据索引保存，滚动复用不会丢失；
    /// 设置 KeySelector 后按业务 Id 保存，数据重排/分页插入/树分组编辑都会重新定位。
    /// 纯 C#，不拥有列表。
    /// </summary>
    public sealed class UGUIVirtualSelection : IDisposable
    {
        private readonly UGUIVirtualList _list;
        private readonly HashSet<int> _selected = new HashSet<int>();
        private readonly HashSet<int> _notified = new HashSet<int>();
        private readonly HashSet<string> _selectedKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<int> _added = new List<int>();
        private readonly List<int> _removed = new List<int>();
        private readonly List<int> _snapshot = new List<int>();
        private readonly List<string> _snapshotKeys = new List<string>();
        private Func<object, string> _keySelector;
        private UGUIVirtualSelectionMode _mode;
        private bool _disposed;

        public UGUIVirtualSelection(UGUIVirtualList list, UGUIVirtualSelectionMode mode = UGUIVirtualSelectionMode.None)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _mode = mode;
            _list.DataInserted += OnInserted;
            _list.DataRemoved += OnRemoved;
            _list.DataReset += OnReset;
        }

        public UGUIVirtualSelectionMode Mode
        {
            get => _mode;
            set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(UGUIVirtualSelection));
                if (value != UGUIVirtualSelectionMode.None && value != UGUIVirtualSelectionMode.Single &&
                    value != UGUIVirtualSelectionMode.Multiple)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (_mode == value) return;
                _mode = value;
                if (value == UGUIVirtualSelectionMode.None)
                {
                    _selected.Clear();
                    _selectedKeys.Clear();
                }
                else if (value == UGUIVirtualSelectionMode.Single && _selected.Count > 1)
                {
                    int keep = int.MaxValue;
                    foreach (var index in _selected) if (index < keep) keep = index;
                    _selected.Clear();
                    _selectedKeys.Clear();
                    if (keep != int.MaxValue)
                    {
                        _selected.Add(keep);
                        AddKey(keep);
                    }
                }
                RaiseChanged();
            }
        }

        /// <summary>
        /// 可选：按业务标识保持选中。返回稳定 Id；null 表示该行不可通过 Id 选择。
        /// 设置后全量替换数据也会尝试按 Id 恢复选中。
        /// </summary>
        public Func<object, string> KeySelector
        {
            get => _keySelector;
            set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(UGUIVirtualSelection));
                _keySelector = value;
                RebuildKeys();
            }
        }

        public int SelectedCount => _selected.Count;
        public bool HasSelection => _selected.Count > 0;

        public IReadOnlyList<int> GetSelectedIndices()
        {
            _snapshot.Clear();
            foreach (var index in _selected) _snapshot.Add(index);
            _snapshot.Sort();
            return _snapshot.AsReadOnly();
        }

        public IReadOnlyList<string> GetSelectedKeys()
        {
            _snapshotKeys.Clear();
            foreach (var key in _selectedKeys) _snapshotKeys.Add(key);
            _snapshotKeys.Sort(StringComparer.Ordinal);
            return _snapshotKeys.AsReadOnly();
        }

        public event Action<int, object, bool> SelectionChanged;
        public event Action SelectionBatchCompleted;

        public bool IsSelected(int index) => !_disposed && _selected.Contains(index);
        public bool IsSelectedKey(string key) => !_disposed && key != null && _selectedKeys.Contains(key);

        public bool Select(int index) => SetSelected(index, true);
        public bool Deselect(int index) => SetSelected(index, false);
        public bool Toggle(int index) => _disposed || _mode == UGUIVirtualSelectionMode.None
            ? false
            : SetSelected(index, !_selected.Contains(index));

        public bool SelectByKey(string key)
        {
            if (_disposed || _keySelector == null || key == null || _mode == UGUIVirtualSelectionMode.None) return false;
            if (_selectedKeys.Contains(key)) return false;
            int index = IndexOfKey(key);
            return index >= 0 && SetSelected(index, true);
        }

        public bool DeselectByKey(string key)
        {
            if (_disposed || _keySelector == null || key == null) return false;
            int index = IndexOfKey(key);
            return index >= 0 && SetSelected(index, false);
        }

        public void SelectRange(int from, int to, bool additive = false)
        {
            if (_disposed || _mode != UGUIVirtualSelectionMode.Multiple) return;
            if (from > to) { int swap = from; from = to; to = swap; }
            from = Math.Max(0, from);
            to = Math.Min(_list.Count - 1, to);
            if (to < from) return;
            if (!additive)
            {
                _selected.Clear();
                _selectedKeys.Clear();
            }
            for (int i = from; i <= to; i++)
            {
                _selected.Add(i);
                AddKey(i);
            }
            RaiseChanged();
        }

        public void SelectAll()
        {
            if (_disposed || _mode != UGUIVirtualSelectionMode.Multiple) return;
            for (int i = 0; i < _list.Count; i++)
            {
                _selected.Add(i);
                AddKey(i);
            }
            RaiseChanged();
        }

        public void ClearSelection()
        {
            if (_disposed || _selected.Count == 0) return;
            _selected.Clear();
            _selectedKeys.Clear();
            RaiseChanged();
        }

        private bool SetSelected(int index, bool selected)
        {
            if (_disposed || _mode == UGUIVirtualSelectionMode.None) return false;
            if (index < 0 || index >= _list.Count) return false;
            if (_mode == UGUIVirtualSelectionMode.Single)
            {
                if (!selected)
                {
                    if (!_selected.Remove(index)) return false;
                    RemoveKey(index);
                    RaiseChanged();
                    return true;
                }
                if (_selected.Count == 1 && _selected.Contains(index)) return false;
                _selected.Clear();
                _selectedKeys.Clear();
                _selected.Add(index);
                AddKey(index);
                RaiseChanged();
                return true;
            }
            bool changed = selected ? _selected.Add(index) : _selected.Remove(index);
            if (!changed) return false;
            if (selected) AddKey(index); else RemoveKey(index);
            RaiseChanged();
            return true;
        }

        private void RaiseChanged()
        {
            if (_disposed) return;
            _added.Clear();
            _removed.Clear();
            foreach (var index in _selected) if (!_notified.Contains(index)) _added.Add(index);
            foreach (var index in _notified) if (!_selected.Contains(index)) _removed.Add(index);
            _notified.Clear();
            foreach (var index in _selected) _notified.Add(index);
            if (_added.Count == 0 && _removed.Count == 0) return;
            _added.Sort();
            _removed.Sort();
            if (_list.IsDisposedOrDisposing) return;

            _list.BeginSelectionRender();
            try
            {
                foreach (var index in _removed)
                {
                    _list.QueueItemRebind(index);
                    SelectionChanged?.Invoke(index, _list.GetData(index), false);
                }
                foreach (var index in _added)
                {
                    _list.QueueItemRebind(index);
                    SelectionChanged?.Invoke(index, _list.GetData(index), true);
                }
                SelectionBatchCompleted?.Invoke();
            }
            finally { _list.EndSelectionRender(); }
        }

        private void OnInserted(int index, int count)
        {
            if (_selected.Count == 0) return;
            if (_keySelector != null) { RemapByKeys(); return; }
            _snapshot.Clear();
            foreach (var old in _selected) _snapshot.Add(old);
            _selected.Clear();
            foreach (var old in _snapshot) _selected.Add(old >= index ? old + count : old);
            _notified.Clear();
            foreach (var old in _snapshot) _notified.Add(old >= index ? old + count : old);
        }

        private void OnRemoved(int index, int count)
        {
            if (_selected.Count == 0) return;
            if (_keySelector != null) { RemapByKeys(); return; }
            _snapshot.Clear();
            foreach (var old in _selected) _snapshot.Add(old);
            _selected.Clear();
            foreach (var old in _snapshot)
            {
                if (old < index) _selected.Add(old);
                else if (old >= index + count) _selected.Add(old - count);
            }
            _notified.Clear();
            foreach (var old in _snapshot)
            {
                if (old < index) _notified.Add(old);
                else if (old >= index + count) _notified.Add(old - count);
            }
        }

        /// <summary>全量替换：按 Id 模式尝试恢复，索引模式取消并上报。</summary>
        private void OnReset()
        {
            if (_disposed) return;
            if (_keySelector != null) { RebuildFromKeys(); return; }
            _removed.Clear();
            foreach (var index in _notified) _removed.Add(index);
            _removed.Sort();
            _selected.Clear();
            _notified.Clear();
            if (_removed.Count == 0 || _list.IsDisposedOrDisposing) return;
            _list.BeginSelectionRender();
            try
            {
                foreach (var index in _removed) SelectionChanged?.Invoke(index, null, false);
                SelectionBatchCompleted?.Invoke();
            }
            finally { _list.EndSelectionRender(); }
        }

        private void RemapByKeys()
        {
            RebuildFromKeys(false);
        }

        private void RebuildFromKeys(bool notify = true)
        {
            if (_list.IsDisposedOrDisposing) return;
            _removed.Clear();
            foreach (var index in _notified) _removed.Add(index);
            _removed.Sort();
            _selected.Clear();
            _snapshotKeys.Clear();
            foreach (var key in _selectedKeys)
            {
                int index = IndexOfKey(key);
                if (index < 0) continue; // 数据里已不存在的 Id 从选中集合移除
                _selected.Add(index);
                _snapshotKeys.Add(key);
            }
            _selectedKeys.Clear();
            foreach (var key in _snapshotKeys) _selectedKeys.Add(key);
            _notified.Clear();
            foreach (var index in _selected) _notified.Add(index);
            if (!notify) return;
            if (_removed.Count == 0 && _selected.Count == 0) return;
            _list.BeginSelectionRender();
            try
            {
                foreach (var index in _removed) SelectionChanged?.Invoke(index, null, false);
                foreach (var index in _selected) SelectionChanged?.Invoke(index, _list.GetData(index), true);
                SelectionBatchCompleted?.Invoke();
            }
            finally { _list.EndSelectionRender(); }
        }

        private int IndexOfKey(string key)
        {
            if (key == null || _keySelector == null) return -1;
            for (int i = 0; i < _list.Count; i++)
            {
                var data = _list.GetData(i);
                if (data != null && string.Equals(_keySelector(data), key, StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        private void AddKey(int index)
        {
            if (_keySelector == null) return;
            var data = _list.GetData(index);
            if (data == null) return;
            var key = _keySelector(data);
            if (key != null) _selectedKeys.Add(key);
        }

        private void RemoveKey(int index)
        {
            if (_keySelector == null) return;
            var data = _list.GetData(index);
            if (data == null) return;
            var key = _keySelector(data);
            if (key != null) _selectedKeys.Remove(key);
        }

        private void RebuildKeys()
        {
            _selectedKeys.Clear();
            if (_keySelector == null) return;
            foreach (var index in _selected) AddKey(index);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _list.DataInserted -= OnInserted; } catch { }
            try { _list.DataRemoved -= OnRemoved; } catch { }
            try { _list.DataReset -= OnReset; } catch { }
            _selected.Clear();
            _selectedKeys.Clear();
            _notified.Clear();
            _keySelector = null;
            SelectionChanged = null;
            SelectionBatchCompleted = null;
        }
    }
}
