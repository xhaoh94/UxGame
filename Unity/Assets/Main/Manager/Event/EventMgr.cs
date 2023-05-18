using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ux
{
    public partial class EventMgr : Singleton<EventMgr>
    {
        #region Exe

        interface IEventExe
        {
            void Exe(ref int exeCnt);
        }

        readonly struct EventExe : IEventExe
        {
            readonly int eType;

            public EventExe(int _eType)
            {
                eType = _eType;
            }

            public void Exe(ref int exeCnt)
            {
                if (!Ins.eTypeKeys.TryGetValue(eType, out var keys)) return;
                for (var i = keys.Count - 1; i >= 0; i--)
                {
                    var key = keys[i];
                    if (!Ins.keyEvent.TryGetValue(key, out var aEvent)) continue;
                    if (Ins._waitDels.Count > 0 && Ins._waitDels.Contains(key)) continue;
                    try
                    {
                        aEvent?.Run();
                        exeCnt++;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        readonly struct EventExe<A> : IEventExe
        {
            readonly int eType;
            readonly A a;

            public EventExe(int _eType, A _a)
            {
                eType = _eType;
                a = _a;
            }

            public void Exe(ref int exeCnt)
            {
                if (!Ins.eTypeKeys.TryGetValue(eType, out var keys)) return;
                for (var i = keys.Count - 1; i >= 0; i--)
                {
                    var key = keys[i];
                    if (!Ins.keyEvent.TryGetValue(key, out var aEvent)) continue;
                    if (Ins._waitDels.Count > 0 && Ins._waitDels.Contains(key)) continue;
                    try
                    {
                        aEvent?.Run(a);
                        exeCnt++;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        readonly struct EventExe<A, B> : IEventExe
        {
            readonly int eType;
            readonly A a;
            readonly B b;

            public EventExe(int _eType, A _a, B _b)
            {
                eType = _eType;
                a = _a;
                b = _b;
            }

            public void Exe(ref int exeCnt)
            {
                if (!Ins.eTypeKeys.TryGetValue(eType, out var keys)) return;
                for (var i = keys.Count - 1; i >= 0; i--)
                {
                    var key = keys[i];
                    if (!Ins.keyEvent.TryGetValue(key, out var aEvent)) continue;
                    if (Ins._waitDels.Count > 0 && Ins._waitDels.Contains(key)) continue;
                    try
                    {
                        aEvent?.Run(a, b);
                        exeCnt++;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        readonly struct EventExe<A, B, C> : IEventExe
        {
            readonly int eType;
            readonly A a;
            readonly B b;
            readonly C c;

            public EventExe(int _eType, A _a, B _b, C _c)
            {
                eType = _eType;
                a = _a;
                b = _b;
                c = _c;
            }

            public void Exe(ref int exeCnt)
            {
                if (!Ins.eTypeKeys.TryGetValue(eType, out var keys)) return;
                for (var i = keys.Count - 1; i >= 0; i--)
                {
                    var key = keys[i];
                    if (!Ins.keyEvent.TryGetValue(key, out var aEvent)) continue;
                    if (Ins._waitDels.Count > 0 && Ins._waitDels.Contains(key)) continue;
                    try
                    {
                        aEvent?.Run(a, b, c);
                        exeCnt++;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        #endregion

        //每帧执行上限-超出上限，下一帧处理
        public const int ExeLimit = 1000;
        private readonly Dictionary<int, List<long>> eTypeKeys = new Dictionary<int, List<long>>();
        private readonly Dictionary<long, IEvent> keyEvent = new Dictionary<long, IEvent>();
        private readonly Dictionary<int, List<long>> targetKeys = new Dictionary<int, List<long>>();
        private readonly Dictionary<int, List<long>> actionKeys = new Dictionary<int, List<long>>();

        private readonly Queue<IEventExe> _waitExes = new Queue<IEventExe>();
        private readonly List<IEvent> _waitAdds = new List<IEvent>();
        private readonly List<long> _waitDels = new List<long>();
        

#if UNITY_EDITOR
        private readonly Dictionary<string, EventList> type2editor = new Dictionary<string, EventList>();

        public static void __Debugger_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_CallBack?.Invoke(Ins.type2editor);
            }
        }

        public static Action<Dictionary<string, EventList>> __Debugger_CallBack;


#endif

        Type ___hotfixEvtAttribute;

        public void ___SetEvtAttribute<T>() where T : Attribute, IEvtAttribute
        {
            ___hotfixEvtAttribute = typeof(T);
        }

        public void ___RegisterFastMethod(object target)
        {
            ___RegisterFastMethod(typeof(Main.EvtAttribute), target);
            if (___hotfixEvtAttribute != null)
            {
                ___RegisterFastMethod(___hotfixEvtAttribute, target);
            }
        }

        void ___RegisterFastMethod(Type type, object target)
        {
            var methods = target.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                                      BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var evtAttrs = method.GetCustomAttributes(type).ToArray();
                if (!evtAttrs.Any()) continue;
                var fastMethod = new FastMethodInfo(target, method);
                foreach (var attr in evtAttrs)
                {
                    var evtAttr = (IEvtAttribute)attr;
#if UNITY_EDITOR
                    if (string.IsNullOrEmpty(evtAttr.ETypeStr))
                    {
                        On(evtAttr.EType, fastMethod);
                    }
                    else
                    {
                        On(evtAttr.ETypeStr, evtAttr.EType, fastMethod);
                    }
#else
                        On(evtAttr.EType, fastMethod);
#endif
                }
            }
        }

        private long GetKey(int eType, Delegate action)
        {
            if (action == null) return 0;
            var key = ((long)eType << 32);
            key += Math.Abs(action.GetHashCode());
            var target = action.Target;
            if (target != null)
            {
                var targetHashCode = Math.Abs((long)target.GetHashCode());
                key |= targetHashCode;
            }

            return key;
        }

        private long GetKey(int eType, FastMethodInfo action)
        {
            if (action == null) return 0;
            var key = ((long)eType << 32);
            key += Math.Abs(action.GetHashCode());
            var target = action.Target;
            if (target != null)
            {
                var targetHashCode = Math.Abs((long)target.GetHashCode());
                key |= targetHashCode;
            }

            return key;
        }

        public void Update()
        {
            while (_waitDels.Count > 0)
            {
                var key = _waitDels[0];
                _waitDels.RemoveAt(0);
                if (!keyEvent.TryGetValue(key, out var evt)) continue;
                var eType = evt.EType;
                var target = evt.Target;
                var actionHashCode = evt.Method.GetHashCode();
                if (eTypeKeys.TryGetValue(eType, out var typeKeys))
                {
                    typeKeys.Remove(key);
                    if (typeKeys.Count == 0) eTypeKeys.Remove(eType);
                }

                if (actionKeys.TryGetValue(actionHashCode, out var aKeys))
                {
                    aKeys.Remove(key);
                    if (aKeys.Count == 0) actionKeys.Remove(actionHashCode);
                }

                if (target != null)
                {
                    int hashCode = target.GetHashCode();
                    if (targetKeys.TryGetValue(hashCode, out var tKeys))
                    {
                        tKeys.Remove(key);
                        if (tKeys.Count == 0) targetKeys.Remove(hashCode);
                    }
                }
#if UNITY_EDITOR
                var eTypeStr = evt.ETypeStr;
                if (string.IsNullOrEmpty(eTypeStr)) eTypeStr = eType.ToString();
                if (type2editor.TryGetValue(eTypeStr, out var t2eList))
                {
                    if (t2eList.Remove(evt.MethodName))
                    {
                        type2editor.Remove(eTypeStr);
                        __Debugger_Event();
                    }
                }
#endif

                evt.Release();
                keyEvent.Remove(key);
            }

            while (_waitAdds.Count > 0)
            {
                var evt = _waitAdds[0];
                _waitAdds.RemoveAt(0);
                keyEvent.Add(evt.Key, evt);
                var eType = evt.EType;
                var key = evt.Key;
                var target = evt.Target;
                var actionHashCode = evt.Method.GetHashCode();
                if (!eTypeKeys.TryGetValue(eType, out var typeKeys))
                {
                    typeKeys = new List<long>();
                    eTypeKeys.Add(eType, typeKeys);
                }

#if UNITY_EDITOR
                var eTypeStr = evt.ETypeStr;
                if (string.IsNullOrEmpty(eTypeStr)) eTypeStr = eType.ToString();
                if (!type2editor.TryGetValue(eTypeStr, out var t2eList))
                {
                    t2eList = new EventList(eTypeStr);
                    type2editor.Add(eTypeStr, t2eList);
                }

                t2eList.Add(evt.MethodName);
                __Debugger_Event();
#endif

                if (!typeKeys.Contains(key)) typeKeys.Insert(0, key);

                if (!actionKeys.TryGetValue(actionHashCode, out var aKeys))
                {
                    aKeys = new List<long>();
                    actionKeys.Add(actionHashCode, aKeys);
                }

                if (!aKeys.Contains(key)) aKeys.Insert(0, key);

                if (target != null)
                {
                    int hashCode = target.GetHashCode();
                    if (!targetKeys.TryGetValue(hashCode, out var tKeys))
                    {
                        tKeys = new List<long>();
                        targetKeys.Add(hashCode, tKeys);
                    }

                    if (!tKeys.Contains(key)) tKeys.Insert(0, key);
                }
            }

            int exeCnt = 0;
            while (_waitExes.Count > 0 && exeCnt < ExeLimit)
            {
                var exe = _waitExes.Dequeue();
                exe.Exe(ref exeCnt);
            }
        }

        Action _quitCb;

        public void AddQuitCallBack(Action action)
        {
            _quitCb += action;
        }
        public void RemoveQuitCallBack(Action action)
        {
            _quitCb -= action;
        }

        public void OnApplicationQuit()
        {
            _quitCb?.Invoke();
        }

#if UNITY_EDITOR
        public long On(string eTypeStr, int eType, FastMethodInfo action)
        {
            var evtData = _Add(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
                evtData.SetETypeStr(eTypeStr);
            }

            return key;
        }

        public long On(string eTypeStr, int eType, Action action)
        {
            var evtData = _Add<EventData>(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
                evtData.SetETypeStr(eTypeStr);
            }

            return key;
        }

        public long On<A>(string eTypeStr, int eType, Action<A> action)
        {
            var evtData = _Add<EventData<A>>(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
                evtData.SetETypeStr(eTypeStr);
            }

            return key;
        }

        public long On<A, B>(string eTypeStr, int eType, Action<A, B> action)
        {
            var evtData = _Add<EventData<A, B>>(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
                evtData.SetETypeStr(eTypeStr);
            }

            return key;
        }

        public long On<A, B, C>(string eTypeStr, int eType, Action<A, B, C> action)
        {
            var evtData = _Add<EventData<A, B, C>>(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
                evtData.SetETypeStr(eTypeStr);
            }

            return key;
        }
#endif
        public long On(int eType, FastMethodInfo action)
        {
            var evtData = _Add(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
            }

            return key;
        }

        public long On(int eType, Action action)
        {
            var evtData = _Add<EventData>(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
            }

            return key;
        }

        public long On<A>(int eType, Action<A> action)
        {
            var evtData = _Add<EventData<A>>(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
            }

            return key;
        }

        public long On<A, B>(int eType, Action<A, B> action)
        {
            var evtData = _Add<EventData<A, B>>(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
            }

            return key;
        }

        public long On<A, B, C>(int eType, Action<A, B, C> action)
        {
            var evtData = _Add<EventData<A, B, C>>(out var key, eType, action);
            if (evtData != default)
            {
                evtData.SetExeFn(key, eType, action);
            }

            return key;
        }

        private T _Add<T>(long key) where T : IEvent
        {
            if (key == 0) return default(T);
            if (_waitDels.Count > 0)
            {
                var delIndex = _waitDels.IndexOf(key);
                if (delIndex >= 0)
                {
                    _waitDels.RemoveAt(delIndex);
                }
            }

            if (keyEvent.ContainsKey(key)) return default;
            var evtData = Pool.Get<T>();
            _waitAdds.Add(evtData);
            return evtData;
        }

        private T _Add<T>(out long key, int eType, Delegate action) where T : IEvent
        {
            key = GetKey(eType, action);
            return _Add<T>(key);
        }

        private EventFastMethodData _Add(out long key, int eType, FastMethodInfo action)
        {
            key = GetKey(eType, action);
            return _Add<EventFastMethodData>(key);
        }

        public void Off(int eType, FastMethodInfo action)
        {
            var key = GetKey(eType, action);
            RemoveByKey(key);
        }

        /// <summary>
        /// 删除对应的事件和注册方法
        /// </summary>
        /// <param name="eType"></param>
        /// <param name="action"></param>
        public void Off(int eType, Action action)
        {
            _Remove(eType, action);
        }

        /// <summary>
        /// 删除所有注册此方法的事件
        /// </summary>
        /// <param name="action"></param>
        public void Off(Action action)
        {
            _Remove(action);
        }

        /// <summary>
        /// 删除对应的事件和注册方法
        /// </summary>
        /// <param name="eType"></param>
        /// <param name="action"></param>
        public void Off<A>(int eType, Action<A> action)
        {
            _Remove(eType, action);
        }

        /// <summary>
        /// 删除所有注册此方法的事件
        /// </summary>
        /// <param name="action"></param>
        public void Off<A>(Action<A> action)
        {
            _Remove(action);
        }

        /// <summary>
        /// 删除对应的事件和注册方法
        /// </summary>
        /// <param name="eType"></param>
        /// <param name="action"></param>
        public void Off<A, B>(int eType, Action<A, B> action)
        {
            _Remove(eType, action);
        }

        /// <summary>
        /// 删除所有注册此方法的事件
        /// </summary>
        /// <param name="action"></param>
        public void Off<A, B>(Action<A, B> action)
        {
            _Remove(action);
        }

        /// <summary>
        /// 删除对应的事件和注册方法
        /// </summary>
        /// <param name="eType"></param>
        /// <param name="action"></param>
        public void Off<A, B, C>(int eType, Action<A, B, C> action)
        {
            _Remove(eType, action);
        }

        /// <summary>
        /// 删除所有注册此方法的事件
        /// </summary>
        /// <param name="action"></param>
        public void Off<A, B, C>(Action<A, B, C> action)
        {
            _Remove(action);
        }

        /// <summary>
        /// 删除所有注册此实例的事件
        /// </summary>
        public void OffAll(object target)
        {
            if (target == null) return;
            int hashCode = target.GetHashCode();
            if (!targetKeys.TryGetValue(hashCode, out var keys)) return;
            RemoveByKey(keys);
        }

        private void _Remove(int eType, Delegate action)
        {
            var key = GetKey(eType, action);
            RemoveByKey(key);
        }

        private void _Remove(Delegate action)
        {
            var hashCode = action.GetHashCode();
            if (!actionKeys.TryGetValue(hashCode, out var keys)) return;
            RemoveByKey(keys);
        }

        public void RemoveByKey(List<long> keys)
        {
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                RemoveByKey(keys[i]);
            }
        }

        public void RemoveByKey(long key)
        {
            if (key == 0) return;
            if (_waitAdds.Count > 0)
            {
                var addIndex = _waitAdds.FindIndex(x => x.Key == key);
                if (addIndex >= 0)
                {
                    _waitAdds.RemoveAt(addIndex);
                }
            }

            if (!keyEvent.ContainsKey(key)) return;
            if (_waitDels.Contains(key)) return;
            _waitDels.Add(key);
        }

        public void Send(int eType)
        {
            _waitExes.Enqueue(new EventExe(eType));
        }

        public void Send<A>(int eType, A a)
        {
            _waitExes.Enqueue(new EventExe<A>(eType, a));
        }

        public void Send<A, B>(int eType, A a, B b)
        {
            _waitExes.Enqueue(new EventExe<A, B>(eType, a, b));
        }

        public void Send<A, B, C>(int eType, A a, B b, C c)
        {
            _waitExes.Enqueue(new EventExe<A, B, C>(eType, a, b, c));
        }
    }
}