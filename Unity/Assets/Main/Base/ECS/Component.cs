using System;
using System.Collections.Generic;

namespace Ux
{
    public partial class Entity
    {
        readonly Dictionary<Type, Entity> _components = new Dictionary<Type, Entity>();
        public bool IsComponent => ID == -1;

        #region Component

        Entity CreateComponent(Type type, bool isFromPool)
        {
            var component = (isFromPool ? Pool.Get(type) : Activator.CreateInstance(type)) as Entity;
            if (component == null) return null;
            component.IsFromPool = isFromPool;
            component.IsDestroy = false;
            component.isDestroying = false;
            component.ID = -1;

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
            if (TryComponent(type, out var component))
            {
                return component;
            }

            component = CreateComponent(type, isFromPool);
            if (!_AddComponent(component))
            {
                return null;
            }

            component._InitSystem();
            return component;
        }

        public Entity AddComponent<A>(Type type, A a, bool isFromPool = true)
        {
            if (TryComponent(type, out var component))
            {
                return component;
            }

            component = CreateComponent(type, isFromPool);
            if (!_AddComponent(component))
            {
                return null;
            }

            component._InitSystem(a);
            return component;
        }

        public Entity AddComponent<A, B>(Type type, A a, B b, bool isFromPool = true)
        {
            if (TryComponent(type, out var component))
            {
                return component;
            }

            component = CreateComponent(type, isFromPool);
            if (!_AddComponent(component))
            {
                return null;
            }

            component._InitSystem(a, b);
            return component;
        }

        public Entity AddComponent<A, B, C>(Type type, A a, B b, C c, bool isFromPool = true)
        {
            if (TryComponent(type, out var component))
            {
                return component;
            }

            component = CreateComponent(type, isFromPool);
            if (!_AddComponent(component))
            {
                return null;
            }

            component._InitSystem(a, b, c);
            return component;
        }

        public Entity AddComponent<A, B, C, D>(Type type, A a, B b, C c, D d, bool isFromPool = true)
        {
            if (TryComponent(type, out var component))
            {
                return component;
            }

            component = CreateComponent(type, isFromPool);
            if (!_AddComponent(component))
            {
                return null;
            }

            component._InitSystem(a, b, c, d);
            return component;
        }

        public Entity AddComponent<A, B, C, D, E>(Type type, A a, B b, C c, D d, E e, bool isFromPool = true)
        {
            if (TryComponent(type, out var component))
            {
                return component;
            }

            component = CreateComponent(type, isFromPool);
            if (!_AddComponent(component))
            {
                return null;
            }

            component._InitSystem(a, b, c, d, e);
            return component;
        }

        bool TryComponent(Type type, out Entity component)
        {
            if (CheckDestroy())
            {
                component = null;
                return true;
            }

            if (_components.TryGetValue(type, out component))
            {
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

            if (component.IsDestroy || component.isDestroying)
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

            component._parent?.RemoveComponent(component, false);
            component._parent = this;
            _components.Add(component.GetType(), component);
            return true;
        }

        public bool RemoveComponent(Entity component, bool isDestroy = true)
        {
            return RemoveComponent(component.GetType(), isDestroy);
        }

        public bool RemoveComponent<T>(bool isDestroy = true) where T : Entity
        {
            return RemoveComponent(typeof(T), isDestroy);
        }

        public bool RemoveComponent(Type type, bool isDestroy = true)
        {
            if (CheckDestroy())
            {
                return false;
            }

            if (_components.TryGetValue(type, out var component))
            {
                component._Destroy(isDestroy);
                _components.Remove(type);
                return true;
            }

            return false;
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
            var getComponentSystem = (IGetComponentSystem)component;
            getComponentSystem?.OnGetComponent();

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

            if (_parent == null) return null;
            component = _parent.GetComponentInParent<T>();
            return component;
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

            if (_parent != null)
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

            if (_parent != null)
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

            if (_parent != null)
            {
                _parent.GetComponentsInParent(components);
            }
        }

        #endregion
    }
}