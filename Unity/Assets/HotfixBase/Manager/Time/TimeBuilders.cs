using System;
namespace Ux
{
    #region TimerBuilder

    public interface ITimerBuildBase
    {
        float Delay { get; }
        int Repeat { get; }
        float FirstDelay { get; }
        object Tag { get; }
        Action Complete { get; }
        Action<object> CompleteWithParam { get; }
        object CompleteParam { get; }
        bool IsFrame { get; }
    }
    public interface ITimerBuilder : ITimerBuildBase
    {
        void Init(float delay, bool isFrame, Func<TimerBuilder, long> create);
        void Do(object tag, Action action);
    }
    public interface ITimerBuilder<A> : ITimerBuildBase
    {
        void Init(float delay, bool isFrame, Func<TimerBuilder<A>, long> action);
        void Do(object tag, Action<A> action, A param);
    }

    public abstract class TimerBuildBase<T> : ITimerBuildBase where T : TimerBuildBase<T>
    {
        public float Delay { get; protected set; }
        public int Repeat { get; private set; } = 1;
        public float FirstDelay { get; private set; } = -1;
        public object Tag { get; protected set; }
        public Action Complete { get; private set; }
        public Action<object> CompleteWithParam { get; private set; }
        public object CompleteParam { get; private set; }
        public bool IsFrame { get; protected set; }

        public T Loop()
        {
            return Repeat(0);
        }
        /// <summary>设置重复次数（小于等于0则循环）</summary>
        public T Repeat(int count)
        {
            Repeat = count;
            return (T)this;
        }
        /// <summary>设置首次触发延时</summary>
        public T FirstDelay(float seconds)
        {
            FirstDelay = seconds;
            return (T)this;
        }

        /// <summary>设置完成回调</summary>
        public T OnComplete(Action complete)
        {
            Complete = complete;
            return (T)this;
        }

        /// <summary>设置带参数的完成回调</summary>
        public T OnComplete(Action<object> complete, object param)
        {
            CompleteWithParam = complete;
            CompleteParam = param;
            return (T)this;
        }

        protected void Release()
        {
            Delay = 0;
            Repeat = 1;
            FirstDelay = -1;
            Tag = null;
            Complete = null;
            CompleteWithParam = null;
            CompleteParam = null;
            OnRelease();
        }
        protected abstract void OnRelease();

    }

    public class TimerBuilder : TimerBuildBase<TimerBuilder>, ITimerBuilder
    {
        Func<TimerBuilder, long> _create;
        public Action Func { get; private set; } = null;


        void ITimerBuilder.Init(float delay, bool isFrame, Func<TimerBuilder, long> create)
        {
            Delay = delay;
            IsFrame = isFrame;
            _create = create;
        }
        protected override void OnRelease()
        {
            _create = null;
            Func = null;
        }

        /// <summary>设置执行动作,设置标签（用于批量删除）</summary>        
        void ITimerBuilder.Do(object tag, Action action)
        {
            Tag = tag;
            Func = action;
        }
        /// <summary>构建并启动定时器</summary>
        public long Build()
        {
            var key = _create(this);
            Release();
            return key;
        }

        /// <summary>隐式转换为long定时器ID</summary>
        public static implicit operator long(TimerBuilder builder) => builder.Build();
    }

    public class TimerBuilder<A> : TimerBuildBase<TimerBuilder<A>>, ITimerBuilder<A>
    {
        Func<TimerBuilder<A>, long> _create;
        public Action<A> Func { get; private set; } = null;
        public A Param { get; private set; } = default;


        void ITimerBuilder<A>.Init(float delay, bool isFrame, Func<TimerBuilder<A>, long> action)
        {
            Delay = delay;
            IsFrame = isFrame;
            _create = action;
        }
        protected override void OnRelease()
        {
            _create = null;
            Func = null;
        }

        /// <summary>设置执行动作,设置标签（用于批量删除）</summary>        
        void ITimerBuilder<A>.Do(object tag, Action<A> action, A param)
        {
            Tag = tag;
            Func = action;
            Param = param;
        }
        /// <summary>构建并启动定时器</summary>
        public long Build()
        {
            var key = _create(this);
            Release();
            return key;
        }

        /// <summary>隐式转换为long定时器ID</summary>
        public static implicit operator long(TimerBuilder<A> builder) => builder.Build();
    }


    #endregion

    #region TimeStampBuilder
    public interface ITimeStampBuilderBase
    {
        long TimeStamp { get; }
        bool IsLocalTime { get; }
        object Tag { get; }
    }
    public interface ITimeStampBuilder : ITimeStampBuilderBase
    {
        void Init(long timeStamp, Func<TimeStampBuilder, long> create);
        void Do(object tag, Action action);        
    }
    public interface ITimeStampBuilder<A> : ITimeStampBuilderBase
    {
        void Init(long timeStamp, Func<TimeStampBuilder<A>, long> create);
        void Do(object tag, Action<A> action, A param);
    }

    public abstract class TimeStampBuilderBase<T> : ITimeStampBuilderBase where T : TimeStampBuilderBase<T>
    {
        public long TimeStamp { get; protected set; }
        public object Tag { get; protected set; }
        public bool IsLocalTime { get; private set; }
        /// <summary>设置使用本地时间（默认使用服务器时间）</summary>
        public T UseLocalTime(bool isLocal = true)
        {
            IsLocalTime = isLocal;
            return (T)this;
        }
        protected void Release()
        {
            TimeStamp = 0;
            IsLocalTime = false;
            Tag = null;
            OnRelease();
        }
        protected abstract void OnRelease();
    }



    public class TimeStampBuilder : TimeStampBuilderBase<TimeStampBuilder>, ITimeStampBuilder
    {
        public Action Func { get; private set; } = null;
        Func<TimeStampBuilder, long> _create;
        void ITimeStampBuilder.Init(long timeStamp, Func<TimeStampBuilder, long> create)
        {
            TimeStamp = timeStamp;
            _create = create;
        }
        protected override void OnRelease()
        {
            _create = null;
            Func = null;
        }

        /// <summary>设置执行动作</summary>
        void ITimeStampBuilder.Do(object tag, Action action)
        {
            Tag = tag;
            Func = action;
        }

        /// <summary>构建并启动时间戳定时器</summary>
        public long Build()
        {
            var key = _create(this);
            Release();
            return key;
        }

        /// <summary>隐式转换为long定时器ID</summary>
        public static implicit operator long(TimeStampBuilder builder) => builder.Build();
    }

    public class TimeStampBuilder<A> : TimeStampBuilderBase<TimeStampBuilder<A>>, ITimeStampBuilder<A>
    {
        public Action<A> Func { get; private set; } = null;
        public A Param { get; private set; }
        Func<TimeStampBuilder<A>, long> _create;
        void ITimeStampBuilder<A>.Init(long timeStamp, Func<TimeStampBuilder<A>, long> create)
        {
            TimeStamp = timeStamp;
            _create = create;
        }
        protected override void OnRelease()
        {
            _create = null;
            Func = null;
        }

        /// <summary>设置执行动作</summary>
        void ITimeStampBuilder<A>.Do(object tag, Action<A> action, A param)
        {
            Tag = tag;
            Func = action;
            Param = param;
        }

        /// <summary>构建并启动时间戳定时器</summary>
        public long Build()
        {
            var key = _create(this);
            Release();
            return key;
        }

        /// <summary>隐式转换为long定时器ID</summary>
        public static implicit operator long(TimeStampBuilder<A> builder) => builder.Build();
    }

    #endregion

    #region CronBuilder
    public interface ICronBuilderBase
    {
        string CronExpression { get; }
        bool IsLocalTime { get; }
        object Tag { get; }
    }
    public interface ICronBuilder : ICronBuilderBase
    {
        void Init(string cronExpression, Func<CronBuilder, long> create);
        void Do(object tag, Action action);
    }
    public interface ICronBuilder<A> : ICronBuilderBase
    {
        void Init(string cronExpression, Func<CronBuilder<A>, long> create);
        void Do(object tag, Action<A> action, A param);
    }
    public abstract class CronBuilderBase<T> : ICronBuilderBase where T : CronBuilderBase<T>
    {
        public string CronExpression { get; protected set; }
        public object Tag { get; protected set; }
        public bool IsLocalTime { get; private set; }
        /// <summary>设置使用本地时间（默认使用服务器时间）</summary>
        public T UseLocalTime(bool isLocal = true)
        {
            IsLocalTime = isLocal;
            return (T)this;
        }
        protected void Release()
        {
            CronExpression = null;
            IsLocalTime = false;
            Tag = null;
            OnRelease();
        }
        protected abstract void OnRelease();
    }

    public class CronBuilder : CronBuilderBase<CronBuilder>, ICronBuilder
    {
        public Action Func { get; private set; } = null;
        Func<CronBuilder, long> _create;
        void ICronBuilder.Init(string cronExpression, Func<CronBuilder, long> create)
        {
            CronExpression = cronExpression;
            _create = create;
        }
        protected override void OnRelease()
        {
            _create = null;
            Func = null;
        }

        /// <summary>设置执行动作</summary>
        void ICronBuilder.Do(object tag, Action action)
        {
            Tag = tag;
            Func = action;
        }

        /// <summary>构建并启动Cron定时器</summary>
        public long Build()
        {
            var key = _create(this);
            Release();
            return key;
        }

        /// <summary>隐式转换为long定时器ID</summary>
        public static implicit operator long(CronBuilder builder) => builder.Build();
    }

    public class CronBuilder<A> : CronBuilderBase<CronBuilder<A>>, ICronBuilder<A>
    {
        public Action<A> Func { get; private set; } = null;
        public A Param { get; private set; }
        Func<CronBuilder<A>, long> _create;
        void ICronBuilder<A>.Init(string cronExpression, Func<CronBuilder<A>, long> create)
        {
            CronExpression = cronExpression;
            _create = create;
        }
        protected override void OnRelease()
        {
            _create = null;
            Func = null;
        }

        /// <summary>设置执行动作</summary>
        void ICronBuilder<A>.Do(object tag, Action<A> action, A param)
        {
            Tag = tag;
            Func = action;
            Param = param;
        }

        /// <summary>构建并启动Cron定时器</summary>
        public long Build()
        {
            var key = _create(this);
            Release();
            return key;
        }

        /// <summary>隐式转换为long定时器ID</summary>
        public static implicit operator long(CronBuilder<A> builder) => builder.Build();
    }

    #endregion
}
