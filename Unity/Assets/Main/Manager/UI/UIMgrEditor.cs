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
                __Debugger_UI_Event();
                __Debugger_Showed_Event();
                __Debugger_Showing_Event();
                __Debugger_Cacel_Event();
                __Debugger_TemCacel_Event();
                __Debugger_WaitDel_Event();
            }
        }

        public static void __Debugger_UI_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new Dictionary<string, IUIData>();
                foreach (var kv in Ins._idUIData)
                {
                    list.Add(kv.Value.IDStr, kv.Value);
                }
                __Debugger_UI_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_Showed_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var kv in Ins._showed)
                {
                    list.Add(kv.Value.IDStr);
                }
                __Debugger_Showed_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_Showing_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var id in Ins._showing)
                {
                    list.Add(Ins.GetUIData(id).IDStr);
                }
                __Debugger_Showing_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_Cacel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var kv in Ins._cacel)
                {
                    list.Add(kv.Value.IDStr);
                }
                __Debugger_Cacel_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_TemCacel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var kv in Ins._temCacel)
                {
                    list.Add(kv.Value.IDStr);
                }
                __Debugger_TemCacel_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_WaitDel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var kv in Ins._waitDels)
                {
                    list.Add(kv.Value.IDStr);
                }
                __Debugger_WaitDel_CallBack?.Invoke(list);
            }
        }

        public static Action<Dictionary<string, IUIData>> __Debugger_UI_CallBack;
        public static Action<List<string>> __Debugger_Showed_CallBack;
        public static Action<List<string>> __Debugger_Showing_CallBack;
        public static Action<List<string>> __Debugger_Cacel_CallBack;
        public static Action<List<string>> __Debugger_TemCacel_CallBack;
        public static Action<List<string>> __Debugger_WaitDel_CallBack;

    }
}
#endif