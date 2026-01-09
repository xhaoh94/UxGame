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
                void Reset();
            }

            class EventExe : IEventExe
            {
                int _eType;
                public void Init(int eType)
                {
                    _eType = eType;
                }
                public void Reset()
                {
                    _eType = 0;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    if (!system._eventTypeKeys.TryGetValue(_eType, out var keys)) return;
                    foreach (var key in keys)
                    {
                        if (!system._keyEvent.TryGetValue(key, out var aEvent)) continue;
                        if (system._waitDels.Count > 0 && system._waitDels.Contains(key)) continue;
                        try
                        {
                            aEvent?.Run(system);
                            exeCnt++;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }
            }

            class EventExe<A> : IEventExe
            {
                int _eType;
                A _a;

                public void Init(int _eType, A _a)
                {
                    this._eType = _eType;
                    this._a = _a;
                }
                public void Reset()
                {
                    _eType = 0;
                    _a = default;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    if (!system._eventTypeKeys.TryGetValue(_eType, out var keys)) return;
                    foreach (var key in keys)
                    {
                        if (!system._keyEvent.TryGetValue(key, out var aEvent)) continue;
                        if (system._waitDels.Count > 0 && system._waitDels.Contains(key)) continue;
                        try
                        {
                            aEvent?.Run(system, _a);
                            exeCnt++;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }
            }

            class EventExe<A, B> : IEventExe
            {
                int _eType;
                A _a;
                B _b;

                public void Init(int _eType, A _a, B _b)
                {
                    this._eType = _eType;
                    this._a = _a;
                    this._b = _b;
                }

                public void Reset()
                {
                    _eType = 0;
                    _a = default;
                    _b = default;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    if (!system._eventTypeKeys.TryGetValue(_eType, out var keys)) return;
                    foreach (var key in keys)
                    {
                        if (!system._keyEvent.TryGetValue(key, out var aEvent)) continue;
                        if (system._waitDels.Count > 0 && system._waitDels.Contains(key)) continue;
                        try
                        {
                            aEvent?.Run(system, _a, _b);
                            exeCnt++;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }
            }

            class EventExe<A, B, C> : IEventExe
            {
                int _eType;
                A _a;
                B _b;
                C _c;

                public void Init(int eType, A a, B b, C c)
                {
                    _eType = eType;
                    _a = a;
                    _b = b;
                    _c = c;
                }

                public void Reset()
                {
                    _eType = 0;
                    _a = default;
                    _b = default;
                    _c = default;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    if (!system._eventTypeKeys.TryGetValue(_eType, out var keys)) return;
                    foreach (var key in keys)
                    {
                        if (!system._keyEvent.TryGetValue(key, out var aEvent)) continue;
                        if (system._waitDels.Count > 0 && system._waitDels.Contains(key)) continue;
                        try
                        {
                            aEvent?.Run(system, _a, _b, _c);
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