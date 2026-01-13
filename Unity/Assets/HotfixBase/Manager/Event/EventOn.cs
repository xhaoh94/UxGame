using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    public interface IEventOn
    {
        EventMgr.EventTask Call(int eType, object tag);
        EventMgr.EventTask Call<A>(int eType, object tag);
        long On(int eType, object tag, Action action);
        long On<A>(int eType, object tag, Action<A> action);
        long On<A, B>(int eType, object tag, Action<A, B> action);
        long On<A, B, C>(int eType, object tag, Action<A, B, C> action);
    }
    partial class EventMgr : IEventOn
    {
        partial class EventSystem
        {
            public long On(int eType, FastMethodInfo action)
            {
                var evtData = _Add(out var key, eType, action);
                if (evtData != default)
                {
                    evtData.Init(key, eType, action);
                }

                return key;
            }

            public long On(int eType, object tag, Action action)
            {
                var evtData = _Add<EventData>(out var key, eType, tag, action);
                if (evtData != default)
                {
                    evtData.Init(key, eType, tag, action);
                }
                return key;
            }

            public long On<A>(int eType, object tag, Action<A> action)
            {
                var evtData = _Add<EventData<A>>(out var key, eType, tag, action);
                if (evtData != default)
                {
                    evtData.Init(key, eType, tag, action);
                }
                return key;
            }

            public long On<A, B>(int eType, object tag, Action<A, B> action)
            {
                var evtData = _Add<EventData<A, B>>(out var key, eType, tag, action);
                if (evtData != default)
                {
                    evtData.Init(key, eType, tag, action);
                }
                return key;
            }

            public long On<A, B, C>(int eType, object tag, Action<A, B, C> action)
            {
                var evtData = _Add<EventData<A, B, C>>(out var key, eType, tag, action);
                if (evtData != default)
                {
                    evtData.Init(key, eType, tag, action);
                }
                return key;
            }

            public EventTask Call(int eType, object tag)
            {
                var task = AutoResetUniTaskCompletionSource.Create();
                // 使用自增ID
                var key = IDGenerater.GenerateId();
                var evtData = _Add<EventTaskData>(key);
                if (evtData != default)
                {
                    evtData.Init(key, eType, tag, task);
                }
                return new EventTask(key, task.Task);
            }
             public EventTask Call<A>(int eType, object tag)
            {
                var task = AutoResetUniTaskCompletionSource<A>.Create();
                // 使用自增ID
                var key = IDGenerater.GenerateId();
                var evtData = _Add<EventTaskData<A>>(key);
                if (evtData != default)
                {
                    evtData.Init(key, eType, tag, task);
                }
                return new EventTask(key, task.Task);
            }
        }
        long IEventOn.On(int eType, object tag, Action action)
        {
            return _defaultSystem.On(eType, tag, action);
        }

        long IEventOn.On<A>(int eType, object tag, Action<A> action)
        {
            return _defaultSystem.On(eType, tag, action);
        }

        long IEventOn.On<A, B>(int eType, object tag, Action<A, B> action)
        {
            return _defaultSystem.On(eType, tag, action);
        }

        long IEventOn.On<A, B, C>(int eType, object tag, Action<A, B, C> action)
        {
            return _defaultSystem.On(eType, tag, action);
        }
        EventTask IEventOn.Call(int eType, object tag)
        {
            return _defaultSystem.Call(eType, tag);
        }
        EventTask IEventOn.Call<A>(int eType, object tag)
        {
            return _defaultSystem.Call<A>(eType, tag);
        }
    }
}