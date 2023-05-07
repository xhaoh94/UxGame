using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public partial class TimeMgr : Singleton<TimeMgr>
    {
        public enum TimeType
        {
            Time,
            Frame,
            TimeStamp,
            Cron
        }

        public enum RunStatus
        {
            None,
            Error,
            Wait,
            Update,
            Done
        }

        public enum Status
        {
            Normal,
            WaitDel,
            Release
        }

        sealed class HandleMap
        {
            readonly TimeType _timeType;
            public HandleMap(TimeType timeType)
            {
                _timeType = timeType;
            }
#if UNITY_EDITOR
            public readonly Dictionary<string, TimeList> desc2editor = new Dictionary<string, TimeList>();
#endif
            private readonly List<IHandle> handles = new List<IHandle>();
            private readonly Dictionary<long, IHandle> handleDic = new Dictionary<long, IHandle>();
            private readonly Dictionary<int, List<long>> thix2keys = new Dictionary<int, List<long>>();

            readonly List<IHandle> waitAdds = new List<IHandle>();
            readonly List<IHandle> waitDels = new List<IHandle>();

            public void Clear()
            {
                handles.Clear();
                handleDic.Clear();
                thix2keys.Clear();
                waitAdds.Clear();
                waitDels.Clear();
#if UNITY_EDITOR
                desc2editor.Clear();
#endif
            }

#if UNITY_EDITOR
            void __Debugger_Event()
            {
                switch (_timeType)
                {
                    case TimeType.Time:
                        __Debugger_Time_Event();
                        break;
                    case TimeType.Frame:
                        __Debugger_Frame_Event();
                        break;
                    case TimeType.TimeStamp:
                        __Debugger_TimeStamp_Event();
                        break;
                    case TimeType.Cron:
                        __Debugger_Cron_Event();
                        break;
                }
            }
#endif

            private bool needSort;
#if UNITY_EDITOR
            bool __isEvent;
#endif
            public void Run()
            {
                while (waitDels.Count > 0)
                {
                    var handle = waitDels[0];
                    waitDels.RemoveAt(0);
                    var key = handle.Key;
                    var target = handle.Target;

#if UNITY_EDITOR
                    var exeDesc = handle.MethodName;
                    if (desc2editor.TryGetValue(exeDesc, out var d2eList))
                    {
                        if (d2eList.Remove(handle))
                        {
                            desc2editor.Remove(exeDesc);
                            __Debugger_Event();
                        }
                    }
#endif
                    handleDic.Remove(key);
                    if (target != null)
                    {
                        var hashCode = target.GetHashCode();
                        if (!thix2keys.TryGetValue(hashCode, out var keys)) continue;
                        keys.Remove(key);
                        if (keys.Count == 0) thix2keys.Remove(hashCode);
                    }

                    handle.Release();
                    handles.Remove(handle);
                }

                if (needSort)
                {
                    handles.Sort((a, b) => b.Compare(a));
                }

#if UNITY_EDITOR
                if (__isEvent)
                {
                    __Debugger_Event();
                }
#endif


                while (waitAdds.Count > 0)
                {
                    var handle = waitAdds[0];
                    waitAdds.RemoveAt(0);
                    Sort(handle);
                    handleDic.Add(handle.Key, handle);
                    var target = handle.Target;
#if UNITY_EDITOR
                    if (!desc2editor.TryGetValue(handle.MethodName, out var d2eList))
                    {
                        d2eList = new TimeList(handle.MethodName);
                        desc2editor.Add(handle.MethodName, d2eList);
                    }

                    d2eList.Add(handle);
                    __Debugger_Event();
#endif
                    if (target != null)
                    {
                        int hashCode = target.GetHashCode();
                        if (!thix2keys.TryGetValue(hashCode, out var keys))
                        {
                            keys = new List<long>();
                            thix2keys.Add(hashCode, keys);
                        }

                        keys.Add(handle.Key);
                    }
                }

                OnRun();
            }

            void OnRun()
            {
                needSort = false;
#if UNITY_EDITOR
                __isEvent = false;
#endif
                if (handles.Count <= 0) return;

                for (var i = handles.Count - 1; i >= 0; i--)
                {
                    var handler = handles[i];
                    switch (handler.Run())
                    {
                        case RunStatus.Wait:
                            return;
                        case RunStatus.Update:
#if UNITY_EDITOR
                            __isEvent = true;
#endif
                            needSort = true;
                            break;
                        case RunStatus.Done:
                            Remove(handler);
                            break;
                        case RunStatus.None:
#if UNITY_EDITOR
                            __isEvent = true;
#endif
                            break;
                    }
                }
            }

            private void Sort(IHandle handle)
            {
                var stratIndex = 0;
                var endIndex = handles.Count;
                int loopCnt = 0;
                while (true)
                {
                    if (endIndex - stratIndex <= 10)
                    {
                        int insertIndex = -1;
                        for (var i = stratIndex; i < endIndex; i++)
                        {
                            if (handle.Compare(handles[i]) >= 0)
                            {
                                insertIndex = i;
                                break;
                            }
                        }

                        if (insertIndex != -1)
                        {
                            handles.Insert(insertIndex, handle);
                        }
                        else
                        {
                            handles.Add(handle);
                        }

                        return;
                    }

                    var index = stratIndex + ((endIndex - stratIndex) >> 1);
                    if (handle.Compare(handles[index]) == 0)
                    {
                        handles.Insert(index, handle);
                        return;
                    }
                    else if (handle.Compare(handles[index]) > 0)
                    {
                        endIndex = index;
                    }
                    else
                    {
                        stratIndex = index;
                    }

                    loopCnt++;
                    if (loopCnt > 500)
                    {
                        Log.Warning("时间排序循环超出");
                        break;
                    }
                }
            }

            public void Add(IHandle handle)
            {
                if (waitAdds.Contains(handle)) return;
                handle.Status = Status.Normal;
                waitAdds.Add(handle);
            }

            public bool ContainsKey(long key)
            {
                var b = handleDic.TryGetValue(key, out var handle) && handle.Status == Status.Normal;
                if (b)
                {
                    Log.Error("TIME:重复注册 {0}", handle.MethodName);
                }

                return b;
            }

            public void RemoveAll(object thix)
            {
                if (thix == null) return;
                int hashCode = thix.GetHashCode();
                if (!thix2keys.TryGetValue(hashCode, out var keys)) return;
                foreach (var key in keys)
                {
                    Remove(key);
                }
            }

            public void Remove(long key)
            {
                if (!handleDic.TryGetValue(key, out var handle))
                {
                    if (waitAdds.Count > 0)
                    {
                        var index = waitAdds.FindIndex(x => x.Key == key);
                        waitAdds.RemoveAt(index);
                    }                  
                    return;
                }
                Remove(handle);
            }

            void Remove(IHandle handle)
            {
                if (waitDels.Contains(handle)) return;
                handle.Status = Status.WaitDel;
                waitDels.Add(handle);
            }
        }

        public float TotalTime => Time.unscaledTime;
        public float TotalFrame => Time.frameCount;

        public IGameTime ServerTime { get; private set; }
        public IGameTime LocalTime { get; private set; }

        private Action _fixedUpdate;
        private Action _update;
        private Action _lateUpdate;
        private readonly HandleMap _timer = new HandleMap(TimeType.Time);
        private readonly HandleMap _frame = new HandleMap(TimeType.Frame);
        private readonly HandleMap _timeStamp = new HandleMap(TimeType.TimeStamp);
        private readonly HandleMap _cron = new HandleMap(TimeType.Cron);

        public TimeMgr()
        {
            LocalTime = new LocalTime();
            ServerTime = new ServerTime();
        }

        public void __SetServerTime(long timeStamp)
        {
            var offset = timeStamp - LocalTime.TimeStamp;
            (ServerTime as ServerTime)?.SetOffset(offset);
        }

        #region 编辑器

#if UNITY_EDITOR
        public static void __Debugger_Time_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Time_CallBack?.Invoke(Instance._timer.desc2editor);
            }
        }

        public static void __Debugger_Frame_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Frame_CallBack?.Invoke(Instance._frame.desc2editor);
            }
        }

        public static void __Debugger_TimeStamp_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_TimeStamp_CallBack?.Invoke(Instance._timeStamp.desc2editor);
            }
        }

        public static void __Debugger_Cron_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_Cron_CallBack?.Invoke(Instance._cron.desc2editor);
            }
        }

        public static Action<Dictionary<string, TimeList>> __Debugger_Time_CallBack;
        public static Action<Dictionary<string, TimeList>> __Debugger_Frame_CallBack;
        public static Action<Dictionary<string, TimeList>> __Debugger_TimeStamp_CallBack;
        public static Action<Dictionary<string, TimeList>> __Debugger_Cron_CallBack;

#endif

        #endregion

        public void FixedUpdate()
        {
            _fixedUpdate?.Invoke();
            _timer?.Run();
            _timeStamp?.Run();
            _cron?.Run();
        }

        public void Update()
        {
            _update?.Invoke();
            _frame?.Run();
        }

        public void LateUpdate()
        {
            _lateUpdate?.Invoke();
        }

        public void DoFixedUpdate(Action action)
        {
            _fixedUpdate += action;
        }

        public void DoUpdate(Action action)
        {
            _update += action;
        }

        public void DoLateUpdate(Action action)
        {
            _lateUpdate += action;
        }

        public void RemoveFixedUpdate(Action action)
        {
            _fixedUpdate -= action;
        }

        public void RemoveUpdate(Action action)
        {
            _update -= action;
        }

        public void RemoveLateUpdate(Action action)
        {
            _lateUpdate -= action;
        }

        #region Common

        T CreateHandle<T>(out long key, HandleMap dic, Delegate action) where T : IHandle
        {
            key = GetKey(action, dic);
            if (dic.ContainsKey(key))
            {
                return default;
            }

            var handle = Pool.Get<T>();
            return handle;
        }

        private long GetKey(Delegate action, HandleMap dic)
        {
            if (action == null) return 0;
            long flag = (long)Math.Abs(dic.GetHashCode()) << 32;
            var key = (flag << 32) + Math.Abs(action.GetHashCode());
            var target = action.Target;
            if (target != null)
            {
                key |= (uint)Math.Abs(target.GetHashCode());
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

        public void RemoveAll(object thix)
        {
            _timer.RemoveAll(thix);
            _frame.RemoveAll(thix);
            _timeStamp.RemoveAll(thix);
            _cron.RemoveAll(thix);
        }

        public void Release()
        {
            _update = null;
            _lateUpdate = null;
            _timer.Clear();
            _frame.Clear();
            _timeStamp.Clear();
            _cron.Clear();
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

        private long Create(bool useFrame, HandleMap dic, float first, float delay, int repeat, Action action,
            Action complete)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe>();
            exe.SetExeFn(action);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete);
            dic.Add(handle);
            return key;
        }

        private long Create(bool useFrame, HandleMap dic, float first, float delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe>();
            exe.SetExeFn(action);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete, completeParam);
            dic.Add(handle);
            return key;
        }

        private long Create<A>(bool useFrame, HandleMap dic, float first, float delay, int repeat,
            Action<A> action, A a, Action complete)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A>>();
            exe.SetExeFn(action, a);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete);
            dic.Add(handle);
            return key;
        }

        private long Create<A>(bool useFrame, HandleMap dic, float first, float delay, int repeat,
            Action<A> action, A a, Action<object> complete, object completeParam)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A>>();
            exe.SetExeFn(action, a);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete, completeParam);
            dic.Add(handle);
            return key;
        }

        private long Create<A, B>(bool useFrame, HandleMap dic, float first, float delay, int repeat,
            Action<A, B> action, A a, B b, Action complete)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A, B>>();
            exe.SetExeFn(action, a, b);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete);
            dic.Add(handle);
            return key;
        }

        private long Create<A, B>(bool useFrame, HandleMap dic, float first, float delay, int repeat,
            Action<A, B> action, A a, B b, Action<object> complete, object completeParam)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A, B>>();
            exe.SetExeFn(action, a, b);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete, completeParam);
            dic.Add(handle);
            return key;
        }

        private long Create<A, B, C>(bool useFrame, HandleMap dic, float first, float delay, int repeat,
            Action<A, B, C> action, A a, B b, C c, Action complete)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A, B, C>>();
            exe.SetExeFn(action, a, b, c);
            handle.Init(exe, key, first, delay, repeat, useFrame, complete);
            dic.Add(handle);
            return key;
        }

        private long Create<A, B, C>(bool useFrame, HandleMap dic, float first, float delay, int repeat,
            Action<A, B, C> action, A a, B b, C c, Action<object> complete, object completeParam)
        {
            if (!CheckCreate(action, delay)) return 0;
            var handle = CreateHandle<TimeHandle>(out var key, dic, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A, B, C>>();
            exe.SetExeFn(action, a, b, c);
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
        public long DoLoop(float delay, Action action)
        {
            return DoTimer(delay, 0, action);
        }

        /// <summary>
        /// 循环回调
        /// </summary>
        /// <param name="first">第一次触发秒数</param>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>        
        /// <returns></returns>
        public long DoLoop(float first, float delay, Action action)
        {
            return DoTimer(first, delay, 0, action);
        }

        /// <summary>
        /// 单次回调
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>
        /// <returns></returns>
        public long DoOnce(float delay, Action action)
        {
            return DoTimer(delay, 1, action);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoTimer(float delay, int repeat, Action action,
            Action complete = null)
        {
            return Create(false, _timer, -1, delay, repeat, action, complete);
        }

        public long DoTimer(float firstTime, float delay, int repeat, Action action,
            Action complete = null)
        {
            return Create(false, _timer, firstTime, delay, repeat, action, complete);
        }

        public long DoTimer(float delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, -1, delay, repeat, action, complete, completeParam);
        }

        public long DoTimer(float firstTime, float delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, firstTime, delay, repeat, action, complete, completeParam);
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
        public long DoTimer<A>(float delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return Create(false, _timer, -1, delay, repeat, action, a, complete);
        }

        public long DoTimer<A>(float firstTime, float delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return Create(false, _timer, delay, firstTime, repeat, action, a, complete);
        }

        public long DoTimer<A>(float delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, -1, delay, repeat, action, a, complete, completeParam);
        }

        public long DoTimer<A>(float firstTime, float delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, delay, firstTime, repeat, action, a, complete, completeParam);
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
        public long DoTimer<A, B>(float delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return Create(false, _timer, -1, delay, repeat, action, a, b, complete);
        }

        public long DoTimer<A, B>(float firstTime, float delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return Create(false, _timer, firstTime, delay, repeat, action, a, b, complete);
        }

        public long DoTimer<A, B>(float delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, -1, delay, repeat, action, a, b, complete, completeParam);
        }

        public long DoTimer<A, B>(float firstTime, float delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, firstTime, delay, repeat, action, a, b, complete, completeParam);
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
        public long DoTimer<A, B, C>(float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return Create(false, _timer, -1, delay, repeat, action, a, b, c, complete);
        }

        public long DoTimer<A, B, C>(float firstTime, float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return Create(false, _timer, firstTime, delay, repeat, action, a, b, c, complete);
        }

        public long DoTimer<A, B, C>(float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, -1, delay, repeat, action, a, b, c, complete, completeParam);
        }

        public long DoTimer<A, B, C>(float firstTime, float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return Create(false, _timer, firstTime, delay, repeat, action, a, b, c, complete, completeParam);
        }

        public void RemoveTimer(Action action)
        {
            if (action == null) return;
            var key = GetKey(action, _timer);
            _timer.Remove(key);
        }

        public void RemoveTimer<A>(Action<A> action)
        {
            if (action == null) return;
            var key = GetKey(action, _timer);
            _timer.Remove(key);
        }

        public void RemoveTimer<A, B>(Action<A, B> action)
        {
            if (action == null) return;
            var key = GetKey(action, _timer);
            _timer.Remove(key);
        }

        public void RemoveTimer<A, B, C>(Action<A, B, C> action)
        {
            if (action == null) return;
            var key = GetKey(action, _timer);
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
        public long DoFrame(int delay, int repeat, Action action,
            Action complete = null)
        {
            return Create(true, _frame, -1, delay, repeat, action, complete);
        }

        public long DoFrame(int first, int delay, int repeat, Action action,
            Action complete = null)
        {
            return Create(true, _frame, first, delay, repeat, action, complete);
        }

        public long DoFrame(int delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, -1, delay, repeat, action, complete, completeParam);
        }

        public long DoFrame(int first, int delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, first, delay, repeat, action, complete, completeParam);
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
        public long DoFrame<A>(int delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return Create(true, _frame, -1, delay, repeat, action, a, complete);
        }

        public long DoFrame<A>(int first, int delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return Create(true, _frame, first, delay, repeat, action, a, complete);
        }

        public long DoFrame<A>(int delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, -1, delay, repeat, action, a, complete, completeParam);
        }

        public long DoFrame<A>(int first, int delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, first, delay, repeat, action, a, complete, completeParam);
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
        public long DoFrame<A, B>(int delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return Create(true, _frame, -1, delay, repeat, action, a, b, complete);
        }

        public long DoFrame<A, B>(int first, int delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return Create(true, _frame, first, delay, repeat, action, a, b, complete);
        }

        public long DoFrame<A, B>(int delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, -1, delay, repeat, action, a, b, complete, completeParam);
        }

        public long DoFrame<A, B>(int first, int delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, first, delay, repeat, action, a, b, complete, completeParam);
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
        public long DoFrame<A, B, C>(int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return Create(true, _frame, -1, delay, repeat, action, a, b, c, complete);
        }

        public long DoFrame<A, B, C>(int first, int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return Create(true, _frame, first, delay, repeat, action, a, b, c, complete);
        }

        public long DoFrame<A, B, C>(int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, -1, delay, repeat, action, a, b, c, complete, completeParam);
        }

        public long DoFrame<A, B, C>(int first, int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return Create(true, _frame, first, delay, repeat, action, a, b, c, complete, completeParam);
        }


        public void RemoveFrame(Action action)
        {
            if (action == null) return;
            var key = GetKey(action, _frame);
            _frame.Remove(key);
        }

        public void RemoveFrame<A>(Action<A> action)
        {
            if (action == null) return;
            var key = GetKey(action, _frame);
            _frame.Remove(key);
        }

        public void RemoveFrame<A, B>(Action<A, B> action)
        {
            if (action == null) return;
            var key = GetKey(action, _frame);
            _frame.Remove(key);
        }

        public void RemoveFrame<A, B, C>(Action<A, B, C> action)
        {
            if (action == null) return;
            var key = GetKey(action, _frame);
            _frame.Remove(key);
        }

        #endregion

        #endregion

        #region TimeStamp

        public long DoTimeStamp(DateTime dt, Action action, bool isLocalTime = false)
        {
            return DoTimeStamp(dt.ToTimeStamp(), action, isLocalTime);
        }

        public long DoTimeStamp(long timeStamp, Action action, bool isLocalTime = false)
        {
            if (action == null) return 0;
            if ((isLocalTime ? LocalTime : ServerTime).TimeStamp > timeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe>();
            exe.SetExeFn(action);
            handle.Init(exe, key, timeStamp, isLocalTime);
            _timeStamp.Add(handle);
            return key;
        }

        public long DoTimeStamp<A>(DateTime dt, Action<A> action, A a, bool isLocalTime = false)
        {
            return DoTimeStamp(dt.ToTimeStamp(), action, a);
        }

        public long DoTimeStamp<A>(long timeStamp, Action<A> action, A a, bool isLocalTime = false)
        {
            if (action == null) return 0;
            if ((isLocalTime ? LocalTime : ServerTime).TimeStamp > timeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A>>();
            exe.SetExeFn(action, a);
            handle.Init(exe, key, timeStamp, isLocalTime);
            _timeStamp.Add(handle);
            return key;
        }

        public long DoTimeStamp<A, B>(DateTime dt, Action<A, B> action, A a, B b, bool isLocalTime = false)
        {
            return DoTimeStamp(dt.ToTimeStamp(), action, a, b, isLocalTime);
        }

        public long DoTimeStamp<A, B>(long timeStamp, Action<A, B> action, A a, B b, bool isLocalTime = false)
        {
            if (action == null) return 0;
            if ((isLocalTime ? LocalTime : ServerTime).TimeStamp > timeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A, B>>();
            exe.SetExeFn(action, a, b);
            handle.Init(exe, key, timeStamp, isLocalTime);
            _timeStamp.Add(handle);
            return key;
        }

        public long DoTimeStamp<A, B, C>(DateTime dt, Action<A, B, C> action, A a, B b, C c, bool isLocalTime = false)
        {
            return DoTimeStamp(dt.ToTimeStamp(), action, a, b, c, isLocalTime);
        }

        public long DoTimeStamp<A, B, C>(long timeStamp, Action<A, B, C> action, A a, B b, C c,
            bool isLocalTime = false)
        {
            if (action == null) return 0;
            if ((isLocalTime ? LocalTime : ServerTime).TimeStamp > timeStamp) return 0;
            var handle = CreateHandle<TimeStampHandle>(out var key, _timeStamp, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A, B, C>>();
            exe.SetExeFn(action, a, b, c);
            handle.Init(exe, key, timeStamp, isLocalTime);
            _timeStamp.Add(handle);
            return key;
        }

        public void RemoveTimeStamp(Action action)
        {
            if (action == null) return;
            var key = GetKey(action, _timeStamp);
            _timeStamp.Remove(key);
        }

        public void RemoveTimeStamp<A>(Action<A> action)
        {
            if (action == null) return;
            var key = GetKey(action, _timeStamp);
            _timeStamp.Remove(key);
        }

        public void RemoveTimeStamp<A, B>(Action<A, B> action)
        {
            if (action == null) return;
            var key = GetKey(action, _timeStamp);
            _timeStamp.Remove(key);
        }

        public void RemoveTimeStamp<A, B, C>(Action<A, B, C> action)
        {
            if (action == null) return;
            var key = GetKey(action, _timeStamp);
            _timeStamp.Remove(key);
        }

        #endregion TimeStamp

        #region Cron表达式

        public long DoCron(string cron, Action action, bool isLocalTime = false)
        {
            if (action == null) return 0;
            var handle = CreateHandle<CronHandle>(out var key, _cron, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe>();
            exe.SetExeFn(action);
            if (!handle.Init(exe, key, cron, isLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
        }

        public long DoCron<A>(string cron, Action<A> action, A a, bool isLocalTime = false)
        {
            if (action == null) return 0;
            var handle = CreateHandle<CronHandle>(out var key, _cron, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A>>();
            exe.SetExeFn(action, a);
            if (!handle.Init(exe, key, cron, isLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
        }

        public long DoCron<A, B>(string cron, Action<A, B> action, A a, B b, bool isLocalTime = false)
        {
            if (action == null) return 0;
            var handle = CreateHandle<CronHandle>(out var key, _cron, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A, B>>();
            exe.SetExeFn(action, a, b);
            if (!handle.Init(exe, key, cron, isLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
        }

        public long DoCron<A, B, C>(string cron, Action<A, B, C> action, A a, B b, C c, bool isLocalTime = false)
        {
            if (action == null) return 0;
            var handle = CreateHandle<CronHandle>(out var key, _cron, action);
            if (handle == default) return 0;
            var exe = Pool.Get<HandleExe<A, B, C>>();
            exe.SetExeFn(action, a, b, c);
            if (!handle.Init(exe, key, cron, isLocalTime))
            {
                handle.Status = Status.WaitDel;
                handle.Release();
                return 0;
            }

            _cron.Add(handle);
            return key;
        }

        public void RemoveCron(Action action)
        {
            if (action == null) return;
            var key = GetKey(action, _cron);
            _cron.Remove(key);
        }

        public void RemoveCron<A>(Action<A> action)
        {
            if (action == null) return;
            var key = GetKey(action, _cron);
            _cron.Remove(key);
        }

        public void RemoveCron<A, B>(Action<A, B> action)
        {
            if (action == null) return;
            var key = GetKey(action, _cron);
            _cron.Remove(key);
        }

        public void RemoveCron<A, B, C>(Action<A, B, C> action)
        {
            if (action == null) return;
            var key = GetKey(action, _cron);
            _cron.Remove(key);
        }

        #endregion
    }
}