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
    public interface IEvenEditor
    {
        long On(string eTypeStr, int eType, FastMethodInfo action);

        long On(string eTypeStr, int eType, object tag, Action action);

        long On<A>(string eTypeStr, int eType, object tag, Action<A> action);

        long On<A, B>(string eTypeStr, int eType, object tag, Action<A, B> action);
        long On<A, B, C>(string eTypeStr, int eType, object tag, Action<A, B, C> action);
    }
    public partial class EventMgr: IEvenEditor
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