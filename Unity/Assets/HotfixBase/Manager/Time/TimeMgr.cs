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

        readonly float _updateGap = 1f;
        float _nextUpdateTime;
        protected override void OnCreated()
        {
            LocalTime = new LocalTime();
            ServerTime = new ServerTime();
            GameMethod.Update += _Update;
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

        void _Update()
        {
            _timer?.Run();
            _frame?.Run();
            if (TotalTime >= _nextUpdateTime)
            {
                _nextUpdateTime = TotalTime + _updateGap;
                _timeStamp?.Run();
                _cron?.Run();
            }
        }


        #region Common

        T CreateHandle<T>(out long key, HandleMap dic, Delegate action, object tag) where T : IHandle
        {
            if (action == null)
            {
                key = 0;
                return default;
            }

            // 分配全局唯一自增 ID
            key = IDGenerater.GenerateId();

            var handle = Pool.Get<T>();
            return (T)handle;
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
            if (!CheckCreate(builder.Fn, builder.Delay)) return 0;
            var dic = builder.IsFrame ? _frame : _timer;
            var handle = CreateHandle<TimeHandle>(out var key, dic, builder.Fn, builder.Tag);
            if (handle == null) return key;
            var exe = Pool.Get<HandleExe>();
            exe.Init(builder.Tag, builder.Fn);

            handle.Init(exe, key, builder.First, builder.Delay, builder.Count, builder.IsFrame, builder.CompleteWithParam, builder.CompleteParam);
            dic.Add(handle);
            return key;
        }
        private long CreateTimer<A>(TimerBuilder<A> builder)
        {
            if (!CheckCreate(builder.Fn, builder.Delay)) return 0;
            var dic = builder.IsFrame ? _frame : _timer;
            var handle = CreateHandle<TimeHandle>(out var key, dic, builder.Fn, builder.Tag);
            if (handle == null) return key;
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(builder.Tag, builder.Fn, builder.Param);
            handle.Init(exe, key, builder.First, builder.Delay, builder.Count, builder.IsFrame, builder.CompleteWithParam, builder.CompleteParam);
            dic.Add(handle);
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

        public void RemoveTimer(long key)
        {
            _timer.Remove(key);
        }
        public void RemoveFrame(long key)
        {
            _frame.Remove(key);
        }


        #endregion

        #region TimeStamp

        private long CreateTimeStamp(TimeStampBuilder builder)
        {
            if (builder.Fn == null) return 0;
            if ((builder.IsLocalTime ? LocalTime : ServerTime).TimeStamp > builder.TimeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, builder.Fn, builder.Tag);
            if (handle == null) return key;
            var exe = Pool.Get<HandleExe>();
            exe.Init(builder.Tag, builder.Fn);
            long triggerKey = 0;
            if (builder.TriggerLoopGap > 0 && builder.TriggerFn != null)
            {
                triggerKey = Timer(builder.TriggerLoopGap, builder.Tag, builder.TriggerFn).Loop().Build();
            }
            handle.Init(exe, key, builder.TimeStamp, builder.IsLocalTime, triggerKey);
            _timeStamp.Add(handle);
            return key;
        }
        private long CreateTimeStamp<A>(TimeStampBuilder<A> builder)
        {
            if (builder.Fn == null) return 0;
            if ((builder.IsLocalTime ? LocalTime : ServerTime).TimeStamp > builder.TimeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, builder.Fn, builder.Tag);
            if (handle == null) return key;
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(builder.Tag, builder.Fn, builder.Param);
            long triggerKey = 0;
            if (builder.TriggerLoopGap > 0 && builder.TriggerFn != null)
            {
                triggerKey = Timer(builder.TriggerLoopGap, builder.Tag, builder.TriggerFn).Loop().Build();
            }
            handle.Init(exe, key, builder.TimeStamp, builder.IsLocalTime, triggerKey);
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
        public void RemoveTimeStamp(long key)
        {
            _timeStamp.Remove(key);
        }



        #endregion TimeStamp

        #region Cron表达式                
        private long CreateCron(CronBuilder builder)
        {
            if (builder.Fn == null) return 0;
            var handle = CreateHandle<CronHandle>(out var key, _cron, builder.Fn, builder.Tag);
            if (handle == null) return key;
            var exe = Pool.Get<HandleExe>();
            exe.Init(builder.Tag, builder.Fn);
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
            if (builder.Fn == null) return 0;
            var handle = CreateHandle<CronHandle>(out var key, _cron, builder.Fn, builder.Tag);
            if (handle == null) return key;
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(builder.Tag, builder.Fn, builder.Param);
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
        public void RemoveCron(long key)
        {
            _cron.Remove(key);
        }
        #endregion
    }
}