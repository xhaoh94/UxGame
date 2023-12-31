using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ux
{
    public partial class EventMgr : Singleton<EventMgr>
    {
        //每帧执行上限-超出上限，下一帧处理
        public const int ExeLimit = 200;
        private readonly Dictionary<long, IEvent> _keyEvent = new Dictionary<long, IEvent>();
        /// <summary>
        /// 事件ID对应的所有IEvent
        /// </summary>
        private readonly Dictionary<int, List<long>> _eTypeKeys = new Dictionary<int, List<long>>();
        /// <summary>
        /// 标签对应的所有IEvent
        /// </summary>
        private readonly Dictionary<int, List<long>> _tagKeys = new Dictionary<int, List<long>>();
        /// <summary>
        /// 函数对应的所有IEvent
        /// </summary>
        private readonly Dictionary<int, List<long>> _actionKeys = new Dictionary<int, List<long>>();

        private readonly Queue<IEventExe> _waitExes = new Queue<IEventExe>();
        private readonly List<IEvent> _waitAdds = new List<IEvent>();
        private readonly List<long> _waitDels = new List<long>();

        protected override void OnInit()
        {
            TimeMgr.Ins.DoUpdate(_Update);
        }

        Type ___hotfixEvtAttribute;

        public void ___SetEvtAttribute<T>() where T : Attribute, IEvtAttribute
        {
            ___hotfixEvtAttribute = typeof(T);
        }

        public void ___RegisterFastMethod(object target)
        {
            ___RegisterFastMethod(typeof(MainEvtAttribute), target);
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
                    if (!string.IsNullOrEmpty(evtAttr.ETypeStr))
                    {
                        On(evtAttr.ETypeStr, evtAttr.EType, fastMethod);
                        continue;
                    }
#endif
                    On(evtAttr.EType, fastMethod);
                }
            }
        }
        private long GetKey(int eType, object action, object tag)
        {
            if (tag == null)
            {
                return IDGenerater.GenerateId(eType, action.GetHashCode());
            }
            else
            {
                return IDGenerater.GenerateId(eType, action.GetHashCode(), tag.GetHashCode());
            }
        }
        private long GetKey(int eType, object tag, Delegate action)
        {
            if (action == null) return 0;
            return GetKey(eType, action, tag);
        }

        private long GetKey(int eType, FastMethodInfo action)
        {
            if (action == null) return 0;
            return GetKey(eType, action.Method, action.Target);
        }

        void _Update()
        {
            while (_waitDels.Count > 0)
            {
                var key = _waitDels[0];
                _waitDels.RemoveAt(0);
                if (!_keyEvent.TryGetValue(key, out var evt)) continue;
                var eType = evt.EType;
                if (_eTypeKeys.TryGetValue(eType, out var typeKeys))
                {
                    typeKeys.Remove(key);
                    if (typeKeys.Count == 0) _eTypeKeys.Remove(eType);
                }

                var actionHashCode = evt.Method.GetHashCode();
                if (_actionKeys.TryGetValue(actionHashCode, out var aKeys))
                {
                    aKeys.Remove(key);
                    if (aKeys.Count == 0) _actionKeys.Remove(actionHashCode);
                }

                var target = evt.Tag;
                if (target != null)
                {
                    int hashCode = target.GetHashCode();
                    if (_tagKeys.TryGetValue(hashCode, out var tKeys))
                    {
                        tKeys.Remove(key);
                        if (tKeys.Count == 0) _tagKeys.Remove(hashCode);
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
                _keyEvent.Remove(key);
            }

            while (_waitAdds.Count > 0)
            {
                var evt = _waitAdds[0];
                _waitAdds.RemoveAt(0);
                _keyEvent.Add(evt.Key, evt);
                var eType = evt.EType;
                if (!_eTypeKeys.TryGetValue(eType, out var typeKeys))
                {
                    typeKeys = new List<long>();
                    _eTypeKeys.Add(eType, typeKeys);
                }
                var key = evt.Key;
                if (!typeKeys.Contains(key)) typeKeys.Insert(0, key);

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


                var actionHashCode = evt.Method.GetHashCode();
                if (!_actionKeys.TryGetValue(actionHashCode, out var aKeys))
                {
                    aKeys = new List<long>();
                    _actionKeys.Add(actionHashCode, aKeys);
                }

                if (!aKeys.Contains(key)) aKeys.Insert(0, key);

                var target = evt.Tag;
                if (target != null)
                {
                    int hashCode = target.GetHashCode();
                    if (!_tagKeys.TryGetValue(hashCode, out var tKeys))
                    {
                        tKeys = new List<long>();
                        _tagKeys.Add(hashCode, tKeys);
                    }

                    if (!tKeys.Contains(key)) tKeys.Insert(0, key);
                }
            }

            _exeCnt = 0;
            while (_waitExes.Count > 0 && _exeCnt < ExeLimit)
            {
                var exe = _waitExes.Dequeue();
                exe.Exe(ref _exeCnt);
            }
        }
        int _exeCnt = 0;

        Action _quitEvent;

        public void OnQuit(Action action)
        {
            _quitEvent += action;
        }
        public void OffQuit(Action action)
        {
            _quitEvent -= action;
        }

        public void OnApplicationQuit()
        {
            _quitEvent?.Invoke();
        }

#if UNITY_EDITOR
        public long On(string eTypeStr, int eType, FastMethodInfo action)
        {
            var evtData = _Add(out var key, eType, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }

        public long On(string eTypeStr, int eType, object tag, Action action)
        {
            var evtData = _Add<EventData>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }

        public long On<A>(string eTypeStr, int eType, object tag, Action<A> action)
        {
            var evtData = _Add<EventData<A>>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }

        public long On<A, B>(string eTypeStr, int eType, object tag, Action<A, B> action)
        {
            var evtData = _Add<EventData<A, B>>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }

        public long On<A, B, C>(string eTypeStr, int eType, object tag, Action<A, B, C> action)
        {
            var evtData = _Add<EventData<A, B, C>>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
                evtData.Init(eTypeStr);
            }

            return key;
        }
#endif
        public long On(int eType, FastMethodInfo action)
        {
            var evtData = _Add(out var key, eType, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, action);
            }

            return key;
        }

        public long On(int eType, object tag, Action action)
        {
            var evtData = _Add<EventData>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
            }
            return key;
        }

        public long On<A>(int eType, object tag, Action<A> action)
        {
            var evtData = _Add<EventData<A>>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
            }
            return key;
        }

        public long On<A, B>(int eType, object tag, Action<A, B> action)
        {
            var evtData = _Add<EventData<A, B>>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
            }
            return key;
        }

        public long On<A, B, C>(int eType, object tag, Action<A, B, C> action)
        {
            var evtData = _Add<EventData<A, B, C>>(out var key, eType, tag, action);
            if (evtData != default)
            {
                evtData.Init(key, eType, tag, action);
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

            if (_keyEvent.TryGetValue(key, out var temEvt)) return (T)temEvt;
            var evtData = _waitAdds.Find(x => x.Key == key);
            if (evtData != null) return (T)evtData;
            evtData = Pool.Get<T>();
            _waitAdds.Add(evtData);
            return (T)evtData;
        }

        private T _Add<T>(out long key, int eType, object tag, Delegate action) where T : IEvent
        {
            key = GetKey(eType, action, tag);
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
        public void Off(int eType, object tag, Action action)
        {
            _Remove(eType, tag, action);
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
        public void Off<A>(int eType, object tag, Action<A> action)
        {
            _Remove(eType, tag, action);
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
        public void Off<A, B>(int eType, object tag, Action<A, B> action)
        {
            _Remove(eType, tag, action);
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
        public void Off<A, B, C>(int eType, object tag, Action<A, B, C> action)
        {
            _Remove(eType, tag, action);
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
        /// 删除所有注册此标签的事件
        /// </summary>
        public void OffTag(object tag)
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
            if (!_tagKeys.TryGetValue(hashCode, out var keys)) return;
            RemoveByKey(keys);
        }

        private void _Remove(int eType, object tag, Delegate action)
        {
            var key = GetKey(eType, action, tag);
            RemoveByKey(key);
        }

        private void _Remove(Delegate action)
        {
            var hashCode = action.GetHashCode();
            if (_waitAdds.Count > 0)
            {
                for (int i = _waitAdds.Count - 1; i >= 0; i--)
                {
                    var wa = _waitAdds[i];
                    if (wa.Method == action)
                    {
                        _waitAdds.RemoveAt(i);
                    }
                }
            }

            if (!_actionKeys.TryGetValue(hashCode, out var keys)) return;
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

            if (!_keyEvent.ContainsKey(key))
            {
                if (_waitAdds.Count > 0)
                {
                    var addIndex = _waitAdds.FindIndex(x => x.Key == key);
                    if (addIndex >= 0)
                    {
                        _waitAdds.RemoveAt(addIndex);
                    }
                }
                return;
            }
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