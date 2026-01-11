using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ux
{
    public partial class TimeMgr : Singleton<TimeMgr>
    {
        public float TotalTime => Time.unscaledTime;
        public int TotalFrame => Time.frameCount;

        public IGameTime ServerTime { get; private set; }
        public IGameTime LocalTime { get; private set; }

        private readonly HandleMap _timer = new HandleMap(TimeType.Time);
        private readonly HandleMap _frame = new HandleMap(TimeType.Frame);
        private readonly HandleMap _timeStamp = new HandleMap(TimeType.TimeStamp);
        private readonly HandleMap _cron = new HandleMap(TimeType.Cron);

        readonly float _updateGap = 0.1f;
        float _nextUpdateTime;
        protected override void OnCreated()
        {
            LocalTime = new LocalTime();
            ServerTime = new ServerTime();
            GameMethod.Update += _Update;
            GameMethod.FixedUpdate += _FixedUpdate;
            GameMethod.LowMemory += _OnLowMemory;
        }
        public void Release()
        {
            _timer.Clear();
            _frame.Clear();
            _timeStamp.Clear();
            _cron.Clear();
            _OnLowMemory();
        }
        void _OnLowMemory()
        {
            CronData.Release();
        }
        public void __SetServerTime(long timeStamp)
        {
            var offset = timeStamp - LocalTime.TimeStamp;
            (ServerTime as ServerTime)?.SetOffset(offset);
        }


        void _FixedUpdate()
        {
            _frame?.Run();
        }

        void _Update()
        {
            if (TotalTime >= _nextUpdateTime)
            {
                _nextUpdateTime = TotalTime + _updateGap;
                _timer?.Run();
                _timeStamp?.Run();
                _cron?.Run();
            }
        }


        #region Common

        T CreateHandle<T>(out long key, HandleMap dic, Delegate action, object tag) where T : IHandle
        {
            key = GetKey(action, tag, dic);
            var handle = dic.Get(key);
            if (handle != null)
            {
                Log.Warning($"Time重复注册,新回调:{action.MethodName()}->将覆盖老回调:{handle.MethodName}");
                return (T)handle;
            }

            handle = Pool.Get<T>();
            return (T)handle;
        }

        private long GetKey(Delegate action, object tag, HandleMap dic)
        {
            if (action == null) return 0;
            if (tag == null)
            {
                return IDGenerater.GenerateId(RuntimeHelpers.GetHashCode(action), dic.GetHashCode());
            }
            else
            {
                return IDGenerater.GenerateId(action.GetHashCode(), dic.GetHashCode(), tag.GetHashCode());
            }
        }

        public void RemoveKey(long key)
        {
            _timer.Remove(key);
            _frame.Remove(key);
            _timeStamp.Remove(key);
            _cron.Remove(key);
        }

        public void RemoveTag(object tag)
        {
            _timer.RemoveAll(tag);
            _frame.RemoveAll(tag);
            _timeStamp.RemoveAll(tag);
            _cron.RemoveAll(tag);
        }

        #endregion

        #region TimeOrFrame

        bool CheckCreate(Delegate action, float delay)
        {
            if (delay <= 0 || action == null)
            {
                Log.Debug("延时参数不能小于或等于零");
                return false;
            }

            return true;
        }

        /// <summary>时间定时器内部创建方法</summary>
        private long CreateTimer(TimerBuilder builder)
        {
            if (!CheckCreate(builder.Func, builder.Delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, _timer, builder.Func, builder.Tag);
            var exe = Pool.Get<HandleExe>();
            exe.Init(builder.Tag, builder.Func);

            handle.Init(exe, key, builder.FirstDelay, builder.Delay, builder.Repeat, builder.IsFrame, builder.CompleteWithParam, builder.CompleteParam);
            _timer.Add(handle);
            return key;
        }
        private long CreateTimer<A>(TimerBuilder<A> builder)
        {
            if (!CheckCreate(builder.Func, builder.Delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, _timer, builder.Func, builder.Tag);
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(builder.Tag, builder.Func, builder.Param);
            handle.Init(exe, key, builder.FirstDelay, builder.Delay, builder.Repeat, builder.IsFrame, builder.CompleteWithParam, builder.CompleteParam);
            _timer.Add(handle);
            return key;
        }

        public TimerBuilder Timer(float seconds, object tag, Action action)
        {
            var builder = Pool.Get<TimerBuilder>();
            if (builder is ITimerBuilder initBuilder)
            {
                initBuilder.Init(seconds, false, CreateTimer);
                initBuilder.Do(tag, action);
            }
            return builder;
        }
        public TimerBuilder<A> Timer<A>(float seconds, object tag, Action<A> action, A param)
        {
            var builder = Pool.Get<TimerBuilder<A>>();
            if (builder is ITimerBuilder<A> initBuilder)
            {
                initBuilder.Init(seconds, false, CreateTimer);
                initBuilder.Do(tag, action, param);
            }
            return builder;
        }
        public TimerBuilder Frame(float frame, object tag, Action action)
        {
            var builder = Pool.Get<TimerBuilder>();
            if (builder is ITimerBuilder initBuilder)
            {
                initBuilder.Init(frame, true, CreateTimer);
                initBuilder.Do(tag, action);
            }
            return builder;
        }
        public TimerBuilder<A> Frame<A>(float frame, object tag, Action<A> action, A param)
        {
            var builder = Pool.Get<TimerBuilder<A>>();
            if (builder is ITimerBuilder<A> initBuilder)
            {
                initBuilder.Init(frame, true, CreateTimer);
                initBuilder.Do(tag, action, param);
            }
            return builder;
        }

        public void RemoveTimer(object tag, Action action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _timer);
            _timer.Remove(key);
        }

        public void RemoveTimer<A>(object tag, Action<A> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _timer);
            _timer.Remove(key);
        }

        public void RemoveFrame(object tag, Action action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _frame);
            _frame.Remove(key);
        }

        public void RemoveFrame<A>(object tag, Action<A> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _frame);
            _frame.Remove(key);
        }


        #endregion

        #region TimeStamp

        private long CreateTimeStamp(TimeStampBuilder builder)
        {
            if (builder.Func == null) return 0;
            if ((builder.IsLocalTime ? LocalTime : ServerTime).TimeStamp > builder.TimeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, builder.Func, builder.Tag);
            var exe = Pool.Get<HandleExe>();
            exe.Init(builder.Tag, builder.Func);
            handle.Init(exe, key, builder.TimeStamp, builder.IsLocalTime);
            _timeStamp.Add(handle);
            return key;
        }
        private long CreateTimeStamp<A>(TimeStampBuilder<A> builder)
        {
            if (builder.Func == null) return 0;
            if ((builder.IsLocalTime ? LocalTime : ServerTime).TimeStamp > builder.TimeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, builder.Func, builder.Tag);
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(builder.Tag, builder.Func, builder.Param);
            handle.Init(exe, key, builder.TimeStamp, builder.IsLocalTime);
            _timeStamp.Add(handle);
            return key;
        }

        public TimeStampBuilder TimeStamp(DateTime dt, object tag, Action action)
        {
            var builder = Pool.Get<TimeStampBuilder>();
            if (builder is ITimeStampBuilder initBuilder)
            {
                initBuilder.Init(dt.ToTimeStamp(), CreateTimeStamp);
                initBuilder.Do(tag, action);
            }
            return builder;
        }
        public TimeStampBuilder<A> TimeStamp<A>(DateTime dt, object tag, Action<A> action, A param)
        {
            var builder = Pool.Get<TimeStampBuilder<A>>();
            if (builder is ITimeStampBuilder<A> initBuilder)
            {
                initBuilder.Init(dt.ToTimeStamp(), CreateTimeStamp);
                initBuilder.Do(tag, action, param);
            }
            return builder;
        }
        public void RemoveTimeStamp(object tag, Action action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _timeStamp);
            _timeStamp.Remove(key);
        }

        public void RemoveTimeStamp<A>(object tag, Action<A> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _timeStamp);
            _timeStamp.Remove(key);
        }


        #endregion TimeStamp

        #region Cron表达式                
        private long CreateCron(CronBuilder builder)
        {
            if (builder.Func == null) return 0;
            var handle = CreateHandle<CronHandle>(out var key, _cron, builder.Func, builder.Tag);
            var exe = Pool.Get<HandleExe>();
            exe.Init(builder.Tag, builder.Func);
            if (!handle.Init(exe, key, builder.CronExpression, builder.IsLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
        }
        private long CreateCron<A>(CronBuilder<A> builder)
        {
            if (builder.Func == null) return 0;
            var handle = CreateHandle<CronHandle>(out var key, _cron, builder.Func, builder.Tag);
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(builder.Tag, builder.Func, builder.Param);
            if (!handle.Init(exe, key, builder.CronExpression, builder.IsLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
        }

        public CronBuilder Cron(string cron, object tag, Action action)
        {
            var builder = Pool.Get<CronBuilder>();
            if (builder is ICronBuilder initBuilder)
            {
                initBuilder.Init(cron, CreateCron);
                initBuilder.Do(tag, action);
            }
            return builder;
        }
        public CronBuilder<A> Cron<A>(string cron, object tag, Action<A> action, A param)
        {
            var builder = Pool.Get<CronBuilder<A>>();
            if (builder is ICronBuilder<A> initBuilder)
            {
                initBuilder.Init(cron, CreateCron);
                initBuilder.Do(tag, action, param);
            }
            return builder;
        }

        public void RemoveCron(object tag, Action action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _cron);
            _cron.Remove(key);
        }

        public void RemoveCron<A>(object tag, Action<A> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _cron);
            _cron.Remove(key);
        }
        #endregion
    }
}