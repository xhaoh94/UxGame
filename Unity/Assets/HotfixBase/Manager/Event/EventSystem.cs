using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ux
{
    public partial class EventMgr
    {
        /// <summary>事件签名，用于重复注册检测</summary>
        public struct EventSignature : IEquatable<EventSignature>
        {
            public Delegate Action;
            public object Tag;
            public int EType;

            public bool Equals(EventSignature other)
            {
                return ReferenceEquals(Action, other.Action) &&
                       ReferenceEquals(Tag, other.Tag) &&
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
            readonly Attribute[] _attributes;
            readonly FastMethodInfo _methodInfo;

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
            private readonly Dictionary<Delegate, HashSet<long>> _actionKeys = new();
            private IEventExe _lastExe;
             /// <summary>
            /// 待执行事件队列
            /// </summary>
            private readonly Queue<IEventExe> _waitExes = new();
            private readonly Dictionary<long, IEvent> _waitAdds = new();
            private readonly HashSet<long> _waitDels = new();
            private readonly List<long> _dispatchKeys = new();
            private readonly List<long> _tempKeys = new();

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
                _dispatchKeys.Clear();
                _tempKeys.Clear();
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
                var evtAttrs = Attribute.GetCustomAttributes(eventObject.GetType(), type);
                if (evtAttrs.Length == 0) return;
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
                        var evtAttrs = Attribute.GetCustomAttributes(method, type);
                        if (evtAttrs.Length == 0) continue;
                        if (refList == null)
                        {
                            refList = new List<FastMethodRef>();
                            Ins._fastMethodRefMap.Add(key, refList);
                        }

                        refList.Add(new FastMethodRef(evtAttrs, new FastMethodInfo(target, method)));
                    }
                }

                if (refList == null || refList.Count == 0) return;
                foreach (var refMethod in refList)
                {
                    refMethod.On(this);
                }
            }

            private EventSignature _CreateSignature(int eType, Delegate action, object tag)
            {
                return new EventSignature
                {
                    Action = action,
                    Tag = tag,
                    EType = eType
                };
            }

            private long _GetOrCreateKey(int eType, Delegate action, object tag)
            {
                var signature = _CreateSignature(eType, action, tag);
                if (_signToKey.TryGetValue(signature, out _))
                {
                    Log.Error($"浜嬩欢{action.MethodName()}閲嶅娉ㄥ唽锛岃妫€鏌ヤ笟鍔￠€昏緫鏄惁姝ｇ‘銆?");
                    return 0;
                }

                long key = IDGenerater.GenerateId();
                _signToKey[signature] = key;
                _keyToSign[key] = signature;
                return key;
            }

            private long _GetOrCreateKey(int eType, FastMethodInfo action)
            {
                if (!action.IsValid) return 0;
                return _GetOrCreateKey(eType, action.Method, action.Target);
            }

            private bool TryGetKey(int eType, Delegate action, object tag, out long key)
            {
                return _signToKey.TryGetValue(_CreateSignature(eType, action, tag), out key);
            }

            private bool TryGetKey(int eType, FastMethodInfo action, out long key)
            {
                key = 0;
                return action.IsValid && TryGetKey(eType, action.Method, action.Target, out key);
            }

            private void RemovePendingAdds(Predicate<IEvent> match)
            {
                if (_waitAdds.Count == 0) return;

                _tempKeys.Clear();
                foreach (var kv in _waitAdds)
                {
                    if (match(kv.Value))
                    {
                        _tempKeys.Add(kv.Key);
                    }
                }

                foreach (var key in _tempKeys)
                {
                    _waitAdds.Remove(key);
                }

                _tempKeys.Clear();
            }

            private bool TryPrepareDispatchKeys(int eType, out List<long> keys)
            {
                keys = null;
                if (!_eventTypeKeys.TryGetValue(eType, out var typeKeys) || typeKeys.Count == 0)
                {
                    return false;
                }

                _dispatchKeys.Clear();
                foreach (var key in typeKeys)
                {
                    _dispatchKeys.Add(key);
                }

                keys = _dispatchKeys;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool UseRefDispatch<A>()
            {
                return RuntimeHelpers.IsReferenceOrContainsReferences<A>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool UseRefDispatch<A, B>()
            {
                return RuntimeHelpers.IsReferenceOrContainsReferences<A>() &&
                       RuntimeHelpers.IsReferenceOrContainsReferences<B>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool UseRefDispatch<A, B, C>()
            {
                return RuntimeHelpers.IsReferenceOrContainsReferences<A>() &&
                       RuntimeHelpers.IsReferenceOrContainsReferences<B>() &&
                       RuntimeHelpers.IsReferenceOrContainsReferences<C>();
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
                            if (_actionKeys.TryGetValue(evt.Method, out var aKeys))
                            {
                                aKeys.Remove(key);
                                if (aKeys.Count == 0) _actionKeys.Remove(evt.Method);
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
                    foreach (var evt in _waitAdds.Values)
                    {
                        _keyEvent.Add(evt.Key, evt);
                        var eType = evt.EType;
                        if (!_eventTypeKeys.TryGetValue(eType, out var typeKeys))
                        {
                            typeKeys = new HashSet<long>();
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
                            if (!_actionKeys.TryGetValue(evt.Method, out var aKeys))
                            {
                                aKeys = new HashSet<long>();
                                _actionKeys.Add(evt.Method, aKeys);
                            }

                            aKeys.Add(key);
                        }

                        var target = evt.Tag;
                        if (target != null)
                        {
                            if (!_tagKeys.TryGetValue(target, out var tKeys))
                            {
                                tKeys = new HashSet<long>();
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
                if (_waitAdds.TryGetValue(key, out var evtData)) return (T)evtData;

                evtData = Pool.Get<T>();
                _waitAdds[key] = evtData;
                return (T)evtData;
            }

            private T _Add<T>(out long key, int eType, object tag, Delegate action) where T : IEvent
            {
                key = _GetOrCreateKey(eType, action, tag);
                return _Add<T>(key);
            }

            private EventFastMethodData _Add(out long key, int eType, FastMethodInfo action)
            {
                key = _GetOrCreateKey(eType, action);
                return _Add<EventFastMethodData>(key);
            }

            private void _Remove(int eType, object tag, Delegate action)
            {
                if (TryGetKey(eType, action, tag, out var key))
                {
                    RemoveByKey(key);
                }
            }

            private void _Remove(Delegate action)
            {
                RemovePendingAdds(evt => evt.Method == action);

                if (!_actionKeys.TryGetValue(action, out var keys)) return;
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
                        if (_waitAdds.TryGetValue(key, out _))
                        {
                            _waitAdds.Remove(key);
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
                IEventExe exe;
                if (UseRefDispatch<A>())
                {
                    var refExe = Pool.Get<EventExe1>();
                    refExe.Init(eType, a);
                    exe = refExe;
                }
                else
                {
                    var typedExe = Pool.Get<TypedEventExe<A>>();
                    typedExe.Init(eType, a);
                    exe = typedExe;
                }

                _waitExes.Enqueue(exe);
                _lastExe = exe;
                return this;
            }

            public EventSystem Run<A, B>(int eType, A a, B b)
            {
                IEventExe exe;
                if (UseRefDispatch<A, B>())
                {
                    var refExe = Pool.Get<EventExe2>();
                    refExe.Init(eType, a, b);
                    exe = refExe;
                }
                else
                {
                    var typedExe = Pool.Get<TypedEventExe<A, B>>();
                    typedExe.Init(eType, a, b);
                    exe = typedExe;
                }

                _waitExes.Enqueue(exe);
                _lastExe = exe;
                return this;
            }

            public EventSystem Run<A, B, C>(int eType, A a, B b, C c)
            {
                IEventExe exe;
                if (UseRefDispatch<A, B, C>())
                {
                    var refExe = Pool.Get<EventExe3>();
                    refExe.Init(eType, a, b, c);
                    exe = refExe;
                }
                else
                {
                    var typedExe = Pool.Get<TypedEventExe<A, B, C>>();
                    typedExe.Init(eType, a, b, c);
                    exe = typedExe;
                }

                _waitExes.Enqueue(exe);
                _lastExe = exe;
                return this;
            }

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
