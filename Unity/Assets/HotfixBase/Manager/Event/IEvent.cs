using System;
using Cysharp.Threading.Tasks;

using System.Runtime.CompilerServices;

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
        public struct EventTask<A>
        {
            public EventTask(long key, UniTask<A> task)
            {
                Key = key;
                Task = task;
            }
            public long Key { get; }
            public UniTask<A> Task { get; }
        }
        public interface IEvent
        {
            void Run0(EventSystem system);
            void Run1<T>(EventSystem system, T a);
            void Run2<T, U>(EventSystem system, T a, U b);
            void Run3<T, U, V>(EventSystem system, T a, U b, V c);
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


            public abstract void Run0(EventSystem system);
            public abstract void Run1<P1>(EventSystem system, P1 a);
            public abstract void Run2<P1, P2>(EventSystem system, P1 a, P2 b);
            public abstract void Run3<P1, P2, P3>(EventSystem system, P1 a, P2 b, P3 c);

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

            public override void Run0(EventSystem system)
            {
                if (_method.IsValid) _method.Invoke();
            }

            public override void Run1<T>(EventSystem system, T a)
            {
                if (_method.IsValid) _method.Invoke(a);
            }

            public override void Run2<T, U>(EventSystem system, T a, U b)
            {
                if (_method.IsValid) _method.Invoke(a, b);
            }

            public override void Run3<T, U, V>(EventSystem system, T a, U b, V c)
            {
                if (_method.IsValid) _method.Invoke(a, b, c);
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

            public override void Run0(EventSystem system)
            {
                _fn?.Invoke();
            }

            public override void Run1<T>(EventSystem system, T a)
            {
                _fn?.Invoke();
            }

            public override void Run2<T, U>(EventSystem system, T a, U b)
            {
                _fn?.Invoke();
            }

            public override void Run3<T, U, V>(EventSystem system, T a, U b, V c)
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


            public override void Run0(EventSystem system) { }

            public override void Run1<T>(EventSystem system, T a)
            {
                if (a is A pA) _fn?.Invoke(pA);
            }

            public override void Run2<T, U>(EventSystem system, T a, U b) { }

            public override void Run3<T, U, V>(EventSystem system, T a, U b, V c) { }

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


            public override void Run0(EventSystem system) { }

            public override void Run1<T>(EventSystem system, T a) { }

            public override void Run2<T, U>(EventSystem system, T a, U b)
            {
                if (a is A pA && b is B pB) _fn?.Invoke(pA, pB);
            }

            public override void Run3<T, U, V>(EventSystem system, T a, U b, V c) { }

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

            public override void Run0(EventSystem system) { }
            public override void Run1<T>(EventSystem system, T a) { }
            public override void Run2<T, U>(EventSystem system, T a, U b) { }
            public override void Run3<T, U, V>(EventSystem system, T a, U b, V c)
            {
                if (a is A pA && b is B pB && c is C pC) _fn?.Invoke(pA, pB, pC);
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

            public override void Run0(EventSystem system)
            {
                system.RemoveByKey(Key);
                _fn.TrySetResult();
            }
            public override void Run1<T>(EventSystem system, T a) => Run0(system);
            public override void Run2<T, U>(EventSystem system, T a, U b) => Run0(system);
            public override void Run3<T, U, V>(EventSystem system, T a, U b, V c) => Run0(system);
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

            public override void Run0(EventSystem system)
            {
                system.RemoveByKey(Key);
            }
            public override void Run1<T>(EventSystem system, T a)
            {
                system.RemoveByKey(Key);
                if (typeof(T) == typeof(A))
                {
                    _fn.TrySetResult(Unsafe.As<T, A>(ref a));
                }
            }
            public override void Run2<T, U>(EventSystem system, T a, U b) => Run0(system);
            public override void Run3<T, U, V>(EventSystem system, T a, U b, V c) => Run0(system);
        }
        
    }
}
