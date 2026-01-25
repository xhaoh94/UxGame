using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ux
{
    public partial class EventMgr
    {
        /// <summary>事件签名，用于重复注册检测</summary>
        public struct EventSignature : System.IEquatable<EventSignature>
        {
            public Delegate Action;
            public object Tag;
            public int EType;

            public bool Equals(EventSignature other)
            {
                return Action == other.Action &&
                       Tag == other.Tag &&
                       EType == other.EType;
            }

            public override bool Equals(object obj)
            {
                return obj is EventSignature other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + (Action != null ? RuntimeHelpers.GetHashCode(Action) : 0);
                    hash = hash * 31 + (Tag != null ? RuntimeHelpers.GetHashCode(Tag) : 0);
                    hash = hash * 31 + EType;
                    return hash;
                }
            }
        }

        public struct FastMethodRef
        {
            Attribute[] _attributes;
            FastMethodInfo _methodInfo;
            public FastMethodRef(Attribute[] attributes, FastMethodInfo methodInfo)
            {
                _attributes = attributes;
                _methodInfo = methodInfo;
            }
            public void On(EventMgr.EventSystem system)
            {
                foreach (var attr in _attributes)
                {
                    var evtAttr = (IEvtAttribute)attr;
                    system.On(evtAttr.EType, _methodInfo);
                }
            }
        }
        OverdueMap<long, List<FastMethodRef>> _fastMethodRefMap;
        public interface IEventSystem
        {
            void Init(int exeLimit);
            void Release();
        }
        public partial class EventSystem : IEventSystem
        {
            //每帧执行数量-防止卡顿，一帧执行
            int _exeLimit = 200;
            int _exeCnt = 0;

            // 签名相关字典：用于重复检测和删除
            private readonly Dictionary<EventSignature, long> _signToKey = new();
            private readonly Dictionary<long, EventSignature> _keyToSign = new();

            private readonly Dictionary<long, IEvent> _keyEvent = new();
            /// <summary>
            /// 事件ID对应的IEvent
            /// </summary>
            private readonly Dictionary<int, HashSet<long>> _eventTypeKeys = new();
            /// <summary>
            /// 标签对应的IEvent
            /// </summary>
            private readonly Dictionary<object, HashSet<long>> _tagKeys = new();
            /// <summary>
            /// 动作对应的IEvent
            /// </summary>
            private readonly Dictionary<int, HashSet<long>> _actionKeys = new();
            private IEventExe _lastExe;
            /// <summary>
            /// 待执行事件队列
            /// </summary>
            private readonly Queue<IEventExe> _waitExes = new();
            private readonly List<IEvent> _waitAdds = new();
            private readonly List<long> _waitDels = new();

            void IEventSystem.Init(int exeLimit)
            {
                _exeLimit = exeLimit;
                GameMethod.Update += _Update;
            }
            void IEventSystem.Release()
            {
                GameMethod.Update -= _Update;
                Clear();
                Pool.Push(this);
            }
            public void Clear()
            {
                _signToKey.Clear();
                _keyToSign.Clear();
                _keyEvent.Clear();
                _eventTypeKeys.Clear();
                _tagKeys.Clear();
                _actionKeys.Clear();
                _waitExes.Clear();
                _waitAdds.Clear();
                _waitDels.Clear();
            }

            Type _hotfixEvtAttribute;

            public void SetEvtAttribute<T>() where T : Attribute, IEvtAttribute
            {
                _hotfixEvtAttribute = typeof(T);
            }

            public void RegisterEventTrigger(IEventTrigger eventObject)
            {
                _RegisterEventTrigger(typeof(MainEvtAttribute), eventObject);
                if (_hotfixEvtAttribute != null)
                {
                    _RegisterEventTrigger(_hotfixEvtAttribute, eventObject);
                }
            }
            void _RegisterEventTrigger(Type type, IEventTrigger eventObject)
            {
                var evtAttrs = eventObject.GetType().GetCustomAttributes(type).ToArray();
                if (!evtAttrs.Any()) return;
                foreach (var attr in evtAttrs)
                {
                    var evtAttr = (IEvtAttribute)attr;
                    On(evtAttr.EType, eventObject, eventObject.OnTriggerEvent);
                }
            }

            public void RegisterFastMethod(object target)
            {
                _RegisterFastMethod(typeof(MainEvtAttribute), target);
                if (_hotfixEvtAttribute != null)
                {
                    _RegisterFastMethod(_hotfixEvtAttribute, target);
                }
            }

            void _RegisterFastMethod(Type type, object target)
            {
                Ins._fastMethodRefMap ??= new OverdueMap<long, List<FastMethodRef>>(100);
                var key = IDGenerater.GenerateId(RuntimeHelpers.GetHashCode(target), RuntimeHelpers.GetHashCode(type));
                if (!Ins._fastMethodRefMap.TryGetValue(key, out var refList))
                {
                    var methods = target.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                                          BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var method in methods)
                    {
                        var evtAttrs = method.GetCustomAttributes(type).ToArray();
                        if (!evtAttrs.Any()) continue;
                        if (refList == null)
                        {
                            refList = new List<FastMethodRef>();
                            Ins._fastMethodRefMap.Add(key, refList);
                        }
                        refList.Add(new FastMethodRef(evtAttrs, new FastMethodInfo(target, method)));
                    }
                }
                if (refList != null && refList.Count > 0)
                {
                    foreach (var refMethod in refList)
                    {
                        refMethod.On(this);
                    }
                }
            }
            private long _GetKey(int eType, Delegate action, object tag)
            {
                // 构造签名用于重复检测
                EventSignature signature = new EventSignature
                {
                    Action = action,
                    Tag = tag,
                    EType = eType
                };

                // 检查是否重复注册
                if (_signToKey.TryGetValue(signature, out var existingKey))
                {
                    Log.Error($"事件{action.MethodName()}重复注册，请检查业务逻辑是否正确。");
                    return 0;
                }

                // 分配全局唯一自增 ID
                long key = IDGenerater.GenerateId();

                // 注册签名映射
                _signToKey[signature] = key;
                _keyToSign[key] = signature;

                return key;
            }

            private long _GetKey(int eType, FastMethodInfo action)
            {
                if (!action.IsValid) return 0;
                return _GetKey(eType, action.Method, action.Target);
            }

            void _Update()
            {
                if (_waitDels.Count > 0)
                {
                    foreach (var key in _waitDels)
                    {
                        if (!_keyEvent.TryGetValue(key, out var evt)) continue;
                        var eType = evt.EType;
                        if (_eventTypeKeys.TryGetValue(eType, out var typeKeys))
                        {
                            typeKeys.Remove(key);
                            if (typeKeys.Count == 0) _eventTypeKeys.Remove(eType);
                        }

                        if (evt.Method != null)
                        {
                            var actionHashCode = RuntimeHelpers.GetHashCode(evt.Method);
                            if (_actionKeys.TryGetValue(actionHashCode, out var aKeys))
                            {
                                aKeys.Remove(key);
                                if (aKeys.Count == 0) _actionKeys.Remove(actionHashCode);
                            }
                        }


                        var target = evt.Tag;
                        if (target != null)
                        {
                            if (_tagKeys.TryGetValue(target, out var tKeys))
                            {
                                tKeys.Remove(key);
                                if (tKeys.Count == 0) _tagKeys.Remove(target);
                            }
                        }

                        // 清理签名映射
                        if (_keyToSign.TryGetValue(key, out var sign))
                        {
                            _keyToSign.Remove(key);
                            _signToKey.Remove(sign);
                        }
#if UNITY_EDITOR
                        if (Ins._defaultSystem == this)
                        {
                            Ins._EditorRemove(evt);
                        }
#endif

                        evt.Release();
                        _keyEvent.Remove(key);
                    }
                    _waitDels.Clear();
                }

                if (_waitAdds.Count > 0)
                {
                    foreach (var evt in _waitAdds)
                    {
                        _keyEvent.Add(evt.Key, evt);
                        var eType = evt.EType;
                        if (!_eventTypeKeys.TryGetValue(eType, out var typeKeys))
                        {
                            typeKeys = new();
                            _eventTypeKeys.Add(eType, typeKeys);
                        }
                        var key = evt.Key;
                        typeKeys.Add(key);

#if UNITY_EDITOR

                        if (Ins._defaultSystem == this)
                        {
                            Ins._EditorAdd(evt);
                        }
#endif

                        if (evt.Method != null)
                        {
                            var actionHashCode = RuntimeHelpers.GetHashCode(evt.Method);
                            if (!_actionKeys.TryGetValue(actionHashCode, out var aKeys))
                            {
                                aKeys = new();
                                _actionKeys.Add(actionHashCode, aKeys);
                            }

                            aKeys.Add(key);
                        }


                        var target = evt.Tag;
                        if (target != null)
                        {
                            if (!_tagKeys.TryGetValue(target, out var tKeys))
                            {
                                tKeys = new();
                                _tagKeys.Add(target, tKeys);
                            }
                            tKeys.Add(key);
                        }
                    }
                    _waitAdds.Clear();
                }

                _exeCnt = 0;
                while (_waitExes.Count > 0 && _exeCnt < _exeLimit)
                {
                    var exe = _waitExes.Dequeue();
                    if (exe.SkipInQueue)
                    {
                        // 跳过执行，直接回收
                        exe.Reset();
                        Pool.Push(exe);
                        continue;
                    }
                    try
                    {
                        exe.Exe(this, ref _exeCnt);
                    }
                    finally
                    {
                        exe.Reset();
                        Pool.Push(exe);
                    }
                }
            }
            private T _Add<T>(long key) where T : IEvent
            {
                if (key == 0) return default;
                if (_waitDels.Count > 0)
                {
                    _waitDels.Remove(key);
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
                key = _GetKey(eType, action, tag);
                return _Add<T>(key);
            }

            private EventFastMethodData _Add(out long key, int eType, FastMethodInfo action)
            {
                key = _GetKey(eType, action);
                return _Add<EventFastMethodData>(key);
            }



            private void _Remove(int eType, object tag, Delegate action)
            {
                var key = _GetKey(eType, action, tag);
                RemoveByKey(key);
            }

            private void _Remove(Delegate action)
            {
                var hashCode = RuntimeHelpers.GetHashCode(action);
                if (_waitAdds.Count > 0)
                {
                    _waitAdds.RemoveAll(x => x.Method == action);
                }

                if (!_actionKeys.TryGetValue(hashCode, out var keys)) return;
                RemoveByKey(keys);
            }

            public EventSystem RemoveByKey(IEnumerable<long> keys)
            {
                foreach (var key in keys)
                {
                    RemoveByKey(key);
                }
                return this;
            }

            public EventSystem RemoveByKey(long key)
            {
                if (key > 0)
                {
                    if (!_keyEvent.ContainsKey(key))
                    {
                        if (_waitAdds.Count > 0)
                        {
                            _waitAdds.RemoveAll(x => x.Key == key);
                        }
                        return this;
                    }
                    _waitDels.Add(key);
                }
                return this;
            }

            public EventSystem Run(int eType)
            {
                var exe = Pool.Get<EventExe>();
                exe.Init(eType);
                _waitExes.Enqueue(exe);
                _lastExe = exe;
                return this;
            }

            public EventSystem Run<A>(int eType, A a)
            {
                var exe = Pool.Get<EventExe<A>>();
                exe.Init(eType, a);
                _waitExes.Enqueue(exe);
                _lastExe = exe;
                return this;
            }

            public EventSystem Run<A, B>(int eType, A a, B b)
            {
                var exe = Pool.Get<EventExe<A, B>>();
                exe.Init(eType, a, b);
                _waitExes.Enqueue(exe);
                _lastExe = exe;
                return this;
            }

            public EventSystem Run<A, B, C>(int eType, A a, B b, C c)
            {
                var exe = Pool.Get<EventExe<A, B, C>>();
                exe.Init(eType, a, b, c);
                _waitExes.Enqueue(exe);
                _lastExe = exe;
                return this;
            }
            //立即执行
            public void Immediate()
            {
                if (_lastExe == null)
                {
                    Log.Error("立即执行事件失败，没有可立即执行事件");
                    return;
                }
                _exeCnt = 0;
                var tmp = _exeLimit;
                _exeLimit = int.MaxValue;
                try
                {
                    _lastExe.Exe(this, ref _exeCnt);
                }
                finally
                {
                    _lastExe.SkipInQueue = true;
                }
                _exeLimit = tmp;
                _lastExe = null;
            }
        }
    }

}
