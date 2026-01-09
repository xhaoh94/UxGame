using System;
using Cysharp.Threading.Tasks;

namespace Ux
{
    public interface IEventTrigger
    {
        void OnTriggerEvent();
    }
    public partial class EventMgr
    {
        public struct EventTask
        {
            public EventTask(long key, UniTask task)
            {
                Key = key;
                Task = task;
            }
            public long Key { get; }
            public UniTask Task { get; }
        }
        public interface IEvent
        {
            void Run(EventSystem system, params object[] args);
            void Release();

            long Key { get; }
            int EType { get; }
            object Tag { get; }
            Delegate Method { get; }
#if UNITY_EDITOR
            string MethodName { get; }
#endif
        }

        public abstract class EventBaseData : IEvent
        {
            public long Key { get; protected set; }
            public int EType { get; protected set; }
            public object Tag { get; protected set; }
            public abstract Delegate Method { get; }
#if UNITY_EDITOR            
            public virtual string MethodName => Method?.MethodName();
#endif


            public abstract void Run(EventSystem system,params object[] args);

            public void Release()
            {
                Tag = null;
                Key = 0;
                EType = 0;
                OnRelease();
                Pool.Push(this);
            }

            protected abstract void OnRelease();
        }

        public sealed class EventFastMethodData : EventBaseData
        {
            public override Delegate Method => _method.Method;
            FastMethodInfo _method;

#if UNITY_EDITOR
            public override string MethodName => _method.MethodName;
#endif
            public void Init(long key, int eType, FastMethodInfo method)
            {
                Tag = method.Target;
                Key = key;
                EType = eType;
                _method = method;
            }

            protected override void OnRelease()
            {
                _method = default;
            }

            public override void Run(EventSystem system,params object[] args)
            {
                if (_method.IsValid) _method.Invoke();
            }
        }

        public sealed class EventData : EventBaseData
        {
            public override Delegate Method => _fn;
            Action _fn;

            public void Init(long key, int eType, object tag, Action fn)
            {
                Tag = tag;
                Key = key;
                EType = eType;
                _fn = fn;
            }

            protected override void OnRelease()
            {
                _fn = null;
            }

            public override void Run(EventSystem system,params object[] args)
            {
                _fn?.Invoke();
            }

        }

        public sealed class EventData<A> : EventBaseData
        {
            public override Delegate Method => _fn;
            Action<A> _fn;

            public void Init(long key, int eType, object tag, Action<A> fn)
            {
                Tag = tag;
                Key = key;
                EType = eType;
                _fn = fn;
            }


            public override void Run(EventSystem system,params object[] args)
            {
                if (args.Length > 0 && args[0] is A pA) _fn?.Invoke(pA);
            }


            protected override void OnRelease()
            {
                _fn = null;
            }
        }

        public sealed class EventData<A, B> : EventBaseData
        {
            public override Delegate Method => _fn;
            Action<A, B> _fn;

            public void Init(long key, int eType, object tag, Action<A, B> fn)
            {
                Tag = tag;
                Key = key;
                EType = eType;
                _fn = fn;
            }


            public override void Run(EventSystem system,params object[] args)
            {
                if (args.Length > 1 && args[0] is A pA && args[1] is B pB) _fn?.Invoke(pA, pB);
            }


            protected override void OnRelease()
            {
                _fn = null;
            }
        }

        public sealed class EventData<A, B, C> : EventBaseData

        {
            public override Delegate Method => _fn;
            Action<A, B, C> _fn;

            public void Init(long key, int eType, object tag, Action<A, B, C> fn)
            {
                Tag = tag;
                Key = key;
                EType = eType;
                _fn = fn;
            }


            public override void Run(EventSystem system,params object[] args)
            {
                if (args.Length > 2 && args[0] is A pA && args[1] is B pB && args[2] is C pC) _fn?.Invoke(pA, pB, pC);
            }

            protected override void OnRelease()
            {
                _fn = null;
            }
        }
        public sealed class EventTaskData : EventBaseData
        {
            public override Delegate Method => null;
            AutoResetUniTaskCompletionSource _fn;

            public void Init(long key, int eType, object tag, AutoResetUniTaskCompletionSource fn)
            {
                Tag = tag;
                Key = key;
                EType = eType;
                _fn = fn;
            }

            protected override void OnRelease()
            {
                _fn = null;
            }

            public override void Run(EventSystem system,params object[] args)
            {
                system.RemoveByKey(Key);
                _fn.TrySetResult();
            }
        }
        public sealed class EventTaskData<A> : EventBaseData
        {
            public override Delegate Method => null;
            AutoResetUniTaskCompletionSource<A> _fn;

            public void Init(long key, int eType, object tag, AutoResetUniTaskCompletionSource<A> fn)
            {
                Tag = tag;
                Key = key;
                EType = eType;
                _fn = fn;
            }

            protected override void OnRelease()
            {
                _fn = null;
            }

            public override void Run(EventSystem system,params object[] args)
            {
                system.RemoveByKey(Key);
                if (args.Length > 0 && args[0] is A pA) _fn.TrySetResult(pA);
            }
        }
        
    }
}