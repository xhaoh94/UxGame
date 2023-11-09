#if UNITY_EDITOR
using System;
using System.Collections.Generic;

public readonly struct EventList
{
    public readonly string _eventType;

    public readonly List<string> events;

    public EventList(string typeStr)
    {
        _eventType = typeStr;
        events = new List<string>();
    }
    public void Add(string _evtData)
    {
        events.Add(_evtData);
    }
    public bool Remove(string _evtData)
    {
        events.Remove(_evtData);
        return events.Count == 0;
    }
}

namespace Ux
{
    public partial class EventMgr
    {
        private readonly Dictionary<string, EventList> type2editor = new Dictionary<string, EventList>();

        public static void __Debugger_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_CallBack?.Invoke(Ins.type2editor);
            }
        }

        public static Action<Dictionary<string, EventList>> __Debugger_CallBack;
    }
}

#endif