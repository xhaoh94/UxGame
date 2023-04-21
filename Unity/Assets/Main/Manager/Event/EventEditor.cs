#if UNITY_EDITOR
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
#endif