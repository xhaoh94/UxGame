using System.Collections.Generic;

namespace Ux
{
    internal class UIStackHandler
    {
        private readonly List<UIMgr.StackEntry> _entries;

        internal UIStackHandler(List<UIMgr.StackEntry> entries)
        {
            _entries = entries;
        }

        internal void Clear()
        {
            _entries.Clear();
        }

        internal void CommitVisible(int rootId, int activeId, IUIParam param, UIType type)
        {
            if (type == UIType.Fixed)
            {
                return;
            }

            if (_entries.Count > 0)
            {
                var lastIndex = _entries.Count - 1;
                var last = _entries[lastIndex];
                if (last.RootId == rootId)
                {
                    _entries[lastIndex] = new UIMgr.StackEntry(rootId, activeId, param, type);
#if UNITY_EDITOR
                    UIMgr.__Debugger_Stack_Event();
#endif
                    return;
                }
            }

            _entries.Add(new UIMgr.StackEntry(rootId, activeId, param, type));
#if UNITY_EDITOR
            UIMgr.__Debugger_Stack_Event();
#endif
        }

        internal void RemoveRoot(int rootId)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].RootId == rootId)
                {
                    _entries.RemoveAt(i);
                }
            }
#if UNITY_EDITOR
            UIMgr.__Debugger_Stack_Event();
#endif
        }

        internal bool ContainsActive(int id)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].ActiveId == id)
                {
                    return true;
                }
            }
            return false;
        }

        internal UIMgr.StackEntry? PeekPrevious(int rootId)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].RootId == rootId)
                {
                    continue;
                }
                return _entries[i];
            }
            return null;
        }
    }
}
