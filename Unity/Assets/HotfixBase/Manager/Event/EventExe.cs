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
                bool SkipInQueue {get;set;}
            }
            
            static class EventExeHelper
            {
                public delegate void EventRunner(IEvent evt, EventSystem system);
                
                public static void ExecuteEvents(EventSystem system, int eType, EventRunner runner, ref int exeCnt)
                {
                    if (!system._eventTypeKeys.TryGetValue(eType, out var keys)) return;
                    foreach (var key in keys)
                    {
                        if (!system._keyEvent.TryGetValue(key, out var aEvent)) continue;
                        if (system._waitDels.Contains(key)) continue;
                        try
                        {
                            runner(aEvent, system);
                            exeCnt++;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }
            }

            class EventExe : IEventExe
            {
                public bool SkipInQueue {get;set;} = false;
                int _eType;
                public void Init(int eType)
                {
                    SkipInQueue = false;
                    _eType = eType;
                }
                public void Reset()
                {
                    SkipInQueue = false;
                    _eType = 0;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    EventExeHelper.ExecuteEvents(system, _eType, (evt, sys) => evt.Run(sys), ref exeCnt);
                }
            }

            class EventExe<A> : IEventExe
            {
                public bool SkipInQueue {get;set;} = false;
                int _eType;
                A _a;

                public void Init(int _eType, A _a)
                {
                    SkipInQueue = false;
                    this._eType = _eType;
                    this._a = _a;
                }
                public void Reset()
                {
                    SkipInQueue = false;
                    _eType = 0;
                    _a = default;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    EventExeHelper.ExecuteEvents(system, _eType, (evt, sys) => evt.Run(sys, _a), ref exeCnt);
                }
            }

            class EventExe<A, B> : IEventExe
            {
                public bool SkipInQueue {get;set;} = false;
                int _eType;
                A _a;
                B _b;

                public void Init(int _eType, A _a, B _b)
                {
                    SkipInQueue = false;
                    this._eType = _eType;
                    this._a = _a;
                    this._b = _b;
                }

                public void Reset()
                {
                    SkipInQueue = false;
                    _eType = 0;
                    _a = default;
                    _b = default;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    EventExeHelper.ExecuteEvents(system, _eType, (evt, sys) => evt.Run(sys, _a, _b), ref exeCnt);
                }
            }

            class EventExe<A, B, C> : IEventExe
            {
                public bool SkipInQueue {get;set;} = false;
                int _eType;
                A _a;
                B _b;
                C _c;

                public void Init(int eType, A a, B b, C c)
                {
                    SkipInQueue = false;
                    _eType = eType;
                    _a = a;
                    _b = b;
                    _c = c;
                }

                public void Reset()
                {
                    SkipInQueue = false;
                    _eType = 0;
                    _a = default;
                    _b = default;
                    _c = default;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    EventExeHelper.ExecuteEvents(system, _eType, (evt, sys) => evt.Run(sys, _a, _b, _c), ref exeCnt);
                }
            }

        }
    }
}