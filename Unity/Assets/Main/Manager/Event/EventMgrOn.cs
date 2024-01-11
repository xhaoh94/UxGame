using System;
using System.Collections;
using UnityEngine;

namespace Ux
{
    public partial class EventMgr
    {

        Action _quitEvent;

        public void OnQuit(Action action)
        {
            _quitEvent += action;
        }
        public void OffQuit(Action action)
        {
            _quitEvent -= action;
        }

        public void OnApplicationQuit()
        {
            _quitEvent?.Invoke();
        }


#if UNITY_EDITOR
        long IEvenEditor.On(string eTypeStr, int eType, FastMethodInfo action)
        {
            var evtData = _Add(out var key, eType, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }
        long IEvenEditor.On(string eTypeStr, int eType, object tag, Action action)
        {
            var evtData = _Add<EventData>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }
        long IEvenEditor.On<A>(string eTypeStr, int eType, object tag, Action<A> action)
        {
            var evtData = _Add<EventData<A>>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }
        long IEvenEditor.On<A, B>(string eTypeStr, int eType, object tag, Action<A, B> action)
        {
            var evtData = _Add<EventData<A, B>>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }
        long IEvenEditor.On<A, B, C>(string eTypeStr, int eType, object tag, Action<A, B, C> action)
        {
            var evtData = _Add<EventData<A, B, C>>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }
#endif
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
    }
}