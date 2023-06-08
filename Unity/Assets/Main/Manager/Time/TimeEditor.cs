#if UNITY_EDITOR
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
#endif