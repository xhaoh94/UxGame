#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using static Ux.TimeMgr;

public readonly struct TimeList
{
    public string ExeDesc { get; }
    public List<IHandle> Handles { get; }
    public TimeList(string exeDesc)
    {
        ExeDesc = exeDesc;
        Handles = new List<IHandle>();
    }
    public void Add(IHandle handle)
    {
        Handles.Add(handle);
    }
    public bool Remove(IHandle handle)
    {
        Handles.Remove(handle);
        return Handles.Count == 0;
    }
}

namespace Ux
{
    public partial class TimeMgr
    {
        partial class HandleMap
        {
            public readonly Dictionary<string, TimeList> _descEditor = new Dictionary<string, TimeList>();

            bool __isEvent;
            void __Debugger_Event()
            {
                switch (_timeType)
                {
                    case TimeType.Time:
                        __Debugger_Time_Event();
                        break;
                    case TimeType.Frame:
                        __Debugger_Frame_Event();
                        break;
                    case TimeType.TimeStamp:
                        __Debugger_TimeStamp_Event();
                        break;
                    case TimeType.Cron:
                        __Debugger_Cron_Event();
                        break;
                }
            }
        }
        #region 编辑器

#if UNITY_EDITOR
        public static void __Debugger_Time_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Time_CallBack?.Invoke(Ins._timer._descEditor);
            }
        }

        public static void __Debugger_Frame_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Frame_CallBack?.Invoke(Ins._frame._descEditor);
            }
        }

        public static void __Debugger_TimeStamp_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_TimeStamp_CallBack?.Invoke(Ins._timeStamp._descEditor);
            }
        }

        public static void __Debugger_Cron_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Cron_CallBack?.Invoke(Ins._cron._descEditor);
            }
        }

        public static Action<Dictionary<string, TimeList>> __Debugger_Time_CallBack;
        public static Action<Dictionary<string, TimeList>> __Debugger_Frame_CallBack;
        public static Action<Dictionary<string, TimeList>> __Debugger_TimeStamp_CallBack;
        public static Action<Dictionary<string, TimeList>> __Debugger_Cron_CallBack;

#endif

        #endregion
    }
}
#endif