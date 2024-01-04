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

        static Dictionary<string, IUIData> _Debugger_UI_Dict = new Dictionary<string, IUIData>();
        public static void __Debugger_UI_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                _Debugger_UI_Dict.Clear();
                foreach (var kv in Ins._idUIData)
                {
                    _Debugger_UI_Dict.Add(kv.Value.Name, kv.Value);
                }
                __Debugger_UI_CallBack?.Invoke(_Debugger_UI_Dict);
            }
        }

        static List<string> _Debugger_Showeds = new List<string>();
        public static void __Debugger_Showed_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                _Debugger_Showeds.Clear();
                foreach (var kv in Ins._showed)
                {
                    _Debugger_Showeds.Add(kv.Value.Name);
                }
                __Debugger_Showed_CallBack?.Invoke(_Debugger_Showeds);
            }
        }

        public static void __Debugger_Stack_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Stack_CallBack?.Invoke(Ins._stack);
            }
        }
        static List<string> _Debugger_Showings = new List<string>();
        public static void __Debugger_Showing_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                _Debugger_Showings.Clear();
                foreach (var id in Ins._showing)
                {
                    _Debugger_Showings.Add(Ins.GetUIData(id).Name);
                }
                __Debugger_Showing_CallBack?.Invoke(_Debugger_Showings);
            }
        }

        static List<string> _Debugger_Cacels = new List<string>();
        public static void __Debugger_Cacel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                _Debugger_Cacels.Clear();
                foreach (var kv in Ins._cacel)
                {
                    _Debugger_Cacels.Add(kv.Value.Name);
                }
                __Debugger_Cacel_CallBack?.Invoke(_Debugger_Cacels);
            }
        }

        static List<string> _Debugger_TemCacels = new List<string>();
        public static void __Debugger_TemCacel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                _Debugger_TemCacels.Clear();
                foreach (var kv in Ins._temCacel)
                {
                    _Debugger_TemCacels.Add(kv.Value.Name);
                }
                __Debugger_TemCacel_CallBack?.Invoke(_Debugger_TemCacels);
            }
        }
        static List<string> _Debugger_WaitDels = new List<string>();
        public static void __Debugger_WaitDel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                _Debugger_WaitDels.Clear();
                foreach (var kv in Ins._waitDels)
                {
                    _Debugger_WaitDels.Add(kv.Value.Name);
                }
                __Debugger_WaitDel_CallBack?.Invoke(_Debugger_WaitDels);
            }
        }

        public static Action<Dictionary<string, IUIData>> __Debugger_UI_CallBack;
        public static Action<List<string>> __Debugger_Showed_CallBack;
        public static Action<List<UIStack>> __Debugger_Stack_CallBack;
        public static Action<List<string>> __Debugger_Showing_CallBack;
        public static Action<List<string>> __Debugger_Cacel_CallBack;
        public static Action<List<string>> __Debugger_TemCacel_CallBack;
        public static Action<List<string>> __Debugger_WaitDel_CallBack;

    }
}
#endif