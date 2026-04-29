#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Ux
{
    public partial class UIMgr
    {

        public static void __Debugger_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                ((IUIMgrDebuggerAccess)Ins).GetAllUIData();
                __Debugger_Stack_Event();
                __Debugger_Showed_Event();
                __Debugger_Showing_Event();
                __Debugger_Cacel_Event();
                __Debugger_WaitDel_Event();
            }
        }

        public static void __Debugger_UI_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_UI_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetAllUIData());
            }
        }

        public static void __Debugger_Showed_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Showed_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetShowedUI());
            }
        }

        public static void __Debugger_Stack_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Stack_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetUIStacks());
            }
        }

        public static void __Debugger_Showing_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Showing_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetShowingUI());
            }
        }

        public static void __Debugger_Cacel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Cacel_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetCacheUI());
            }
        }

        public static void __Debugger_WaitDel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_WaitDel_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetWaitDelUI());
            }
        }

        
        public static void __Debugger_Pkg_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Pkg_CallBack?.Invoke(((IResUIDebuggerAccess)ResMgr.Ins).GetPkgRefs());
            }
        }
        
        public static Action<Dictionary<string, IUIData>> __Debugger_UI_CallBack;
        public static Action<List<string>> __Debugger_Showed_CallBack;
        public static Action<List<UIStack>> __Debugger_Stack_CallBack;
        public static Action<List<string>> __Debugger_Showing_CallBack;
        public static Action<List<string>> __Debugger_Cacel_CallBack;
        public static Action<List<string>> __Debugger_WaitDel_CallBack;
        public static Action<Dictionary<string, UIPkgRef>> __Debugger_Pkg_CallBack;

    }
}
#endif