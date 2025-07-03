using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ux
{
    public partial class EventMgr
    {
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
        Dictionary<long, List<FastMethodRef>> _fastMethodRefDic = new();
        public interface IEventSystem
        {
            void Init(int exeLimit);
            void Release();
        }
        public partial class EventSystem: IEventSystem
        {
            //每帧执行上限-超出上限，下一帧处理
            int _exeLimit = 200;
            int _exeCnt = 0;

            private readonly Dictionary<long, IEvent> _keyEvent = new();
            /// <summary>
            /// 事件ID对应的所有IEvent
            /// </summary>
            private readonly Dictionary<int, HashSet<long>> _eTypeKeys = new();
            /// <summary>
            /// 标签对应的所有IEvent
            /// </summary>
            private readonly Dictionary<int, HashSet<long>> _tagKeys = new();
            /// <summary>
            /// 函数对应的所有IEvent
            /// </summary>
            private readonly Dictionary<int, HashSet<long>> _actionKeys = new();

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
                _keyEvent.Clear();
                _eTypeKeys.Clear();
                _tagKeys.Clear();
                _actionKeys.Clear();
                _waitExes.Clear();
                _waitAdds.Clear();
                _waitDels.Clear();
                Pool.Push(this);
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
                var key = IDGenerater.GenerateId(target.GetHashCode(), type.GetHashCode());
                if (!Ins._fastMethodRefDic.TryGetValue(key, out var refList))
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
                            Ins._fastMethodRefDic.Add(key, refList);
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
            private long _GetKey(int eType, object action, object tag)
            {
                if (tag == null)
                {
                    return IDGenerater.GenerateId(action.GetHashCode(), eType);
                }
                else
                {
                    return IDGenerater.GenerateId(action.GetHashCode(), tag.GetHashCode(), eType);
                }
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
                    var enumerator = _waitDels.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var key = enumerator.Current;
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
                    var enumerator = _waitAdds.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var evt = enumerator.Current;
                        _keyEvent.Add(evt.Key, evt);
                        var eType = evt.EType;
                        if (!_eTypeKeys.TryGetValue(eType, out var typeKeys))
                        {
                            typeKeys = new();
                            _eTypeKeys.Add(eType, typeKeys);
                        }
                        var key = evt.Key;
                        typeKeys.Add(key);

#if UNITY_EDITOR

                        if (Ins._defaultSystem == this)
                        {
                            Ins._EditorAdd(evt);
                        }
#endif


                        var actionHashCode = evt.Method.GetHashCode();
                        if (!_actionKeys.TryGetValue(actionHashCode, out var aKeys))
                        {
                            aKeys = new();
                            _actionKeys.Add(actionHashCode, aKeys);
                        }

                        aKeys.Add(key);

                        var target = evt.Tag;
                        if (target != null)
                        {
                            int hashCode = target.GetHashCode();
                            if (!_tagKeys.TryGetValue(hashCode, out var tKeys))
                            {
                                tKeys = new();
                                _tagKeys.Add(hashCode, tKeys);
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
                    exe.Exe(this, ref _exeCnt);
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
                var hashCode = action.GetHashCode();
                if (_waitAdds.Count > 0)
                {
                    _waitAdds.RemoveAll(x => x.Method == action);
                }

                if (!_actionKeys.TryGetValue(hashCode, out var keys)) return;
                RemoveByKey(keys);
            }

            public EventSystem RemoveByKey(IEnumerable<long> keys)
            {
                using var enumerator = keys.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    RemoveByKey(enumerator.Current);
                }

                //for (int i = keys.Count - 1; i >= 0; i--)
                //{               
                //    RemoveByKey(keys[i]);
                //}
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
                _waitExes.Enqueue(new EventExe(eType));
                return this;
            }

            public EventSystem Run<A>(int eType, A a)
            {
                _waitExes.Enqueue(new EventExe<A>(eType, a));
                return this;
            }

            public EventSystem Run<A, B>(int eType, A a, B b)
            {
                _waitExes.Enqueue(new EventExe<A, B>(eType, a, b));
                return this;
            }

            public EventSystem Run<A, B, C>(int eType, A a, B b, C c)
            {
                _waitExes.Enqueue(new EventExe<A, B, C>(eType, a, b, c));
                return this;
            }
            //立即调用
            public void Immediate()
            {
                var tmp = _exeLimit;
                _exeLimit = int.MaxValue;
                _Update();
                _exeLimit = tmp;
            }
        }
    }

}
