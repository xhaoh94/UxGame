#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Ux
{
    public partial class UIMgr
    {
        /// <summary>
        /// 调试器事件触发函数（仅编辑器模式）
        /// 在游戏运行时触发所有调试回调，用于UI调试面板显示
        /// </summary>
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

        /// <summary>
        /// 调试器UI事件（仅编辑器模式）
        /// 通知调试面板UI注册信息已更新
        /// </summary>
        public static void __Debugger_UI_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_UI_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetAllUIData());
            }
        }

        /// <summary>
        /// 调试器已显示UI事件（仅编辑器模式）
        /// 通知调试面板当前显示的UI列表
        /// </summary>
        public static void __Debugger_Showed_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Showed_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetShowedUI());
            }
        }

        /// <summary>
        /// 调试器UI栈事件（仅编辑器模式）
        /// 通知调试面板UI栈信息
        /// </summary>
        public static void __Debugger_Stack_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Stack_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetUIStacks());
            }
        }

        /// <summary>
        /// 调试器正在显示UI事件（仅编辑器模式）
        /// 通知调试面板正在显示过程中的UI列表
        /// </summary>
        public static void __Debugger_Showing_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Showing_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetShowingUI());
            }
        }

        /// <summary>
        /// 调试器缓存UI事件（仅编辑器模式）
        /// 通知调试面板当前缓存的UI列表
        /// </summary>
        public static void __Debugger_Cacel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Cacel_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetCacheUI());
            }
        }

        /// <summary>
        /// 调试器等待销毁UI事件（仅编辑器模式）
        /// 通知调试面板等待销毁的UI列表
        /// </summary>
        public static void __Debugger_WaitDel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_WaitDel_CallBack?.Invoke(((IUIMgrDebuggerAccess)Ins).GetWaitDelUI());
            }
        }

        
        /// <summary>
        /// 调试器资源包引用事件（仅编辑器模式）
        /// 通知调试面板资源包引用信息
        /// </summary>
        public static void __Debugger_Pkg_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Pkg_CallBack?.Invoke(((IResUIDebuggerAccess)ResMgr.Ins).GetPkgRefs());
            }
        }
        
        /// <summary>
        /// UI数据调试回调（仅编辑器模式）
        /// </summary>
        public static Action<Dictionary<string, IUIData>> __Debugger_UI_CallBack;
        
        /// <summary>
        /// 已显示UI调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<string>> __Debugger_Showed_CallBack;
        
        /// <summary>
        /// UI栈调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<UIStack>> __Debugger_Stack_CallBack;
        
        /// <summary>
        /// 正在显示UI调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<string>> __Debugger_Showing_CallBack;
        
        /// <summary>
        /// 缓存UI调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<string>> __Debugger_Cacel_CallBack;
        
        /// <summary>
        /// 等待销毁UI调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<string>> __Debugger_WaitDel_CallBack;
        
        /// <summary>
        /// 资源包引用调试回调（仅编辑器模式）
        /// </summary>
        public static Action<Dictionary<string, UIPkgRef>> __Debugger_Pkg_CallBack;

    }
}
#endif