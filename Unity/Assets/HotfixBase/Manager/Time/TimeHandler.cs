#if UNITY_EDITOR
#endif
using System;

namespace Ux
{
    public partial class TimeMgr
    {

        #region Handle

        public interface IHandle
        {
            RunStatus Run();
            void Release();
            int Compare(IHandle handle);
            long Key { get; }
            Status Status { get; set; }
            object Tag { get; }
            string MethodName { get; }
        }
        public abstract class HandleBase : IHandle
        {
            IHandleExe _exe;
            protected IHandleExe Exe
            {
                get
                {
                    return _exe;
                }
                set
                {
                    if (_exe == value) return;
                    if (_exe != null)
                    {
                        _exe?.Release();
                    }
                    _exe = value;
                }
            }

            public string MethodName => Exe?.MethodName;
            public object Tag => Exe?.Tag;
            public Status Status { get; set; }

            public long Key { get; protected set; }

            public abstract int Compare(IHandle handle);

            public void Release()
            {
                if (Status != Status.WaitDel)
                {
                    Log.Error("出现非WaitDel状态却执行删除的IHandler {0}", MethodName);
                    return;
                }
                Status = Status.Release;
                Key = 0;
                Exe = null;
                OnRelease();
                Pool.Push(this);
            }
            protected virtual void OnRelease()
            {

            }

            public abstract RunStatus Run();
        }
        public interface IHandleExe
        {
            object Tag { get; }
            string MethodName { get; }
            void Run();
            void Release();
        }
        public abstract class HandleExeBase : IHandleExe
        {
            public virtual object Tag { get; protected set; }
            protected abstract Delegate Method { get; }

            public string MethodName => Method.MethodName();

            public void Run()
            {
                try
                {
                    OnRun();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            protected abstract void OnRun();

            public void Release()
            {
                Tag = null;
                OnRelease();
                Pool.Push(this);
            }

            protected abstract void OnRelease();
        }

        class HandleExe : HandleExeBase
        {
            private Action _fn;
            protected override Delegate Method => _fn;

            public void Init(object tag, Action fn)
            {
                Tag = tag;
                _fn = fn;
            }

            protected override void OnRun()
            {
                _fn?.Invoke();
            }

            protected override void OnRelease()
            {
                _fn = null;
            }
        }

        class HandleExe<A> : HandleExeBase
        {
            private Action<A> _fn;
            private A a;

            protected override Delegate Method => _fn;

            public void Init(object tag, Action<A> fn, A _a)
            {
                Tag = tag;
                _fn = fn;
                a = _a;
            }

            protected override void OnRun()
            {
                _fn?.Invoke(a);
            }

            protected override void OnRelease()
            {
                _fn = null;
                a = default;
            }
        }

        class HandleExe<A, B> : HandleExeBase
        {
            private Action<A, B> _fn;
            private A a;
            private B b;
            protected override Delegate Method => _fn;

            public void Init(object tag, Action<A, B> fn, A _a, B _b)
            {
                Tag = tag;
                _fn = fn;
                a = _a;
                b = _b;
            }

            protected override void OnRun()
            {
                _fn?.Invoke(a, b);
            }

            protected override void OnRelease()
            {
                _fn = null;
                a = default;
                b = default;
            }
        }

        class HandleExe<A, B, C> : HandleExeBase
        {
            private Action<A, B, C> _fn;
            private A a;
            private B b;
            private C c;
            protected override Delegate Method => _fn;

            public void Init(object tag, Action<A, B, C> fn, A _a, B _b, C _c)
            {
                Tag = tag;
                _fn = fn;
                a = _a;
                b = _b;
                c = _c;
            }

            protected override void OnRun()
            {
                _fn?.Invoke(a, b, c);
            }

            protected override void OnRelease()
            {
                _fn = null;
                a = default;
                b = default;
                c = default;
            }
        }

        #endregion

        #region Cron

        public interface ICronHandle : IHandle
        {
            bool Init(IHandleExe _exe, long _key, string _cron, bool _isLocalTime);
        }

        public class CronHandle : HandleBase, ICronHandle
        {
            bool isLocalTime;
            public long TimeStamp { get; private set; }

#if UNITY_EDITOR
            public string Cron { get; private set; }
            public string TimeStampDesc { get; private set; }
#endif
            CronData _data;

            public override int Compare(IHandle compare)
            {
                if (compare is CronHandle handle)
                {
                    return TimeStamp.CompareTo(handle.TimeStamp);
                }

                return 0;
            }

            public bool Init(IHandleExe _exe, long _key, string _cron, bool _isLocalTime)
            {
                Exe = _exe;
                Key = _key;
                isLocalTime = _isLocalTime;
                _data = CronData.Create(_cron);
                var nextTime = _data.GetNext((isLocalTime ? Ins.LocalTime : Ins.ServerTime).Now);
                if (nextTime == default)
                {
                    return false;
                }

                TimeStamp = nextTime.ToTimeStamp();
#if UNITY_EDITOR
                TimeStampDesc = TimerHelper.TimeStampToString(TimeStamp);
                Cron = _cron;
#endif
                return true;
            }

            protected override void OnRelease()
            {
                isLocalTime = false;
                TimeStamp = long.MaxValue;
                _data = default;
            }


            public override RunStatus Run()
            {
                if (Status != Status.Normal)
                {
                    return RunStatus.None;
                }

                IGameTime gameTime = isLocalTime ? Ins.LocalTime : Ins.ServerTime;
                if (gameTime.TimeStamp < TimeStamp) return RunStatus.Wait;
                Exe?.Run();
                if (Status != Status.Normal) //以防OnRun业务逻辑给Release掉了
                {
                    return RunStatus.None;
                }

                var nextTime = _data.GetNext(gameTime.Now);
                if (nextTime == default)
                {
                    return RunStatus.Done;
                }

                TimeStamp = nextTime.ToTimeStamp();
#if UNITY_EDITOR
                TimeStampDesc = TimerHelper.TimeStampToString(TimeStamp);
#endif
                return RunStatus.Update;
            }
        }

        #endregion

        #region TimeStamp

        public interface ITimeStampHandle : IHandle
        {
            void Init(IHandleExe _exe, long _key, long _timeStamp, bool _isLocalTime);
        }

        public class TimeStampHandle : HandleBase, ITimeStampHandle
        {
            private bool isLocalTime;
            public long TimeStamp { get; private set; }


#if UNITY_EDITOR
            public string TimeStampDesc { get; private set; }
#endif

            public void Init(IHandleExe _exe, long _key, long _timeStamp, bool _isLocalTime)
            {
                Exe = _exe;
                Key = _key;
                TimeStamp = _timeStamp;
                isLocalTime = _isLocalTime;
#if UNITY_EDITOR
                TimeStampDesc = TimerHelper.TimeStampToString(TimeStamp);
#endif
            }

            public override int Compare(IHandle compare)
            {
                if (compare is TimeStampHandle handle)
                {
                    return TimeStamp.CompareTo(handle.TimeStamp);
                }

                return 0;
            }

            public override RunStatus Run()
            {
                if (Status != Status.Normal)
                {
                    return RunStatus.None;
                }

                if ((isLocalTime ? Ins.LocalTime : Ins.ServerTime).TimeStamp < TimeStamp)
                    return RunStatus.Wait;
                Exe?.Run();
                if (Status != Status.Normal) //以防OnRun业务逻辑给Release掉了
                {
                    return RunStatus.None;
                }

                return RunStatus.Done;
            }


            protected override void OnRelease()
            {
                TimeStamp = long.MaxValue;
                isLocalTime = false;
            }
        }

        #endregion

        #region Time

        public interface ITimeHandle : IHandle
        {
            void Init(IHandleExe _exe, long _key, float _first, float _delay, int _repeat, bool _useFrame,
                Action _complete);

            void Init(IHandleExe _exe, long _key, float _first, float _delay, int _repeat, bool _useFrame,
                Action<object> _complete, object _param);
        }

        public class TimeHandle : HandleBase, ITimeHandle
        {
            public float Delay { get; private set; }
            public int Repeat { get; private set; }
            public bool UseFrame { get; private set; }
            public bool IsLoop { get; private set; }
            public float ExeTime { get; private set; }
            public int ExeCnt { get; private set; }

            Action _complete;
            Action<object> _completeWithParam;
            object _completeParam;

            public void Init(IHandleExe exe, long key, float first, float delay, int repeat, bool useFrame, Action complete)
            {
                Exe = exe;
                Key = key;
                Delay = delay;
                Repeat = repeat;
                UseFrame = useFrame;
                this._complete = complete;
                IsLoop = Repeat <= 0;
                ExeCnt = 0;
                var addTime = first >= 0 ? first : Delay;
                ExeTime = UseFrame ? Ins.TotalFrame + addTime : Ins.TotalTime + addTime;
            }

            public void Init(IHandleExe exe, long key, float first, float delay, int repeat, bool useFrame,
                Action<object> complete, object param)
            {
                Exe = exe;
                Key = key;
                Delay = delay;
                Repeat = repeat;
                UseFrame = useFrame;
                _completeWithParam = complete;
                _completeParam = param;
                IsLoop = Repeat <= 0;
                ExeCnt = 0;
                var addTime = first >= 0 ? first : Delay;
                ExeTime = UseFrame ? Ins.TotalFrame + addTime : Ins.TotalTime + addTime;
            }

            public override int Compare(IHandle compare)
            {
                if (compare is TimeHandle handle)
                {
                    return ExeTime.CompareTo(handle.ExeTime);
                }

                return 0;
            }

            public override RunStatus Run()
            {
                if (Status != Status.Normal)
                {
                    return RunStatus.None;
                }

                float total = UseFrame ? UnityEngine.Time.frameCount : UnityEngine.Time.unscaledTime;
                if (total < ExeTime) return RunStatus.Wait;
                ExeTime += Delay;
                ExeCnt++;
                Exe?.Run();
                if (Status != Status.Normal) //以防Run业务逻辑给Release掉了
                {
                    return RunStatus.None;
                }

                if (IsLoop) return RunStatus.Update;
                Repeat--;
                if (Repeat > 0) return RunStatus.Update;
                _complete?.Invoke();
                _completeWithParam?.Invoke(_completeParam);
                return RunStatus.Done;
            }

            protected override void OnRelease()
            {
                _complete = null;
                _completeWithParam = null;
                _completeParam = null;
                Delay = 0;
                Repeat = 0;
                UseFrame = false;
                IsLoop = false;
                ExeTime = int.MaxValue;
                ExeCnt = 0;
            }
        }

        #endregion
    }
}