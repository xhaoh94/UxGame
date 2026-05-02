using System;
using System.Collections.Generic;

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
                bool SkipInQueue { get; set; }
            }

            class EventExe : IEventExe
            {
                public bool SkipInQueue { get; set; }
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
                    if (!system.TryPrepareDispatchKeys(_eType, out List<long> keys)) return;
                    foreach (var key in keys)
                    {
                        if (exeCnt >= system._exeLimit) break;
                        if (!system._keyEvent.TryGetValue(key, out var evt)) continue;
                        if (system._waitDels.Contains(key)) continue;
                        try
                        {
                            evt.Run0(system);
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
                public bool SkipInQueue { get; set; }
                int _eType;
                A _a;

                public void Init(int eType, A a)
                {
                    SkipInQueue = false;
                    _eType = eType;
                    _a = a;
                }

                public void Reset()
                {
                    SkipInQueue = false;
                    _eType = 0;
                    _a = default;
                }

                public void Exe(EventSystem system, ref int exeCnt)
                {
                    if (!system.TryPrepareDispatchKeys(_eType, out List<long> keys)) return;
                    foreach (var key in keys)
                    {
                        if (exeCnt >= system._exeLimit) break;
                        if (!system._keyEvent.TryGetValue(key, out var evt)) continue;
                        if (system._waitDels.Contains(key)) continue;
                        try
                        {
                            evt.Run1(system, _a);
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
                public bool SkipInQueue { get; set; }
                int _eType;
                A _a;
                B _b;

                public void Init(int eType, A a, B b)
                {
                    SkipInQueue = false;
                    _eType = eType;
                    _a = a;
                    _b = b;
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
                    if (!system.TryPrepareDispatchKeys(_eType, out List<long> keys)) return;
                    foreach (var key in keys)
                    {
                        if (exeCnt >= system._exeLimit) break;
                        if (!system._keyEvent.TryGetValue(key, out var evt)) continue;
                        if (system._waitDels.Contains(key)) continue;
                        try
                        {
                            evt.Run2(system, _a, _b);
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
                public bool SkipInQueue { get; set; }
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
                    if (!system.TryPrepareDispatchKeys(_eType, out List<long> keys)) return;
                    foreach (var key in keys)
                    {
                        if (exeCnt >= system._exeLimit) break;
                        if (!system._keyEvent.TryGetValue(key, out var evt)) continue;
                        if (system._waitDels.Contains(key)) continue;
                        try
                        {
                            evt.Run3(system, _a, _b, _c);
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
