using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public sealed partial class UGUIVirtualListComponent
    {
        [SerializeField] private UGUIVirtualSelectionMode _selectionMode;
        private Func<object, string> _selectionKeySelector;
        private UGUIVirtualSelection _selection;

        public UGUIVirtualSelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                _selectionMode = value;
                if (_selection != null) _selection.Mode = value;
            }
        }

        public int SelectedCount => _selection?.SelectedCount ?? 0;
        public event Action<int, object, bool> SelectionChanged;
        public event Action SelectionBatchCompleted;

        public UGUIVirtualSelection Selection
        {
            get
            {
                if (_selection == null)
                {
                    _selection = new UGUIVirtualSelection(Controller, _selectionMode)
                    {
                        KeySelector = _selectionKeySelector
                    };
                    _selection.SelectionChanged += ForwardSelectionChanged;
                    _selection.SelectionBatchCompleted += ForwardSelectionBatch;
                }
                return _selection;
            }
        }

        /// <summary>可选：按业务 Id 保持选中（例如服务器重排、分页插入后仍跟随同一条数据）。</summary>
        public Func<object, string> SelectionKeySelector
        {
            get => _selectionKeySelector;
            set
            {
                _selectionKeySelector = value;
                if (_selection != null) _selection.KeySelector = value;
            }
        }

        public bool IsSelected(int index) => _selection != null && _selection.IsSelected(index);
        public bool IsSelectedByKey(string key) => _selection != null && _selection.IsSelectedKey(key);
        public bool SelectByKey(string key) => Selection.SelectByKey(key);
        public bool DeselectByKey(string key) => Selection.DeselectByKey(key);
        public IReadOnlyList<string> GetSelectedKeys() => _selection?.GetSelectedKeys() ?? Array.Empty<string>();
        public bool Select(int index) => Selection.Select(index);
        public bool Deselect(int index) => Selection.Deselect(index);
        public bool ToggleSelection(int index) => Selection.Toggle(index);
        public void SelectRange(int from, int to, bool additive = false) => Selection.SelectRange(from, to, additive);
        public void SelectAll() => Selection.SelectAll();
        public void ClearSelection() => _selection?.ClearSelection();
        public IReadOnlyList<int> GetSelectedIndices() => _selection?.GetSelectedIndices() ?? Array.Empty<int>();

        private void ForwardSelectionChanged(int index, object data, bool selected)
            => SelectionChanged?.Invoke(index, data, selected);
        private void ForwardSelectionBatch() => SelectionBatchCompleted?.Invoke();

        private void DisposeOwnedSelection()
        {
            var selection = _selection;
            _selection = null;
            if (selection == null) return;
            selection.SelectionChanged -= ForwardSelectionChanged;
            selection.SelectionBatchCompleted -= ForwardSelectionBatch;
            selection.Dispose();
        }
    }
}
