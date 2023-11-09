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
            object Target { get; }
            string MethodName { get; }
        }

        public interface IHandleExe
        {
            object Target { get; }
            string MethodName { get; }
            void Run();
            void Release();
        }

        public abstract class HandleExeBase : IHandleExe
        {
            public object Target => Method.Target;
            protected abstract Delegate Method { get; }

            public string MethodName
            {
                get
                {
                    var target = Method.Target;
                    if (target == null)
                    {
                        return Method.Method.ReflectedType != null
                            ? $"静态：{Method.Method.ReflectedType.FullName}.{Method.Method.Name}"
                            : string.Empty;
                    }
                    var targetType = target.GetType();
                    return targetType.Name.Contains("<>c")
                        ? $"匿名：{targetType.FullName}.{Method.Method.Name}"
                        : $"类名：{targetType.FullName}.{Method.Method.Name}";
                }
            }

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
                OnRelease();
                Pool.Push(this);
            }

            protected abstract void OnRelease();
        }

        class HandleExe : HandleExeBase
        {
            private Action _fn;
            protected override Delegate Method => _fn;

            public void Init(Action fn)
            {
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

            public void Init(Action<A> fn, A _a)
            {
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

            public void Init(Action<A, B> fn, A _a, B _b)
            {
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

            public void Init(Action<A, B, C> fn, A _a, B _b, C _c)
            {
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

        public class CronHandle : ICronHandle
        {
            bool isLocalTime;
            IHandleExe exe;
            public string MethodName => exe.MethodName;
            public object Target => exe?.Target;
            public Status Status { get; set; }
            public long Key { get; private set; }
            public long TimeStamp { get; private set; }

#if UNITY_EDITOR
            public string Cron { get; private set; }
            public string TimeStampDesc { get; private set; }
#endif
            CronData _data;

            public int Compare(IHandle compare)
            {
                if (compare is CronHandle handle)
                {
                    return TimeStamp.CompareTo(handle.TimeStamp);
                }

                return 0;
            }

            public bool Init(IHandleExe _exe, long _key, string _cron, bool _isLocalTime)
            {
                exe = _exe;
                Key = _key;
                isLocalTime = _isLocalTime;
                _data = CronData.Create(_cron);
                var list = _data.GetExeTime((isLocalTime ? Ins.LocalTime : Ins.ServerTime).Now);
                if (list == null || list.Count == 0)
                {
                    return false;
                }

                TimeStamp = list[0].ToTimeStamp();
#if UNITY_EDITOR
                TimeStampDesc = TimerHelper.TimeStampToString(TimeStamp);
                Cron = _cron;
#endif
                return true;
            }

            public void Release()
            {
                if (Status != Status.WaitDel)
                {
                    Log.Error("出现非WaitDel状态却执行删除的TimeHandler {0}", MethodName);
                    return;
                }

                Status = Status.Release;
                isLocalTime = false;
                Key = 0;
                exe?.Release();
                exe = null;
                Pool.Push(this);
            }


            public RunStatus Run()
            {
                if (Status != Status.Normal)
                {
                    return RunStatus.None;
                }

                IGameTime gameTime = isLocalTime ? Ins.LocalTime : Ins.ServerTime;
                if (gameTime.TimeStamp < TimeStamp) return RunStatus.Wait;
                exe?.Run();
                if (Status != Status.Normal) //以防OnRun业务逻辑给Release掉了
                {
                    return RunStatus.None;
                }

                var list = _data.GetExeTime(gameTime.Now);
                if (list == null || list.Count == 0)
                {
                    return RunStatus.Done;
                }

                TimeStamp = list[0].ToTimeStamp();
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

        public class TimeStampHandle : ITimeStampHandle
        {
            private bool isLocalTime;
            protected IHandleExe exe;
            public string MethodName => exe?.MethodName;
            public object Target => exe?.Target;
            public Status Status { get; set; }
            public long Key { get; private set; }
            public long TimeStamp { get; private set; }


#if UNITY_EDITOR
            public string TimeStampDesc { get; private set; }
#endif

            public void Init(IHandleExe _exe, long _key, long _timeStamp, bool _isLocalTime)
            {
                exe = _exe;
                Key = _key;
                TimeStamp = _timeStamp;
                isLocalTime = _isLocalTime;
#if UNITY_EDITOR
                TimeStampDesc = TimerHelper.TimeStampToString(TimeStamp);
#endif
            }

            public int Compare(IHandle compare)
            {
                if (compare is TimeStampHandle handle)
                {
                    return TimeStamp.CompareTo(handle.TimeStamp);
                }

                return 0;
            }

            public RunStatus Run()
            {
                if (Status != Status.Normal)
                {
                    return RunStatus.None;
                }

                if ((isLocalTime ? Ins.LocalTime : Ins.ServerTime).TimeStamp < TimeStamp)
                    return RunStatus.Wait;
                exe?.Run();
                if (Status != Status.Normal) //以防OnRun业务逻辑给Release掉了
                {
                    return RunStatus.None;
                }

                return RunStatus.Done;
            }


            public void Release()
            {
                if (Status != Status.WaitDel)
                {
                    Log.Error("出现非WaitDel状态却执行删除的TimeHandler {0}", MethodName);
                    return;
                }

                Status = Status.Release;
                isLocalTime = false;
                Key = 0;
                exe?.Release();
                exe = null;
                Pool.Push(this);
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

        public class TimeHandle : ITimeHandle
        {
            protected IHandleExe exe;

            public string MethodName => exe?.MethodName;
            public object Target => exe?.Target;
            public Status Status { get; set; }

            public long Key { get; private set; }
            public float Delay { get; private set; }
            public int Repeat { get; private set; }
            public bool UseFrame { get; private set; }
            public bool IsLoop { get; private set; }
            public float ExeTime { get; private set; }
            public int ExeCnt { get; private set; }

            Action _complete1;
            Action<object> _complete2;
            object _completeParam;

            public void Init(IHandleExe _exe, long _key, float _first, float _delay, int _repeat, bool _useFrame,
                Action _complete)
            {
                exe = _exe;
                Key = _key;
                Delay = _delay;
                Repeat = _repeat;
                UseFrame = _useFrame;
                _complete1 = _complete;
                IsLoop = Repeat <= 0;
                ExeCnt = 0;
                var addTime = _first >= 0 ? _first : Delay;
                ExeTime = UseFrame ? Ins.TotalFrame + addTime : Ins.TotalTime + addTime;
            }

            public void Init(IHandleExe _exe, long _key, float _first, float _delay, int _repeat, bool _useFrame,
                Action<object> _complete, object _param)
            {
                exe = _exe;
                Key = _key;
                Delay = _delay;
                Repeat = _repeat;
                UseFrame = _useFrame;
                _complete2 = _complete;
                _completeParam = _param;
                IsLoop = Repeat <= 0;
                ExeCnt = 0;
                var addTime = _first >= 0 ? _first : Delay;
                ExeTime = UseFrame ? Ins.TotalFrame + addTime : Ins.TotalTime + addTime;
            }

            public int Compare(IHandle compare)
            {
                if (compare is TimeHandle handle)
                {
                    return ExeTime.CompareTo(handle.ExeTime);
                }

                return 0;
            }

            public RunStatus Run()
            {
                if (Status != Status.Normal)
                {
                    return RunStatus.None;
                }

                float total = UseFrame ? UnityEngine.Time.frameCount : UnityEngine.Time.unscaledTime;
                if (total < ExeTime) return RunStatus.Wait;
                ExeTime += Delay;
                ExeCnt++;
                exe?.Run();
                if (Status != Status.Normal) //以防Run业务逻辑给Release掉了
                {
                    return RunStatus.None;
                }

                if (IsLoop) return RunStatus.Update;
                Repeat--;
                if (Repeat > 0) return RunStatus.Update;
                _complete1?.Invoke();
                _complete2?.Invoke(_completeParam);
                return RunStatus.Done;
            }

            public void Release()
            {
                if (Status != Status.WaitDel)
                {
                    Log.Error("出现非WaitDel状态却执行删除的TimeHandler {0}", MethodName);
                    return;
                }

                Status = Status.Release;
                Key = 0;
                _complete1 = null;
                exe?.Release();
                exe = null;
                ExeCnt = 0;
                Pool.Push(this);
            }
        }

        #endregion
    }
}