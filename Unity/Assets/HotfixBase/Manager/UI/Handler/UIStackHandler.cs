using System.Collections.Generic;
using static Ux.UIMgr;

namespace Ux
{
    public class UIStackHandler
    {
        private readonly IUIStackHandlerCallback _callback;
        private readonly List<UIStack> _uiStacks = new List<UIStack>();
        private readonly Stack<UIStack> _backStacks = new Stack<UIStack>();

        public List<UIStack> UIStacks => _uiStacks;

        public UIStackHandler(IUIStackHandlerCallback callback)
        {
            _callback = callback;
        }

        public void Clear()
        {
            foreach (var stack in _uiStacks)
            {
                if (stack.ParamIsNew)
                {
                    stack.Param?.Release();
                }
            }
            _uiStacks.Clear();
            _backStacks.Clear();
        }

        public void OnShowed(IUI ui, IUIParam param, bool checkStack)
        {
            var uiType = ui.Type;
            if (!checkStack || uiType == UIType.Fixed)
            {
                return;
            }

            var parentID = ui.Data.GetParentID();
            if (_uiStacks.Count > 0)
            {
                var lastStack = _uiStacks[_uiStacks.Count - 1];
                if (lastStack.ParentID == parentID)
                {
                    lastStack.ID = ui.ID;
                    lastStack.Param = param;
                    _uiStacks[_uiStacks.Count - 1] = lastStack;
#if UNITY_EDITOR
                    __Debugger_Stack_Event();
#endif
                    return;
                }
            }

            _uiStacks.Add(new UIStack(parentID, ui.ID, param, uiType));
#if UNITY_EDITOR
            __Debugger_Stack_Event();
#endif
            if (uiType == UIType.Stack)
            {
                for (var i = _uiStacks.Count - 2; i >= 0; i--)
                {
                    var preStack = _uiStacks[i];
                    if (preStack.ParentID == ui.ID)
                    {
                        continue;
                    }
                    _HideByStack(preStack, i);
                    if (preStack.Type == UIType.Stack)
                    {
                        break;
                    }
                }
            }
        }

        public bool OnHide(IUI ui, bool checkStack = false)
        {
            if (_uiStacks.Count > 0)
            {
                var lastIndex = _uiStacks.Count - 1;
                var last = _uiStacks[lastIndex];
                if (last.ID == ui.ID || last.ParentID == ui.ID)
                {
                    _uiStacks.RemoveAt(lastIndex);
#if UNITY_EDITOR
                    __Debugger_Stack_Event();
#endif
                }
            }
            bool shouldBreak = false;
            if (checkStack && ui.Type == UIType.Stack)
            {
                _backStacks.Clear();
                for (int i = _uiStacks.Count - 1; i >= 0; i--)
                {
                    var preStack = _uiStacks[i];
                    if (preStack.ParamIsNew)
                    {
                        preStack.ParamIsNew = false;
                        _uiStacks[i] = preStack;
                    }
                    if (preStack.Type == UIType.Stack)
                    {
                        if (ui.ID == preStack.ID)
                        {
                            shouldBreak = true;
                        }
                        else
                        {
                            _backStacks.Push(preStack);
                        }
                        break;
                    }
                    else
                    {
                        _backStacks.Push(preStack);
                    }
                }
                while (_backStacks.Count > 0)
                {
                    var preStack = _backStacks.Pop();
                    _ShowByStack(preStack);
                }
            }
            return shouldBreak;
        }

        void _ShowByStack(UIStack stack)
        {
            _callback.ShowByStack(stack.ID, stack.Param);
        }

        void _HideByStack(UIStack stack, int index)
        {
            bool needCopyParam = true;
            IUI ui = null;
            var id = stack.ParentID;
            if (_callback.IsShowing(id))
            {
                if (!_callback.IsInCreatedDels(id)) _callback.AddToCreatedDels(id);
            }
            else
            {
                ui = _callback.GetShowed(id);
                if (ui != null && (ui.State == UIState.HideAnim || ui.State == UIState.Hide))
                {
                    ui = null;
                    needCopyParam = false;
                }
            }

            if (needCopyParam)
            {
                var copyParam = stack.Param?.Copy();
                if (copyParam != null)
                {
                    stack.Param = copyParam;
                    stack.ParamIsNew = true;
                    _uiStacks[index] = stack;
                }
            }
            ui?.DoHide(false, false);
        }
    }
}