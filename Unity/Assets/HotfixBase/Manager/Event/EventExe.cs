using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ux
{
    public partial class EventMgr
    {
        public partial class EventSystem
        {
            interface IEventExe
            {
                void Exe(EventSystem system, ref int exeCnt);
            }

            readonly struct EventExe : IEventExe
            {
                readonly int _eType;                

                public EventExe(int eType)
                {
                    _eType = eType;                   
                }

                public void Exe(EventSystem system,ref int exeCnt)
                {
                    if (!system._eTypeKeys.TryGetValue(_eType, out var keys)) return;
                    var enumerator = keys.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var key = enumerator.Current;
                        if (!system._keyEvent.TryGetValue(key, out var aEvent)) continue;
                        if (system._waitDels.Count > 0 && system._waitDels.Contains(key)) continue;
                        try
                        {
                            aEvent?.Run();
                            exeCnt++;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }
            }

            readonly struct EventExe<A> : IEventExe
            {
                readonly int eType;
                readonly A a;

                public EventExe(int _eType, A _a)
                {
                    eType = _eType;
                    a = _a;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    if (!system._eTypeKeys.TryGetValue(eType, out var keys)) return;
                    var enumerator = keys.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var key = enumerator.Current;
                        if (!system._keyEvent.TryGetValue(key, out var aEvent)) continue;
                        if (system._waitDels.Count > 0 && system._waitDels.Contains(key)) continue;
                        try
                        {
                            aEvent?.Run(a);
                            exeCnt++;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }
            }

            readonly struct EventExe<A, B> : IEventExe
            {
                readonly int eType;
                readonly A a;
                readonly B b;

                public EventExe(int _eType, A _a, B _b)
                {
                    eType = _eType;
                    a = _a;
                    b = _b;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    if (!system._eTypeKeys.TryGetValue(eType, out var keys)) return;
                    var enumerator = keys.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var key = enumerator.Current;
                        if (!system._keyEvent.TryGetValue(key, out var aEvent)) continue;
                        if (system._waitDels.Count > 0 && system._waitDels.Contains(key)) continue;
                        try
                        {
                            aEvent?.Run(a, b);
                            exeCnt++;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }
            }

            readonly struct EventExe<A, B, C> : IEventExe
            {
                readonly int eType;
                readonly A a;
                readonly B b;
                readonly C c;

                public EventExe(int _eType, A _a, B _b, C _c)
                {
                    eType = _eType;
                    a = _a;
                    b = _b;
                    c = _c;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    if (!system._eTypeKeys.TryGetValue(eType, out var keys)) return;
                    var enumerator = keys.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var key = enumerator.Current;
                        if (!system._keyEvent.TryGetValue(key, out var aEvent)) continue;
                        if (system._waitDels.Count > 0 && system._waitDels.Contains(key)) continue;
                        try
                        {
                            aEvent?.Run(a, b, c);
                            exeCnt++;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }
            }

        }
    }
}