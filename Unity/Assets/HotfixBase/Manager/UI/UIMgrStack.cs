using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    partial class UIMgr
    {
        /// <summary>
        /// 界面栈
        /// </summary>
        List<UIStack> _stack = new List<UIStack>();
        Stack<UIStack> _backs = new Stack<UIStack>();

        void _ClearStack()
        {
            foreach (var stack in _stack)
            {
                if (stack.ParamIsNew)
                {
                    stack.Param?.Release();
                }
            }
            _stack.Clear();
        }


        //显示完成后，把ui放进栈
        void _ShowedPushStack(IUI ui, IUIParam param, bool checkStack)
        {
            var uiType = ui.Type;
            if (!checkStack || uiType == UIType.Fixed)
            {
                //非checkStack 或 固定面板不需要放入栈中
                //非checkStack （存在于关闭界面触发栈后，重新显示的界面，此时不需要再把界面放进栈中）
                return;
            }

            var parentID = ui.Data.GetParentID();
            if (_stack.Count > 0)
            {
                var lastStack = _stack[_stack.Count - 1];
                if (lastStack.ParentID == parentID)
                {
                    lastStack.ID = ui.ID;
                    lastStack.Param = param;
                    _stack[_stack.Count - 1] = lastStack;
#if UNITY_EDITOR
                    __Debugger_Stack_Event();
#endif
                    return;
                }
            }

            _stack.Add(new UIStack(parentID, ui.ID, param, uiType));
#if UNITY_EDITOR            
            __Debugger_Stack_Event();            
#endif

            if (uiType == UIType.Stack)
            {
                for (var i = _stack.Count - 2; i >= 0; i--)
                {
                    var preStack = _stack[i];
                    var id = preStack.ParentID;
                    if (id != ui.ID)
                    {
                        _HideByStack(preStack, i);
                    }
                    if (preStack.Type == UIType.Stack)
                    {
                        break;
                    }
                }
            }
        }               
        //关闭界面前，检测栈中界面，是否重新打开
        bool _HideBeforePopStack(IUI ui, bool checkStack = false)
        {
            if (_stack.Count > 0)
            {
                var lastIndex = _stack.Count - 1;
                var last = _stack[lastIndex];
                if (last.ID == ui.ID || last.ParentID == ui.ID)
                {
                    _stack.RemoveAt(lastIndex);
#if UNITY_EDITOR
                    __Debugger_Stack_Event();
#endif
                }
            }
            bool isBreak = false;
            if (checkStack && ui.Type == UIType.Stack)
            {
                _backs.Clear();
                for (int i = _stack.Count - 1; i >= 0; i--)
                {
                    var preStack = _stack[i];
                    if (preStack.ParamIsNew)
                    {
                        preStack.ParamIsNew = false;
                        _stack[i] = preStack;
                    }
                    if (preStack.Type == UIType.Stack)
                    {
                        if (ui.ID == preStack.ID)
                        {
                            isBreak = true;
                        }
                        else
                        {
                            _backs.Push(preStack);
                        }
                        break;
                    }
                    else
                    {
                        _backs.Push(preStack);
                    }
                }
                while (_backs.Count > 0)
                {
                    var preStack = _backs.Pop();
                    _ShowByStack(preStack);
                }
            }
            return isBreak;
        }
        void _ShowByStack(UIStack stack)
        {
            _ShowAsync<IUI>(stack.ID, stack.Param, false, false).Forget();
        }
        void _HideByStack(UIStack stack, int index)
        {
            bool needCopyParam = true;
            IUI ui = null;
            var id = stack.ParentID;
            if (_showing.Contains(id))
            {
                if (!_createdDels.Contains(id)) _createdDels.Add(id);
            }
            else if (_showed.TryGetValue(id, out ui) && (ui.State == UIState.HideAnim || ui.State == UIState.Hide))
            {
                ui = null;
                needCopyParam = false;
            }

            if (needCopyParam)
            {
                //Hide之后，参数会被回收。所以提前拷贝参数对象
                var copyParam = stack.Param?.Copy();
                //重新赋值参数
                if (copyParam != null)
                {
                    stack.Param = copyParam;
                    stack.ParamIsNew = true;
                    _stack[index] = stack;
                }
            }
            ui?.DoHide(false, false);
        }
    }
}
