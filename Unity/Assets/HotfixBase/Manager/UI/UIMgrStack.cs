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

        //显示完成后，把ui放进栈
        void _ShowCallBack_Stack(IUI ui, IUIParam param, bool isStack)
        {
            var uiType = ui.Type;
            if (!isStack || uiType == UIType.Fixed)
            {
                //非isStack 或 固定面板不需要放入栈中
                //非isStack （存在于关闭界面触发栈后，重新显示的界面，此时不需要再把界面放进栈中）
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
#if UNITY_EDITOR
                    lastStack.IDStr = ui.Name;
#endif
                    _stack[_stack.Count - 1] = lastStack;

#if UNITY_EDITOR
                    __Debugger_Stack_Event();
#endif
                    return;
                }
            }
#if UNITY_EDITOR
            _stack.Add(new UIStack(parentID, ui.Name, ui.ID, param, uiType));
            __Debugger_Stack_Event();
#else
            _stack.Add(new UIStack(parentID, ui.ID, param, uiType));
#endif

            if (uiType == UIType.Stack)
            {
                for (var i = _stack.Count - 2; i >= 0; i--)
                {
                    var preStack = _stack[i];
                    if (preStack.ID != ui.ID)
                    {
                        //Hide之后，参数会被回收。所以提前拷贝参数对象
                        var copyParam = preStack.Param?.Copy();
                        Hide(preStack.ParentID);
                        //重新赋值参数
                        preStack.Param = copyParam;
                    }
                    if (preStack.Type == UIType.Stack)
                    {
                        break;
                    }
                }
            }

        }

        //关闭界面前，检测栈中界面，是否重新打开
        bool _HideBefore_Stack(IUI ui, bool isStack = false)
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
            if (isStack && ui.Type == UIType.Stack)
            {
                _backs.Clear();
                for (int i = _stack.Count - 1; i >= 0; i--)
                {
                    var preStack = _stack[i];
                    if (preStack.Type == UIType.Stack)
                    {
                        if (ui.ID == preStack.ID)
                        {
                            isBreak = true;
                        }
                        else
                        {
                            _ShowByStack(preStack.ID, preStack.Param);
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
                    _ShowByStack(preStack.ID, preStack.Param);
                }
            }
            return isBreak;
        }

        //关闭界面且触发检测栈
        void _Hide_Stack(int id, bool isAnim)
        {
            _Hide(true, id, isAnim);
        }

    }
}
