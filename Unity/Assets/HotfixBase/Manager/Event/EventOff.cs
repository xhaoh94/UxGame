using System;
using System.Collections.Generic;

namespace Ux
{
    public interface IEventOff
    {
        void Off(int eType, object tag, Action action);
        void Off<A>(int eType, object tag, Action<A> action);
        void Off<A, B>(int eType, object tag, Action<A, B> action);
        void Off<A, B, C>(int eType, object tag, Action<A, B, C> action);
    }

    partial class EventMgr : IEventOff
    {
        public EventSystem RemoveByKey(IEnumerable<long> keys)
        {
            return _defaultSystem.RemoveByKey(keys);
        }

        public EventSystem RemoveByKey(long key)
        {
            return _defaultSystem.RemoveByKey(key);
        }

        partial class EventSystem
        {
            public void Off(int eType, FastMethodInfo action)
            {
                if (TryGetKey(eType, action, out var key))
                {
                    RemoveByKey(key);
                }
            }

            public void Off(int eType, object tag, Action action)
            {
                _Remove(eType, tag, action);
            }

            public void Off(Action action)
            {
                _Remove(action);
            }

            public void Off<A>(int eType, object tag, Action<A> action)
            {
                _Remove(eType, tag, action);
            }

            public void Off<A>(Action<A> action)
            {
                _Remove(action);
            }

            public void Off<A, B>(int eType, object tag, Action<A, B> action)
            {
                _Remove(eType, tag, action);
            }

            public void Off<A, B>(Action<A, B> action)
            {
                _Remove(action);
            }

            public void Off<A, B, C>(int eType, object tag, Action<A, B, C> action)
            {
                _Remove(eType, tag, action);
            }

            public void Off<A, B, C>(Action<A, B, C> action)
            {
                _Remove(action);
            }

            public void OffTag(object tag)
            {
                if (tag == null) return;
                RemovePendingAdds(evt => evt.Tag == tag);

                if (!_tagKeys.TryGetValue(tag, out var keys)) return;
                RemoveByKey(keys);
            }
        }

        void IEventOff.Off(int eType, object tag, Action action)
        {
            _defaultSystem.Off(eType, tag, action);
        }

        public void Off(Action action)
        {
            _defaultSystem.Off(action);
        }

        void IEventOff.Off<A>(int eType, object tag, Action<A> action)
        {
            _defaultSystem.Off(eType, tag, action);
        }

        public void Off<A>(Action<A> action)
        {
            _defaultSystem.Off(action);
        }

        void IEventOff.Off<A, B>(int eType, object tag, Action<A, B> action)
        {
            _defaultSystem.Off(eType, tag, action);
        }

        public void Off<A, B>(Action<A, B> action)
        {
            _defaultSystem.Off(action);
        }

        void IEventOff.Off<A, B, C>(int eType, object tag, Action<A, B, C> action)
        {
            _defaultSystem.Off(eType, tag, action);
        }

        public void Off<A, B, C>(Action<A, B, C> action)
        {
            _defaultSystem.Off(action);
        }

        public void OffTag(object tag)
        {
            _defaultSystem.OffTag(tag);
        }
    }
}
