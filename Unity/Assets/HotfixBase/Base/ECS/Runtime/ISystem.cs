using System;
using System.Collections.Generic;

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

    #endregion

    #region 事件

    public partial class Entity
    {
        /// <summary>父实体上的子组件通知表：精确类型 → 多播委托。</summary>
        private Dictionary<Type, Action<Entity>> _childAdded;
        private Dictionary<Type, Action<Entity>> _childRemoved;

        /// <summary>本实体订阅过的项，实体移除时按记录自动注销，不需要单独的标记接口。</summary>
        private List<ChildListenerReg> _childListeners;

        private readonly struct ChildListenerReg
        {
            public readonly Entity Parent;
            public readonly bool IsAdd;
            public readonly Type ListenType;
            public readonly Action<Entity> Handler;

            public ChildListenerReg(Entity parent, bool isAdd, Type listenType, Action<Entity> handler)
            {
                Parent = parent;
                IsAdd = isAdd;
                ListenType = listenType;
                Handler = handler;
            }
        }

        /// <summary>
        /// 订阅「同一父实体下添加了 T 类型的兄弟组件」。只响应订阅之后发生的添加；
        /// 「已经存在」的情况由业务自己判断（存在就直接走逻辑，不存在再订阅）：
        /// <code>
        /// var item = Parent.Get&lt;ItemComponent&gt;();
        /// if (item != null) OnItemAdded(item);
        /// else Parent.OnListenAddEntity&lt;ItemComponent&gt;(OnItemAdded);
        /// </code>
        /// 实体被移除/销毁时自动注销，无需手动反订阅。
        /// </summary>
        public void OnListenAddEntity<T>(Action<T> handler) where T : Entity
        {
            _SubscribeChild(true, handler);
        }

        /// <summary>订阅「同一父实体下移除了 T 类型的兄弟组件」。</summary>
        public void OnListenRemoveEntity<T>(Action<T> handler) where T : Entity
        {
            _SubscribeChild(false, handler);
        }

        private void _SubscribeChild<T>(bool isAdd, Action<T> handler) where T : Entity
        {
            if (handler == null) return;

            var parent = Parent;
            if (parent == null)
            {
                Log.Error("{0} 订阅子组件通知失败：还没有父实体", GetType().Name);
                return;
            }

            Action<Entity> wrapper = e => handler((T)e);
            var map = isAdd ? parent._childAdded : parent._childRemoved;
            if (map == null)
            {
                map = new Dictionary<Type, Action<Entity>>();
                if (isAdd) parent._childAdded = map;
                else parent._childRemoved = map;
            }

            var listenType = typeof(T);
            map[listenType] = map.TryGetValue(listenType, out var exist) ? exist + wrapper : wrapper;

            (_childListeners ??= new List<ChildListenerReg>()).Add(
                new ChildListenerReg(parent, isAdd, listenType, wrapper));
        }

        /// <summary>通知本实体下的监听者：某个子组件被添加。</summary>
        internal void _NotifyChildAdded(Entity child)
        {
            if (_childAdded == null || !_childAdded.TryGetValue(child.GetType(), out var handler)) return;
            handler?.Invoke(child);
        }

        /// <summary>通知本实体下的监听者：某个子组件即将被移除。</summary>
        internal void _NotifyChildRemoved(Entity child)
        {
            if (_childRemoved == null || !_childRemoved.TryGetValue(child.GetType(), out var handler)) return;
            handler?.Invoke(child);
        }

        /// <summary>把自己挂在父实体上的订阅全部摘掉。</summary>
        private void _ClearChildListeners()
        {
            if (_childListeners == null || _childListeners.Count == 0) return;

            for (int i = 0; i < _childListeners.Count; i++)
            {
                var reg = _childListeners[i];
                var parent = reg.Parent;
                if (parent == null) continue;

                var map = reg.IsAdd ? parent._childAdded : parent._childRemoved;
                if (map == null || !map.TryGetValue(reg.ListenType, out var exist)) continue;

                var rest = exist - reg.Handler;
                if (rest == null) map.Remove(reg.ListenType);
                else map[reg.ListenType] = rest;
            }
            _childListeners.Clear();
        }

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
                EntityScheduler.Add(updateSystem);
            }

            if (this is ILateUpdateSystem lateUpdateSystem)
            {
                EntityScheduler.Add(lateUpdateSystem);
            }

            if (this is IFixedUpdateSystem fixedUpdateSystem)
            {
                EntityScheduler.Add(fixedUpdateSystem);
            }

            if (this is IApplicationQuitSystem applicationQuit)
            {
                EntityScheduler.Add(applicationQuit);
            }
            Parent?._NotifyChildAdded(this);


            if (!_is_init)
            {
                _is_init = true;
            }
        }

        void _RemoveSystem()
        {
            // 放在最前面：编辑器非 Play 模式下方法会提前 return，订阅也要能清掉。
            _ClearChildListeners();

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
                EntityScheduler.Remove(updateSystem);
            }

            if (this is ILateUpdateSystem lateUpdateSystem)
            {
                EntityScheduler.Remove(lateUpdateSystem);
            }

            if (this is IFixedUpdateSystem fixedUpdateSystem)
            {
                EntityScheduler.Remove(fixedUpdateSystem);
            }

            if (this is IApplicationQuitSystem applicationQuit)
            {
                EntityScheduler.Remove(applicationQuit);
            }


            var parent = Parent;
            if (parent != null && !parent.IsDestroy)
            {
                parent._NotifyChildRemoved(this);
            }
        }
    }

    #endregion
}