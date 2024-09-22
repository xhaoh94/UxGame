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
        class Event
        {
            readonly Dictionary<Type, List<FastMethodInfo>>
                _eventAdd = new Dictionary<Type, List<FastMethodInfo>>();

            readonly Dictionary<Type, List<FastMethodInfo>> _eventRemove =
                new Dictionary<Type, List<FastMethodInfo>>();

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
            public void On(Entity target)
            {
                var methods = target.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                                          BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var method in methods)
                {
                    var addAttrs = method.GetCustomAttributes(typeof(ListenAddEntityAttribute)).ToArray();
                    if (addAttrs.Length > 0)
                    {
                        var fastMethod = new FastMethodInfo(target, method);
                        if (addAttrs.ElementAt(0) is not ListenAddEntityAttribute evtAttr) continue;
                        if (!_eventAdd.TryGetValue(evtAttr.ListenType, out var list))
                        {
                            list = new List<FastMethodInfo>();
                            _eventAdd.Add(evtAttr.ListenType, list);
                        }

                        list.Add(fastMethod);
                    }

                    var removeAttrs = method.GetCustomAttributes(typeof(ListenRemoveEntityAttribute)).ToArray();
                    if (removeAttrs.Length > 0)
                    {
                        var fastMethod = new FastMethodInfo(target, method);
                        if (removeAttrs.ElementAt(0) is not ListenRemoveEntityAttribute evtAttr) continue;
                        if (!_eventRemove.TryGetValue(evtAttr.ListenType, out var list))
                        {
                            list = new List<FastMethodInfo>();
                            _eventRemove.Add(evtAttr.ListenType, list);
                        }

                        list.Add(fastMethod);
                    }
                }
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
                    EventMgr.Ins.___RegisterFastMethod(this);
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