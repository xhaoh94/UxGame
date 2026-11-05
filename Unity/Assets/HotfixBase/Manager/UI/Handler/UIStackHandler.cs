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

        internal void CommitVisible(int rootId, int activeId, int activeTypeId, IUIParam param, UIType type)
        {
            // 只收 Stackable：新加的枚举值默认不进栈，漏配也不会污染返回链。
            if (type != UIType.Stackable)
            {
                return;
            }

            if (_entries.Count > 0)
            {
                var lastIndex = _entries.Count - 1;
                var last = _entries[lastIndex];
                if (last.RootId == rootId)
                {
                    _entries[lastIndex] = new UIMgr.StackEntry(rootId, activeId, activeTypeId, param, type);
#if UNITY_EDITOR
                    UIMgr.__Debugger_Stack_Event();
#endif
                    return;
                }
            }

            _entries.Add(new UIMgr.StackEntry(rootId, activeId, activeTypeId, param, type));
#if UNITY_EDITOR
            UIMgr.__Debugger_Stack_Event();
#endif
        }

        /// <summary>从栈中移除根界面。返回是否真的移除了条目 —— 没入过栈的界面（窗口/弹窗）不该触发回退。</summary>
        internal bool RemoveRoot(int rootId)
        {
            var removed = false;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].RootId == rootId)
                {
                    _entries.RemoveAt(i);
                    removed = true;
                }
            }
#if UNITY_EDITOR
            if (removed)
                UIMgr.__Debugger_Stack_Event();
#endif
            return removed;
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
