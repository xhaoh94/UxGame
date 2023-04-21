using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
            readonly Dictionary<string, List<FastMethodInfo>>
                _eventAdd = new Dictionary<string, List<FastMethodInfo>>();

            readonly Dictionary<string, List<FastMethodInfo>> _eventRemove =
                new Dictionary<string, List<FastMethodInfo>>();

            public void Add(Entity entity)
            {
                var key = entity.GetType().FullName;
                if (string.IsNullOrEmpty(key)) return;
                if (!_eventAdd.TryGetValue(key, out var list))
                {
                    return;
                }

                foreach (var method in list)
                {
                    method.Invoke(entity);
                }
            }

            public void Remove(Entity entity)
            {
                var key = entity.GetType().FullName;
                if (string.IsNullOrEmpty(key)) return;
                if (!_eventRemove.TryGetValue(key, out var list))
                {
                    return;
                }

                foreach (var method in list)
                {
                    method.Invoke(entity);
                }
            }

            public void __Logout(Entity entity)
            {
                var keys = _eventAdd.Keys;
                foreach (var key in keys)
                {
                    var listData = _eventAdd[key];
                    for (int i = listData.Count - 1; i >= 0; i--)
                    {
                        if (listData[i].Target == entity)
                        {
                            listData.RemoveAt(i);
                        }
                    }

                    if (listData.Count == 0)
                    {
                        _eventAdd.Remove(key);
                    }
                }

                keys = _eventRemove.Keys;
                foreach (var key in keys)
                {
                    var listData = _eventRemove[key];
                    for (int i = listData.Count - 1; i >= 0; i--)
                    {
                        if (listData[i].Target == entity)
                        {
                            listData.RemoveAt(i);
                        }
                    }

                    if (listData.Count == 0)
                    {
                        _eventRemove.Remove(key);
                    }
                }
            }

            public void ___Register(Entity target)
            {
                var methods = target.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                                          BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var method in methods)
                {
                    var addAttrs = method.GetCustomAttributes(typeof(ListenAddEntityAttribute)).ToArray();
                    zstring temKey;
                    if (addAttrs.Length > 0)
                    {
                        var fastMethod = new FastMethodInfo(target, method);
                        if (addAttrs.ElementAt(0) is not ListenAddEntityAttribute evtAttr) continue;
                        using (zstring.Block())
                        {
                            temKey = evtAttr.type.FullName;
                            var key = temKey.Intern();
                            if (!_eventAdd.TryGetValue(key, out var list))
                            {
                                list = new List<FastMethodInfo>();
                                _eventAdd.Add(key, list);
                            }

                            list.Add(fastMethod);
                        }
                    }

                    var removeAttrs = method.GetCustomAttributes(typeof(ListenRemoveEntityAttribute)).ToArray();
                    if (removeAttrs.Length > 0)
                    {
                        var fastMethod = new FastMethodInfo(target, method);
                        if (removeAttrs.ElementAt(0) is not ListenRemoveEntityAttribute evtAttr) continue;
                        using (zstring.Block())
                        {
                            temKey = evtAttr.type.FullName;
                            var key = temKey.Intern();
                            if (!_eventRemove.TryGetValue(key, out var list))
                            {
                                list = new List<FastMethodInfo>();
                                _eventRemove.Add(key, list);
                            }

                            list.Add(fastMethod);
                        }
                    }
                }
            }
        }

        Event _event;

        bool __is_init;

        void _InitSystem()
        {
            if (!__is_init && this is IAwakeSystem awakeSystem)
            {
                awakeSystem.OnAwake();
            }

            _AddSystem();
        }

        void _InitSystem<A>(A a)
        {
            if (!__is_init && this is IAwakeSystem<A> awakeSystem)
            {
                awakeSystem.OnAwake(a);
            }

            _AddSystem();
        }

        void _InitSystem<A, B>(A a, B b)
        {
            if (!__is_init && this is IAwakeSystem<A, B> awakeSystem)
            {
                awakeSystem.OnAwake(a, b);
            }

            _AddSystem();
        }

        void _InitSystem<A, B, C>(A a, B b, C c)
        {
            if (!__is_init && this is IAwakeSystem<A, B, C> awakeSystem)
            {
                awakeSystem.OnAwake(a, b, c);
            }

            _AddSystem();
        }

        void _InitSystem<A, B, C, D>(A a, B b, C c, D d)
        {
            if (!__is_init && this is IAwakeSystem<A, B, C, D> awakeSystem)
            {
                awakeSystem.OnAwake(a, b, c, d);
            }

            _AddSystem();
        }

        void _InitSystem<A, B, C, D, E>(A a, B b, C c, D d, E e)
        {
            if (!__is_init && this is IAwakeSystem<A, B, C, D, E> awakeSystem)
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

            if (this is IUpdateSystem updateSystem)
            {
                TimeMgr.Instance.DoUpdate(updateSystem.OnUpdate);
            }

            if (this is ILateUpdateSystem lateUpdateSystem)
            {
                TimeMgr.Instance.DoLateUpdate(lateUpdateSystem.OnLateUpdate);
            }

            if (this is IFixedUpdateSystem fixedUpdateSystem)
            {
                TimeMgr.Instance.DoFixedUpdate(fixedUpdateSystem.OnFixedUpdate);
            }

            var temPar = Parent;
            temPar?._event?.Add(this);

            if (this is IListenSystem && temPar != null)
            {
                temPar._event ??= new Event();
                temPar._event?.___Register(this);
            }

            if (!__is_init)
            {
                if (this is IEventSystem)
                {
                    EventMgr.Instance.___RegisterFastMethod(this);
                }

                __is_init = true;
            }
        }

        void _RemoveSystem()
        {
            if (this is IUpdateSystem updateSystem)
            {
                TimeMgr.Instance.RemoveUpdate(updateSystem.OnUpdate);
            }

            if (this is ILateUpdateSystem lateUpdateSystem)
            {
                TimeMgr.Instance.RemoveLateUpdate(lateUpdateSystem.OnLateUpdate);
            }

            if (this is IRemoveComponentSystem removeComponentSystem)
            {
                removeComponentSystem.OnRemoveComponent();
            }

            var temPar = Parent;

            if (this is IListenSystem)
            {
                temPar?._event?.__Logout(this);
            }

            temPar?._event?.Remove(this);
        }
    }

    #endregion
}