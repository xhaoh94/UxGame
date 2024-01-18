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

        partial class HandleMap
        {
            readonly TimeType _timeType;
            public HandleMap(TimeType timeType)
            {
                _timeType = timeType;
            }

            readonly List<IHandle> _handles = new List<IHandle>();
            readonly Dictionary<long, IHandle> _keyHandle = new Dictionary<long, IHandle>();
            readonly Dictionary<int, List<long>> _tagkeys = new Dictionary<int, List<long>>();

            readonly List<IHandle> _waitAdds = new List<IHandle>();
            readonly List<IHandle> _waitDels = new List<IHandle>();

            public void Clear()
            {
                _handles.Clear();
                _keyHandle.Clear();
                _tagkeys.Clear();
                _waitAdds.Clear();
                _waitDels.Clear();
#if UNITY_EDITOR
                _descEditor.Clear();
#endif
            }


            bool _needSort;

            public void Run()
            {
                while (_waitDels.Count > 0)
                {
                    var handle = _waitDels[0];
                    _waitDels.RemoveAt(0);
#if UNITY_EDITOR
                    var exeDesc = handle.MethodName;
                    if (_descEditor.TryGetValue(exeDesc, out var temList))
                    {
                        if (temList.Remove(handle))
                        {
                            _descEditor.Remove(exeDesc);
                            __Debugger_Event();
                        }
                    }
#endif
                    var key = handle.Key;
                    _keyHandle.Remove(key);

                    var tag = handle.Tag;
                    if (tag != null)
                    {
                        var hashCode = tag.GetHashCode();
                        if (!_tagkeys.TryGetValue(hashCode, out var keys)) continue;
                        keys.Remove(key);
                        if (keys.Count == 0) _tagkeys.Remove(hashCode);
                    }

                    handle.Release();
                    _handles.Remove(handle);
                }

                if (_needSort)
                {
                    _handles.Sort((a, b) => b.Compare(a));
                }

#if UNITY_EDITOR
                if (__isEvent)
                {
                    __Debugger_Event();
                }
#endif


                while (_waitAdds.Count > 0)
                {
                    var handle = _waitAdds[0];
#if UNITY_EDITOR
                    var exeDesc = handle.MethodName;
                    if (!_descEditor.TryGetValue(exeDesc, out var temList))
                    {
                        temList = new TimeList(exeDesc);
                        _descEditor.Add(exeDesc, temList);
                    }

                    temList.Add(handle);
                    __Debugger_Event();
#endif 

                    _waitAdds.RemoveAt(0);
                    Sort(handle);
                    _keyHandle.Add(handle.Key, handle);

                    var tag = handle.Tag;
                    if (tag != null)
                    {
                        int hashCode = tag.GetHashCode();
                        if (!_tagkeys.TryGetValue(hashCode, out var keys))
                        {
                            keys = new List<long>();
                            _tagkeys.Add(hashCode, keys);
                        }

                        keys.Add(handle.Key);
                    }
                }

                OnRun();
            }

            void OnRun()
            {
                _needSort = false;
#if UNITY_EDITOR
                __isEvent = false;
#endif
                if (_handles.Count <= 0) return;

                for (var i = _handles.Count - 1; i >= 0; i--)
                {
                    var handler = _handles[i];
                    switch (handler.Run())
                    {
                        case RunStatus.Wait:
                            return;
                        case RunStatus.Update:
#if UNITY_EDITOR
                            __isEvent = true;
#endif
                            _needSort = true;
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
                var endIndex = _handles.Count;
                int loopCnt = 0;
                while (true)
                {
                    if (endIndex - stratIndex <= 10)
                    {
                        int insertIndex = -1;
                        for (var i = stratIndex; i < endIndex; i++)
                        {
                            if (handle.Compare(_handles[i]) >= 0)
                            {
                                insertIndex = i;
                                break;
                            }
                        }

                        if (insertIndex != -1)
                        {
                            _handles.Insert(insertIndex, handle);
                        }
                        else
                        {
                            _handles.Add(handle);
                        }

                        return;
                    }

                    var index = stratIndex + ((endIndex - stratIndex) >> 1);
                    if (handle.Compare(_handles[index]) == 0)
                    {
                        _handles.Insert(index, handle);
                        return;
                    }
                    else if (handle.Compare(_handles[index]) > 0)
                    {
                        endIndex = index;
                    }
                    else
                    {
                        stratIndex = index;
                    }

                    loopCnt++;
                    if (loopCnt > 1000)
                    {
                        Log.Error("时间排序循环超出");
                        break;
                    }
                }
            }

            public void Add(IHandle handle)
            {
                if (_waitAdds.Contains(handle)) return;
                handle.Status = Status.Normal;
                _waitAdds.Add(handle);
            }

            public IHandle Get(long key)
            {
                if (_keyHandle.TryGetValue(key, out var handle) && handle.Status == Status.Normal)
                {
                    return handle;
                }
                return _waitAdds.Find(x => x.Key == key);
            }

            public void RemoveAll(object tag)
            {
                if (tag == null) return;
                if (_waitAdds.Count > 0)
                {
                    for (int i = _waitAdds.Count - 1; i >= 0; i--)
                    {
                        var wa = _waitAdds[i];
                        if (wa.Tag == tag)
                        {
                            _waitAdds.RemoveAt(i);
                        }
                    }
                }
                int hashCode = tag.GetHashCode();
                if (!_tagkeys.TryGetValue(hashCode, out var keys)) return;
                foreach (var key in keys)
                {
                    Remove(key);
                }
            }

            public void Remove(long key)
            {
                if (!_keyHandle.TryGetValue(key, out var handle))
                {
                    if (_waitAdds.Count > 0)
                    {
                        var index = _waitAdds.FindIndex(x => x.Key == key);
                        if (index >= 0) _waitAdds.RemoveAt(index);
                    }
                    return;
                }
                Remove(handle);
            }

            void Remove(IHandle handle)
            {
                if (_waitDels.Contains(handle)) return;
                handle.Status = Status.WaitDel;
                _waitDels.Add(handle);
            }
        }

        public float TotalTime => Time.unscaledTime;
        public float TotalFrame => Time.frameCount;

        public IGameTime ServerTime { get; private set; }
        public IGameTime LocalTime { get; private set; }

        private readonly HandleMap _timer = new HandleMap(TimeType.Time);
        private readonly HandleMap _frame = new HandleMap(TimeType.Frame);
        private readonly HandleMap _timeStamp = new HandleMap(TimeType.TimeStamp);
        private readonly HandleMap _cron = new HandleMap(TimeType.Cron);

        protected override void OnCreated()
        {
            LocalTime = new LocalTime();
            ServerTime = new ServerTime();
            GameMain.Ins.AddUpdate(_Update);
            GameMain.Ins.AddFixedUpdate(_FixedUpdate);
        }
        public void __SetServerTime(long timeStamp)
        {
            var offset = timeStamp - LocalTime.TimeStamp;
            (ServerTime as ServerTime)?.SetOffset(offset);
        }

        void _FixedUpdate()
        {            
            _timer?.Run();
            _timeStamp?.Run();
            _cron?.Run();
        }

        void _Update()
        {            
            _frame?.Run();
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
                key = (long)IDGenerater.GenerateId(action.GetHashCode(), dic.GetHashCode());
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

        public void Release()
        {
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