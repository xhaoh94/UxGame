using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ux
{
    #region 接口

    public interface IAwakeSystem
    {
        void OnAwake();
    }

    public interface IAwakeSystem<A>
    {
        void OnAwake(A a);
    }

    public interface IAwakeSystem<A, B>
    {
        void OnAwake(A a, B b);
    }

    public interface IAwakeSystem<A, B, C>
    {
        void OnAwake(A a, B b, C c);
    }

    public interface IAwakeSystem<A, B, C, D>
    {
        void OnAwake(A a, B b, C c, D d);
    }

    public interface IAwakeSystem<A, B, C, D, E>
    {
        void OnAwake(A a, B b, C c, D d, E e);
    }

    public interface IGetComponentSystem
    {
        void OnGetComponent();
    }

    public interface IAddComponentSystem
    {
        void OnAddComponent();
    }

    public interface IRemoveComponentSystem
    {
        void OnRemoveComponent();
    }

    public interface IUpdateSystem
    {
        void OnUpdate();
    }

    public interface ILateUpdateSystem
    {
        void OnLateUpdate();
    }

    public interface IFixedUpdateSystem
    {
        void OnFixedUpdate();
    }

    public interface IApplicationQuitSystem
    {
        void OnApplicationQuit();
    }

    public interface IEventSystem
    {
    }

    /// <summary>
    /// 监听添加或移除了某个组件或实体 只对同个父类实体且同层级的生效
    /// </summary>
    public interface IListenSystem
    {
    }

    #endregion

    #region 事件

    public partial class Entity
    {
        public struct FastMethodRef
        {
            Type _type;
            FastMethodInfo _methodInfo;
            public FastMethodRef(Type type, FastMethodInfo methodInfo)
            {
                _type = type;
                _methodInfo = methodInfo;
            }
            public void On(Dictionary<Type, List<FastMethodInfo>> dic)
            {
                if (!dic.TryGetValue(_type, out var list))
                {
                    list = new List<FastMethodInfo>();
                    dic.Add(_type, list);
                }

                list.Add(_methodInfo);
            }
        }
        public struct FastMethodRefList
        {
            List<FastMethodRef> _refAddList;
            List<FastMethodRef> _refRemoveList;
            public FastMethodRefList(List<FastMethodRef> refAddList, List<FastMethodRef> refRemoveList)
            {
                _refAddList = refAddList;
                _refRemoveList = refRemoveList;
            }
            public void On(Event evt)
            {
                if (_refAddList != null && _refAddList.Count > 0)
                {
                    foreach (var refList in _refAddList)
                    {
                        refList.On(evt._eventAdd);
                    }
                }
                if (_refRemoveList != null && _refRemoveList.Count > 0)
                {
                    foreach (var refList in _refRemoveList)
                    {
                        refList.On(evt._eventRemove);
                    }
                }
            }
        }

        bool _isCheckFastMethod;
        FastMethodRefList _fastMethodRefList;
        FastMethodRefList _GetFastMethodRefList()
        {
            if (!_isCheckFastMethod)
            {
                _isCheckFastMethod = true;
                var methods = GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                                                      BindingFlags.Public | BindingFlags.NonPublic);
                List<FastMethodRef> addList = null;
                List<FastMethodRef> removeList = null;
                foreach (var method in methods)
                {
                    var addAttrs = method.GetCustomAttributes(typeof(ListenAddEntityAttribute)).ToArray();
                    if (addAttrs.Length > 0)
                    {
                        if (addAttrs.ElementAt(0) is not ListenAddEntityAttribute evtAttr) continue;
                        var fastMethod = new FastMethodInfo(this, method);
                        addList ??= new List<FastMethodRef>();
                        addList.Add(new FastMethodRef(evtAttr.ListenType, fastMethod));
                    }

                    var removeAttrs = method.GetCustomAttributes(typeof(ListenRemoveEntityAttribute)).ToArray();
                    if (removeAttrs.Length > 0)
                    {
                        if (removeAttrs.ElementAt(0) is not ListenRemoveEntityAttribute evtAttr) continue;
                        var fastMethod = new FastMethodInfo(this, method);
                        removeList ??= new List<FastMethodRef>();
                        removeList.Add(new FastMethodRef(evtAttr.ListenType, fastMethod));
                    }
                }
                _fastMethodRefList = new FastMethodRefList(addList, removeList);
            }
            return _fastMethodRefList;
        }
        
        public class Event
        {
            public readonly Dictionary<Type, List<FastMethodInfo>> _eventAdd = new();

            public readonly Dictionary<Type, List<FastMethodInfo>> _eventRemove = new();

            public void AddSystem(Entity entity)
            {
                var key = entity.GetType();
                if (!_eventAdd.TryGetValue(key, out var list))
                {
                    return;
                }

                foreach (var method in list)
                {
                    method.Invoke(entity);
                }
            }

            public void RemoveSystem(Entity entity)
            {
                var key = entity.GetType();
                if (!_eventRemove.TryGetValue(key, out var list))
                {
                    return;
                }

                foreach (var method in list)
                {
                    method.Invoke(entity);
                }
            }
            List<Type> _waitDel = new List<Type>();
            public void Off(Entity entity)
            {
                _waitDel.Clear();
                foreach (var kv in _eventAdd)
                {
                    var listData = kv.Value;
                    for (int i = listData.Count - 1; i >= 0; i--)
                    {
                        if (listData[i].Target == entity)
                        {
                            listData.RemoveAt(i);
                        }
                    }

                    if (listData.Count == 0)
                    {
                        _waitDel.Add(kv.Key);
                    }
                }
                foreach (var key in _waitDel)
                {
                    _eventAdd.Remove(key);
                }

                _waitDel.Clear();

                foreach (var kv in _eventRemove)
                {
                    var listData = kv.Value;
                    for (int i = listData.Count - 1; i >= 0; i--)
                    {
                        if (listData[i].Target == entity)
                        {
                            listData.RemoveAt(i);
                        }
                    }

                    if (listData.Count == 0)
                    {
                        _waitDel.Add(kv.Key);
                    }
                }
                foreach (var key in _waitDel)
                {
                    _eventAdd.Remove(key);
                }
            }
            public void On(Entity entity)
            {
                entity._GetFastMethodRefList().On(this);                
            }

            public void Clear()
            {
                _eventAdd.Clear();
                _eventRemove.Clear();
            }
        }

        Event _event;

        bool _is_init;

        void _InitSystem()
        {
            if (!_is_init && this is IAwakeSystem awakeSystem)
            {
                awakeSystem.OnAwake();
            }

            _AddSystem();
        }

        void _InitSystem<A>(A a)
        {
            if (!_is_init && this is IAwakeSystem<A> awakeSystem)
            {
                awakeSystem.OnAwake(a);
            }

            _AddSystem();
        }

        void _InitSystem<A, B>(A a, B b)
        {
            if (!_is_init && this is IAwakeSystem<A, B> awakeSystem)
            {
                awakeSystem.OnAwake(a, b);
            }

            _AddSystem();
        }

        void _InitSystem<A, B, C>(A a, B b, C c)
        {
            if (!_is_init && this is IAwakeSystem<A, B, C> awakeSystem)
            {
                awakeSystem.OnAwake(a, b, c);
            }

            _AddSystem();
        }

        void _InitSystem<A, B, C, D>(A a, B b, C c, D d)
        {
            if (!_is_init && this is IAwakeSystem<A, B, C, D> awakeSystem)
            {
                awakeSystem.OnAwake(a, b, c, d);
            }

            _AddSystem();
        }

        void _InitSystem<A, B, C, D, E>(A a, B b, C c, D d, E e)
        {
            if (!_is_init && this is IAwakeSystem<A, B, C, D, E> awakeSystem)
            {
                awakeSystem.OnAwake(a, b, c, d, e);
            }

            _AddSystem();
        }

        void _AddSystem()
        {
            if (this is IAddComponentSystem addConponentSystem)
            {
                addConponentSystem.OnAddComponent();
            }
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                return;
            }
#endif
            if (this is IUpdateSystem updateSystem)
            {
                GameMethod.Update += updateSystem.OnUpdate;
            }

            if (this is ILateUpdateSystem lateUpdateSystem)
            {
                GameMethod.LateUpdate += lateUpdateSystem.OnLateUpdate;
            }

            if (this is IFixedUpdateSystem fixedUpdateSystem)
            {
                GameMethod.FixedUpdate += fixedUpdateSystem.OnFixedUpdate;
            }

            if (this is IApplicationQuitSystem applicationQuit)
            {
                GameMethod.Quit += applicationQuit.OnApplicationQuit;
            }
            var temPar = Parent;
            if (temPar != null)
            {
                temPar._event?.AddSystem(this);
                if (this is IListenSystem)
                {
                    temPar._event ??= Pool.Get<Event>();
                    temPar._event.On(this);
                }
            }


            if (!_is_init)
            {
                if (this is IEventSystem)
                {
                    EventMgr.Ins.RegisterFastMethod(this);
                }

                _is_init = true;
            }
        }

        void _RemoveSystem()
        {
            if (this is IRemoveComponentSystem removeComponentSystem)
            {
                removeComponentSystem.OnRemoveComponent();
            }

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                return;
            }
#endif
            if (this is IUpdateSystem updateSystem)
            {
                GameMethod.Update -= updateSystem.OnUpdate;
            }

            if (this is ILateUpdateSystem lateUpdateSystem)
            {
                GameMethod.LateUpdate -= lateUpdateSystem.OnLateUpdate;
            }

            if (this is IFixedUpdateSystem fixedUpdateSystem)
            {
                GameMethod.FixedUpdate -= fixedUpdateSystem.OnFixedUpdate;
            }

            if (this is IApplicationQuitSystem applicationQuit)
            {
                GameMethod.Quit -= applicationQuit.OnApplicationQuit;
            }


            var temPar = Parent;

            if (temPar != null && !temPar.IsDestroy && temPar._event != null)
            {
                if (this is IListenSystem)
                {
                    temPar._event.Off(this);
                }
                temPar._event.RemoveSystem(this);
            }

        }
    }

    #endregion
}