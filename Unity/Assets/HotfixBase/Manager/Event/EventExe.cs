using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ux
{
    public partial class EventMgr
    {
        #region Exe

        interface IEventExe
        {
            void Exe(ref int exeCnt);
        }

        readonly struct EventExe : IEventExe
        {
            readonly int eType;

            public EventExe(int _eType)
            {
                eType = _eType;
            }

            public void Exe(ref int exeCnt)
            {
                if (!Ins._eTypeKeys.TryGetValue(eType, out var keys)) return;
                for (var i = keys.Count - 1; i >= 0; i--)
                {
                    var key = keys[i];
                    if (!Ins._keyEvent.TryGetValue(key, out var evt)) continue;
                    if (Ins._waitDels.Count > 0 && Ins._waitDels.Contains(key)) continue;
                    try
                    {
                        evt?.Run();
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

            public void Exe(ref int exeCnt)
            {
                if (!Ins._eTypeKeys.TryGetValue(eType, out var keys)) return;
                for (var i = keys.Count - 1; i >= 0; i--)
                {
                    var key = keys[i];
                    if (!Ins._keyEvent.TryGetValue(key, out var aEvent)) continue;
                    if (Ins._waitDels.Count > 0 && Ins._waitDels.Contains(key)) continue;
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

            public void Exe(ref int exeCnt)
            {
                if (!Ins._eTypeKeys.TryGetValue(eType, out var keys)) return;
                for (var i = keys.Count - 1; i >= 0; i--)
                {
                    var key = keys[i];
                    if (!Ins._keyEvent.TryGetValue(key, out var aEvent)) continue;
                    if (Ins._waitDels.Count > 0 && Ins._waitDels.Contains(key)) continue;
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

            public void Exe(ref int exeCnt)
            {
                if (!Ins._eTypeKeys.TryGetValue(eType, out var keys)) return;
                for (var i = keys.Count - 1; i >= 0; i--)
                {
                    var key = keys[i];
                    if (!Ins._keyEvent.TryGetValue(key, out var aEvent)) continue;
                    if (Ins._waitDels.Count > 0 && Ins._waitDels.Contains(key)) continue;
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

        #endregion
    }
}