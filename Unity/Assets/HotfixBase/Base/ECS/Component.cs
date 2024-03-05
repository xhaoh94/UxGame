using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ux
{
    public partial class Entity
    {
        readonly Dictionary<Type, Entity> _components = new Dictionary<Type, Entity>();
        public bool IsComponent => ID == -1;

        #region Component

        Entity _CreateComponent(Type type, bool isFromPool)
        {
            var component = (isFromPool ? Pool.Get(type) : Activator.CreateInstance(type)) as Entity;
            if (component == null) return null;
#if UNITY_EDITOR
            component.Viewer = EntityViewer.Create();
#endif
            component.IsFromPool = isFromPool;
            component._isDestroyed = false;
            component._isDestroying = false;
            component.ID = -1;
            component.Name = $"{type.Name}";
            return component;
        }

        public Entity AddComponent(Entity component)
        {
            return !_AddComponent(component) ? null : component;
        }

        public T AddComponent<T>(bool isFromPool = true) where T : Entity
        {
            return (T)AddComponent(typeof(T), isFromPool);
        }

        public T AddComponent<T, A>(A a, bool isFromPool = true) where T : Entity
        {
            return (T)AddComponent(typeof(T), a, isFromPool);
        }

        public T AddComponent<T, A, B>(A a, B b, bool isFromPool = true) where T : Entity
        {
            return (T)AddComponent(typeof(T), a, b, isFromPool);
        }

        public T AddComponent<T, A, B, C>(A a, B b, C c, bool isFromPool = true) where T : Entity
        {
            return (T)AddComponent(typeof(T), a, b, c, isFromPool);
        }

        public T AddComponent<T, A, B, C, D>(A a, B b, C c, D d, bool isFromPool = true) where T : Entity
        {
            return (T)AddComponent(typeof(T), a, b, c, d, isFromPool);
        }

        public T AddComponent<T, A, B, C, D, E>(A a, B b, C c, D d, E e, bool isFromPool = true) where T : Entity
        {
            return (T)AddComponent(typeof(T), a, b, c, d, e, isFromPool);
        }


        public Entity AddComponent(Type type, bool isFromPool = true)
        {
            if (_TryGetOrAddComponent(type, isFromPool, out var component))
            {
                return component;
            }
            component?._InitSystem();
            return component;
        }

        public Entity AddComponent<A>(Type type, A a, bool isFromPool = true)
        {
            if (_TryGetOrAddComponent(type, isFromPool, out var component))
            {
                return component;
            }

            component?._InitSystem(a);
            return component;
        }

        public Entity AddComponent<A, B>(Type type, A a, B b, bool isFromPool = true)
        {
            if (_TryGetOrAddComponent(type, isFromPool, out var component))
            {
                return component;
            }
            component?._InitSystem(a, b);
            return component;
        }

        public Entity AddComponent<A, B, C>(Type type, A a, B b, C c, bool isFromPool = true)
        {
            if (_TryGetOrAddComponent(type, isFromPool, out var component))
            {
                return component;
            }
            component?._InitSystem(a, b, c);
            return component;
        }

        public Entity AddComponent<A, B, C, D>(Type type, A a, B b, C c, D d, bool isFromPool = true)
        {
            if (_TryGetOrAddComponent(type, isFromPool, out var component))
            {
                return component;
            }
            component?._InitSystem(a, b, c, d);
            return component;
        }

        public Entity AddComponent<A, B, C, D, E>(Type type, A a, B b, C c, D d, E e, bool isFromPool = true)
        {
            if (_TryGetOrAddComponent(type, isFromPool, out var component))
            {
                return component;
            }

            component?._InitSystem(a, b, c, d, e);
            return component;
        }

        bool _TryGetOrAddComponent(Type type, bool isFromPool, out Entity component)
        {
            if (CheckDestroy())
            {
                component = null;
                return true;
            }

            if (_components.TryGetValue(type, out component))
            {
                Log.Warning("重复添加组件");
                return true;
            }

            component = _CreateComponent(type, isFromPool);
            if (!_AddComponent(component))
            {
                component = null;
                return true;
            }
            return false;
        }

        bool _AddComponent(Entity component)
        {
            if (CheckDestroy())
            {
                return false;
            }

            if (component == null)
            {
                Log.Error("Component为空");
                return false;
            }

            if (component.IsDestroy)
            {
                Log.Error("添加已销毁的组件");
                return false;
            }

            if (!component.IsComponent)
            {
                Log.Error("AddComponent 不能添加非组件类型");
                return false;
            }

            if (component == this)
            {
                Log.Error("Component不可添加自己");
                return false;
            }

            if (component._parent == this)
            {
                return true;
            }

            component._parent?.RemoveComponent(component.GetType(), false);
            component._parent = this;
            _components.Add(component.GetType(), component);

#if UNITY_EDITOR            
            component.Viewer.transform.SetParent(component.Parent?.Viewer.ComponentContent);
#endif

            return true;
        }

        public bool RemoveComponent(Entity component)
        {
            return RemoveComponent(component.GetType());
        }

        public bool RemoveComponent<T>() where T : Entity
        {
            return RemoveComponent(typeof(T));
        }

        public bool RemoveComponent(Type type)
        {
            return RemoveComponent(type, true);
        }

        bool RemoveComponent(Type type, bool isDestroy)
        {
            if (_components.TryGetValue(type, out var component))
            {
                component._Destroy(isDestroy);
                _components.Remove(type);
                return true;
            }

            return false;
        }

        void RemoveComponents()
        {
            while (_components.Count > 0)
            {
                RemoveComponent(_components.ElementAt(0).Value);
            }
        }

        public T GetOrAddComponent<T>(bool isFromPool = true) where T : Entity
        {
            return (T)GetOrAddComponent(typeof(T), isFromPool);
        }

        public Entity GetOrAddComponent(Type type, bool isFromPool = true)
        {
            if (_components.TryGetValue(type, out var component))
            {
                return component;
            }

            return AddComponent(type, isFromPool);
        }

        public Entity GetComponent(Type type)
        {
            if (CheckDestroy())
            {
                return null;
            }

            if (!_components.TryGetValue(type, out var component)) return null;
            if (component is IGetComponentSystem getComponentSystem)
            {
                getComponentSystem?.OnGetComponent();
            }
            return component;
        }

        public T GetComponent<T>() where T : Entity
        {
            return (T)GetComponent(typeof(T));
        }

        public T GetComponentInChildren<T>() where T : Entity
        {
            if (CheckDestroy())
            {
                return null;
            }

            var component = GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            foreach (var entity in _entitys)
            {
                if (entity.Value.IsDestroy) continue;
                component = entity.Value.GetComponentInChildren<T>();
                if (component != null) return component;
            }

            return null;
        }

        public Entity GetComponentInChildren(Type type)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var component = GetComponent(type);
            if (component != null)
            {
                return component;
            }

            foreach (var entity in _entitys)
            {
                if (entity.Value.IsDestroy) continue;
                component = entity.Value.GetComponentInChildren(type);
                if (component != null) return component;
            }

            return null;
        }

        public List<T> GetComponentsInChildren<T>() where T : Entity
        {
            List<T> components = new List<T>();
            GetComponentsInChildren(components);
            return components;
        }

        public List<Entity> GetComponentsInChildren(Type type)
        {
            List<Entity> components = new List<Entity>();
            GetComponentsInChildren(components, type);
            return components;
        }

        void GetComponentsInChildren(List<Entity> components, Type type)
        {
            if (CheckDestroy())
            {
                return;
            }

            components ??= new List<Entity>();
            var component = GetComponent(type);
            if (component != null)
            {
                components.Add(component);
            }

            foreach (var entity in _entitys)
            {
                if (entity.Value.IsDestroy) continue;
                entity.Value.GetComponentsInChildren(components, type);
            }
        }

        void GetComponentsInChildren<T>(List<T> components) where T : Entity
        {
            if (CheckDestroy())
            {
                return;
            }

            components ??= new List<T>();
            var component = GetComponent<T>();
            if (component != null)
            {
                components.Add(component);
            }

            foreach (var entity in _entitys)
            {
                if (entity.Value.IsDestroy) continue;
                entity.Value.GetComponentsInChildren(components);
            }
        }

        public T GetComponentInParent<T>() where T : Entity
        {
            if (CheckDestroy())
            {
                return default;
            }

            var component = GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            if (_parent != null && !_parent.IsDestroy)
            {
                component = _parent.GetComponentInParent<T>();
                if (component != null) return component;
            }
            return null;
        }

        public Entity GetComponentInParent(Type type)
        {
            if (CheckDestroy())
            {
                return default;
            }

            var component = GetComponent(type);
            if (component != null)
            {
                return component;
            }

            if (_parent != null && !_parent.IsDestroy)
            {
                component = _parent.GetComponentInParent(type);
                if (component != null) return component;
            }

            return null;
        }

        public List<T> GetComponentsInParent<T>() where T : Entity
        {
            List<T> components = new List<T>();
            GetComponentsInParent(components);
            return components;
        }

        public List<Entity> GetComponentsInParent(Type type)
        {
            List<Entity> components = new List<Entity>();
            GetComponentsInParent(components, type);
            return components;
        }

        void GetComponentsInParent(List<Entity> components, Type type)
        {
            if (CheckDestroy())
            {
                return;
            }

            components ??= new List<Entity>();
            var component = GetComponent(type);
            if (component != null)
            {
                components.Add(component);
            }

            if (_parent != null && !_parent.IsDestroy)
            {
                _parent.GetComponentsInParent(components, type);
            }
        }

        void GetComponentsInParent<T>(List<T> components) where T : Entity
        {
            if (CheckDestroy())
            {
                return;
            }

            components ??= new List<T>();
            var component = GetComponent<T>();
            if (component != null)
            {
                components.Add(component);
            }

            if (_parent != null && !_parent.IsDestroy)
            {
                _parent.GetComponentsInParent(components);
            }
        }

        #endregion
    }
}