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
            long key = 0;
            var target = tag;

            if (target == null)
            {
                key = (long)IDGenerater.GenerateId(RuntimeHelpers.GetHashCode(action), dic.GetHashCode());
            }
            else
            {
                key = (long)IDGenerater.GenerateId(action.GetHashCode(), dic.GetHashCode(), target.GetHashCode());
            }
            return key;
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

        private long Create(bool useFrame, HandleMap dic, float first, float delay, int repeat, object tag, Action action,
            Action complete)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action, tag);
            var exe = Pool.Get<HandleExe>();
            exe.Init(tag, action);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete);
            dic.Add(handle);
            return key;
        }

        private long Create(bool useFrame, HandleMap dic, float first, float delay, int repeat, object tag, Action action,
            Action<object> complete, object completeParam)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action, tag);
            var exe = Pool.Get<HandleExe>();
            exe.Init(tag, action);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete, completeParam);
            dic.Add(handle);
            return key;
        }

        private long Create<A>(bool useFrame, HandleMap dic, float first, float delay, int repeat, object tag,
            Action<A> action, A a, Action complete)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action, tag);
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(tag, action, a);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete);
            dic.Add(handle);
            return key;
        }

        private long Create<A>(bool useFrame, HandleMap dic, float first, float delay, int repeat, object tag,
            Action<A> action, A a, Action<object> complete, object completeParam)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action, tag);
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(tag, action, a);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete, completeParam);
            dic.Add(handle);
            return key;
        }

        private long Create<A, B>(bool useFrame, HandleMap dic, float first, float delay, int repeat, object tag,
            Action<A, B> action, A a, B b, Action complete)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action, tag);
            var exe = Pool.Get<HandleExe<A, B>>();
            exe.Init(tag, action, a, b);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete);
            dic.Add(handle);
            return key;
        }

        private long Create<A, B>(bool useFrame, HandleMap dic, float first, float delay, int repeat, object tag,
            Action<A, B> action, A a, B b, Action<object> complete, object completeParam)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action, tag);
            var exe = Pool.Get<HandleExe<A, B>>();
            exe.Init(tag, action, a, b);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete, completeParam);
            dic.Add(handle);
            return key;
        }

        private long Create<A, B, C>(bool useFrame, HandleMap dic, float first, float delay, int repeat, object tag,
            Action<A, B, C> action, A a, B b, C c, Action complete)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action, tag);
            var exe = Pool.Get<HandleExe<A, B, C>>();
            exe.Init(tag, action, a, b, c);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete);
            dic.Add(handle);
            return key;
        }

        private long Create<A, B, C>(bool useFrame, HandleMap dic, float first, float delay, int repeat, object tag,
            Action<A, B, C> action, A a, B b, C c, Action<object> complete, object completeParam)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action, tag);
            var exe = Pool.Get<HandleExe<A, B, C>>();
            exe.Init(tag, action, a, b, c);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete, completeParam);
            dic.Add(handle);
            return key;
        }


        #region Time

        /// <summary>
        /// 循环回调
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>
        /// <returns></returns>
        public long DoLoop(float delay, object tag, Action action)
        {
            return DoTimer(delay, 0, tag, action);
        }

        /// <summary>
        /// 循环回调
        /// </summary>
        /// <param name="first">第一次触发秒数</param>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>        
        /// <returns></returns>
        public long DoLoop(float first, float delay, object tag, Action action)
        {
            return DoTimer(first, delay, 0, tag, action);
        }

        /// <summary>
        /// 单次回调
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>
        /// <returns></returns>
        public long DoOnce(float delay, object tag, Action action)
        {
            return DoTimer(delay, 1, tag, action);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoTimer(float delay, int repeat, object tag, Action action,
            Action complete = null)
        {
            return Create(false, _timer, -1, delay, repeat, tag, action, complete);
        }

        public long DoTimer(float firstTime, float delay, int repeat, object tag, Action action,
            Action complete = null)
        {
            return Create(false, _timer, firstTime, delay, repeat, tag, action, complete);
        }

        public long DoTimer(float delay, int repeat, object tag, Action action,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, -1, delay, repeat, tag, action, complete, completeParam);
        }

        public long DoTimer(float firstTime, float delay, int repeat, object tag, Action action,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, firstTime, delay, repeat, tag, action, complete, completeParam);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoTimer<A>(float delay, int repeat, object tag, Action<A> action, A a,
            Action complete = null)
        {
            return Create(false, _timer, -1, delay, repeat, tag, action, a, complete);
        }

        public long DoTimer<A>(float firstTime, float delay, int repeat, object tag, Action<A> action, A a,
            Action complete = null)
        {
            return Create(false, _timer, delay, firstTime, repeat, tag, action, a, complete);
        }

        public long DoTimer<A>(float delay, int repeat, object tag, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, -1, delay, repeat, tag, action, a, complete, completeParam);
        }

        public long DoTimer<A>(float firstTime, float delay, int repeat, object tag, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, delay, firstTime, repeat, tag, action, a, complete, completeParam);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoTimer<A, B>(float delay, int repeat, object tag, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return Create(false, _timer, -1, delay, repeat, tag, action, a, b, complete);
        }

        public long DoTimer<A, B>(float firstTime, float delay, int repeat, object tag, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return Create(false, _timer, firstTime, delay, repeat, tag, action, a, b, complete);
        }

        public long DoTimer<A, B>(float delay, int repeat, object tag, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, -1, delay, repeat, tag, action, a, b, complete, completeParam);
        }

        public long DoTimer<A, B>(float firstTime, float delay, int repeat, object tag, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, firstTime, delay, repeat, tag, action, a, b, complete, completeParam);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="c">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoTimer<A, B, C>(float delay, int repeat, object tag, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return Create(false, _timer, -1, delay, repeat, tag, action, a, b, c, complete);
        }

        public long DoTimer<A, B, C>(float firstTime, float delay, int repeat, object tag, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return Create(false, _timer, firstTime, delay, repeat, tag, action, a, b, c, complete);
        }

        public long DoTimer<A, B, C>(float delay, int repeat, object tag, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, -1, delay, repeat, tag, action, a, b, c, complete, completeParam);
        }

        public long DoTimer<A, B, C>(float firstTime, float delay, int repeat, object tag, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, firstTime, delay, repeat, tag, action, a, b, c, complete, completeParam);
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

        public void RemoveTimer<A, B>(object tag, Action<A, B> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _timer);
            _timer.Remove(key);
        }

        public void RemoveTimer<A, B, C>(object tag, Action<A, B, C> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _timer);
            _timer.Remove(key);
        }

        #endregion Time

        #region Frame

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoFrame(int delay, int repeat, object tag, Action action,
            Action complete = null)
        {
            return Create(true, _frame, -1, delay, repeat, tag, action, complete);
        }

        public long DoFrame(int first, int delay, int repeat, object tag, Action action,
            Action complete = null)
        {
            return Create(true, _frame, first, delay, repeat, tag, action, complete);
        }

        public long DoFrame(int delay, int repeat, object tag, Action action,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, -1, delay, repeat, tag, action, complete, completeParam);
        }

        public long DoFrame(int first, int delay, int repeat, object tag, Action action,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, first, delay, repeat, tag, action, complete, completeParam);
        }

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoFrame<A>(int delay, int repeat, object tag, Action<A> action, A a,
            Action complete = null)
        {
            return Create(true, _frame, -1, delay, repeat, tag, action, a, complete);
        }

        public long DoFrame<A>(int first, int delay, int repeat, object tag, Action<A> action, A a,
            Action complete = null)
        {
            return Create(true, _frame, first, delay, repeat, tag, action, a, complete);
        }

        public long DoFrame<A>(int delay, int repeat, object tag, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, -1, delay, repeat, tag, action, a, complete, completeParam);
        }

        public long DoFrame<A>(int first, int delay, int repeat, object tag, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, first, delay, repeat, tag, action, a, complete, completeParam);
        }

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>        
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoFrame<A, B>(int delay, int repeat, object tag, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return Create(true, _frame, -1, delay, repeat, tag, action, a, b, complete);
        }

        public long DoFrame<A, B>(int first, int delay, int repeat, object tag, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return Create(true, _frame, first, delay, repeat, tag, action, a, b, complete);
        }

        public long DoFrame<A, B>(int delay, int repeat, object tag, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, -1, delay, repeat, tag, action, a, b, complete, completeParam);
        }

        public long DoFrame<A, B>(int first, int delay, int repeat, object tag, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, first, delay, repeat, tag, action, a, b, complete, completeParam);
        }

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>        
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="c">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoFrame<A, B, C>(int delay, int repeat, object tag, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return Create(true, _frame, -1, delay, repeat, tag, action, a, b, c, complete);
        }

        public long DoFrame<A, B, C>(int first, int delay, int repeat, object tag, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return Create(true, _frame, first, delay, repeat, tag, action, a, b, c, complete);
        }

        public long DoFrame<A, B, C>(int delay, int repeat, object tag, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, -1, delay, repeat, tag, action, a, b, c, complete, completeParam);
        }

        public long DoFrame<A, B, C>(int first, int delay, int repeat, object tag, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, first, delay, repeat, tag, action, a, b, c, complete, completeParam);
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

        public void RemoveFrame<A, B>(object tag, Action<A, B> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _frame);
            _frame.Remove(key);
        }

        public void RemoveFrame<A, B, C>(object tag, Action<A, B, C> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _frame);
            _frame.Remove(key);
        }

        #endregion

        #endregion

        #region TimeStamp

        public long DoTimeStamp(DateTime dt, object tag, Action action, bool isLocalTime = false)
        {
            return DoTimeStamp(dt.ToTimeStamp(), tag, action, isLocalTime);
        }

        public long DoTimeStamp(long timeStamp, object tag, Action action, bool isLocalTime = false)
        {
            if (action == null) return 0;
            if ((isLocalTime ? LocalTime : ServerTime).TimeStamp > timeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, action, tag);
            var exe = Pool.Get<HandleExe>();
            exe.Init(tag, action);
            handle.Init(exe, key, timeStamp, isLocalTime);
            _timeStamp.Add(handle);
            return key;
        }

        public long DoTimeStamp<A>(DateTime dt, object tag, Action<A> action, A a, bool isLocalTime = false)
        {
            return DoTimeStamp(dt.ToTimeStamp(), tag, action, a);
        }

        public long DoTimeStamp<A>(long timeStamp, object tag, Action<A> action, A a, bool isLocalTime = false)
        {
            if (action == null) return 0;
            if ((isLocalTime ? LocalTime : ServerTime).TimeStamp > timeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, action, tag);
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(tag, action, a);
            handle.Init(exe, key, timeStamp, isLocalTime);
            _timeStamp.Add(handle);
            return key;
        }

        public long DoTimeStamp<A, B>(DateTime dt, object tag, Action<A, B> action, A a, B b, bool isLocalTime = false)
        {
            return DoTimeStamp(dt.ToTimeStamp(), tag, action, a, b, isLocalTime);
        }

        public long DoTimeStamp<A, B>(long timeStamp, object tag, Action<A, B> action, A a, B b, bool isLocalTime = false)
        {
            if (action == null) return 0;
            if ((isLocalTime ? LocalTime : ServerTime).TimeStamp > timeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, action, tag);
            var exe = Pool.Get<HandleExe<A, B>>();
            exe.Init(tag, action, a, b);
            handle.Init(exe, key, timeStamp, isLocalTime);
            _timeStamp.Add(handle);
            return key;
        }

        public long DoTimeStamp<A, B, C>(DateTime dt, object tag, Action<A, B, C> action, A a, B b, C c, bool isLocalTime = false)
        {
            return DoTimeStamp(dt.ToTimeStamp(), tag, action, a, b, c, isLocalTime);
        }

        public long DoTimeStamp<A, B, C>(long timeStamp, object tag, Action<A, B, C> action, A a, B b, C c,
            bool isLocalTime = false)
        {
            if (action == null) return 0;
            if ((isLocalTime ? LocalTime : ServerTime).TimeStamp > timeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, action, tag);
            var exe = Pool.Get<HandleExe<A, B, C>>();
            exe.Init(tag, action, a, b, c);
            handle.Init(exe, key, timeStamp, isLocalTime);
            _timeStamp.Add(handle);
            return key;
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

        public void RemoveTimeStamp<A, B>(object tag, Action<A, B> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _timeStamp);
            _timeStamp.Remove(key);
        }

        public void RemoveTimeStamp<A, B, C>(object tag, Action<A, B, C> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _timeStamp);
            _timeStamp.Remove(key);
        }

        #endregion TimeStamp

        #region Cron表达式                

        public long DoCron(string cron, object tag, Action action, bool isLocalTime = false)
        {
            if (action == null) return 0;            
            var handle = CreateHandle<CronHandle>(out var key, _cron, action, tag);
            var exe = Pool.Get<HandleExe>();
            exe.Init(tag, action);
            if (!handle.Init(exe, key, cron, isLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
        }

        public long DoCron<A>(string cron, object tag, Action<A> action, A a, bool isLocalTime = false)
        {
            if (action == null) return 0;            
            var handle = CreateHandle<CronHandle>(out var key, _cron, action, tag);
            var exe = Pool.Get<HandleExe<A>>();
            exe.Init(tag, action, a);
            if (!handle.Init(exe, key, cron, isLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
        }

        public long DoCron<A, B>(string cron, object tag, Action<A, B> action, A a, B b, bool isLocalTime = false)
        {
            if (action == null) return 0;            
            var handle = CreateHandle<CronHandle>(out var key, _cron, action, tag);
            var exe = Pool.Get<HandleExe<A, B>>();
            exe.Init(tag, action, a, b);
            if (!handle.Init(exe, key, cron, isLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
        }

        public long DoCron<A, B, C>(string cron, object tag, Action<A, B, C> action, A a, B b, C c, bool isLocalTime = false)
        {
            if (action == null) return 0;            
            var handle = CreateHandle<CronHandle>(out var key, _cron, action, tag);
            var exe = Pool.Get<HandleExe<A, B, C>>();
            exe.Init(tag, action, a, b, c);
            if (!handle.Init(exe, key, cron, isLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
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

        public void RemoveCron<A, B>(object tag, Action<A, B> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _cron);
            _cron.Remove(key);
        }

        public void RemoveCron<A, B, C>(object tag, Action<A, B, C> action)
        {
            if (action == null) return;
            var key = GetKey(action, tag, _cron);
            _cron.Remove(key);
        }

        #endregion
    }
}