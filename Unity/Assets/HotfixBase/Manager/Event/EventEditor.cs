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
        public static Action<Dictionary<string, EventList>> __Debugger_CallBack;
        private Type _hotfixEvtType;
        public static void Debugger_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_CallBack?.Invoke(EventMgr.Ins.type2editor);
            }
        }
        public void SetHotfixEvtType(Type type)
        {
            _hotfixEvtType = type;
        }
        void _EditorRemove(IEvent evt)
        {
            if (string.IsNullOrEmpty(evt.MethodName))
            {
                return;
            }
            string eTypeStr;
            if (evt.EType < (int)MainEventType.END)
            {
                eTypeStr = $"MainEventType.{Enum.GetName(typeof(MainEventType), evt.EType)}";
            }
            else
            {
                eTypeStr = $"EventType.{Enum.GetName(_hotfixEvtType, evt.EType)}";
            }
            if (type2editor.TryGetValue(eTypeStr, out var t2eList))
            {
                if (t2eList.Remove(evt.MethodName))
                {
                    type2editor.Remove(eTypeStr);
                    Debugger_Event();
                }
            }
        }

        void _EditorAdd(IEvent evt)
        {
            if (string.IsNullOrEmpty(evt.MethodName))
            {
                return;
            }
            string eTypeStr;
            if (evt.EType < (int)MainEventType.END)
            {
                eTypeStr = $"MainEventType.{Enum.GetName(typeof(MainEventType), evt.EType)}";
            }
            else
            {
                eTypeStr = $"EventType.{Enum.GetName(_hotfixEvtType, evt.EType)}";
            }
            if (!type2editor.TryGetValue(eTypeStr, out var t2eList))
            {
                t2eList = new EventList(eTypeStr);
                type2editor.Add(eTypeStr, t2eList);
            }
            t2eList.Add(evt.MethodName);
            Debugger_Event();
        }
    }
}

#endif