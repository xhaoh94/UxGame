using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ux
{
    public abstract partial class Entity
    {
        static readonly Queue<Action> _delayFn = new Queue<Action>();
        static int _exeNum;
        static void _DelayInvoke(int max)
        {
            _exeNum = 0;
            while (_delayFn.Count > 0 && (_exeNum++) < max) //一帧最多执行max次
            {
                _delayFn.Dequeue()?.Invoke();
            }
        }

        public static void Update()
        {
            _DelayInvoke(200);
        }
#if UNITY_EDITOR
        public GameObject GoViewer { get; private set; }
#endif
        public EntityMono EntityMono { get; private set; }

        public long ID { get; private set; }
        /// <summary>
        /// 是否从对象池获取
        /// </summary>
        public bool IsFromPool { get; private set; }
        /// <summary>
        /// 是否已销毁
        /// </summary>
        public bool IsDestroy => _isDestroyed || _isDestroying;
        bool _isDestroying;
        bool _isDestroyed;
        readonly Dictionary<long, Entity> _entitys = new Dictionary<long, Entity>();
        readonly Dictionary<Type, List<Entity>> _typeToentitys = new Dictionary<Type, List<Entity>>();
        Entity _parent;


        /// <summary>
        /// 获取父类实体，如果父类是组件的时候，会循环往上获取，直到获取到为实体为止
        /// </summary>
        public Entity Parent
        {
            get
            {
                var temPar = _parent;
                while (temPar is { IsComponent: true })
                {
                    temPar = temPar._parent;
                }

                return temPar;
            }
            set
            {
                if (_parent == value)
                {
                    return;
                }

                if (value == null)
                {
                    _ = IsComponent ? _parent?.RemoveComponent(this) : _parent?.RemoveChild(this);
                }
                else
                {
                    _ = IsComponent ? value.AddComponent(this) : value.AddChild(this);
                }
            }
        }
        public T ParentAs<T>() where T : Entity
        {
            return Parent as T;
        }
        #region Entity
        public static TEntity Create<TEntity>(bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Create(IDGenerater.GenerateId(), typeof(TEntity), isFromPool);
        }
        public static TEntity Create<TEntity>(long id, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Create(id, typeof(TEntity), isFromPool);
        }

        public static Entity Create(long id, Type type, bool isFromPool = true)
        {
            var entity = (isFromPool ? Pool.Get(type) : Activator.CreateInstance(type)) as Entity;
            if (entity == null) return null;
            entity._isDestroyed = false;
            entity._isDestroying = false;
            entity.IsFromPool = isFromPool;
            entity.ID = id;
#if UNITY_EDITOR
            entity.GoViewer = new GameObject();
            entity.GoViewer.name = $"{type.Name}_{id}";
            var eg = entity.GoViewer.AddComponent<EntityViewer>();
            eg.SetEntity(entity);
#endif
            return entity;
        }
#if UNITY_EDITOR
        protected void SetViewerName(string name)
        {
            if (GoViewer != null)
            {
                GoViewer.name = name;
            }
        }
#endif

        public void SetMono(GameObject gameObject, bool isSetParent = true)
        {
            EntityMono = gameObject.GetOrAddComponent<EntityMono>();
#if UNITY_EDITOR
            EntityMono.SetEntity(this, GoViewer);
            if (isSetParent)
            {
                Transform assetContent = null;
                if (IsComponent)
                {
                    assetContent = Parent.GoViewer.transform.Find("Asset");
                }
                else
                {
                    assetContent = GoViewer.transform.Find("Asset");
                }

                if (assetContent == null)
                {
                    assetContent = new GameObject("Asset").transform;
                    assetContent.SetParent(GoViewer.transform);
                    assetContent.SetAsFirstSibling();
                }
                gameObject.SetParent(assetContent);
            }
#else
            EntityMono.SetEntity(this);
            if (isSetParent)
            {
                var temParent = Parent;
                if (temParent != null && temParent.EntityMono != null)
                {
                    gameObject.SetParent(temParent.EntityMono.transform);
                }
            }
#endif
        }
        bool _AddChild(Entity entity)
        {
            if (IsComponent)
            {
                var temParent = Parent;
                if (temParent == null)
                {
                    Log.Error("父实体为空，无法添加子实体");
                    return false;
                }
                return temParent._AddChild(entity);
            }
            if (CheckDestroy())
            {
                return false;
            }

            if (entity == null)
            {
                Log.Error("实体为空");
                return false;
            }

            if (entity.IsDestroy)
            {
                Log.Error("添加已销毁的组件");
                return false;
            }

            if (entity.IsComponent)
            {
                Log.Error("AddChild不能添加组件类型");
                return false;
            }

            if (entity == this)
            {
                Log.Error("不可添加自己");
                return false;
            }
            if (_entitys.ContainsKey(entity.ID))
            {
                Log.Error($"重复添加实体,ID:{entity.ID}");
                return false;
            }


            if (entity._parent == this)
            {
                return true;
            }

            entity._parent?.RemoveChild(entity, false);
            entity._parent = this;
            var entityID = entity.ID;
            _entitys.Add(entityID, entity);
            var type = entity.GetType();
            if (!_typeToentitys.TryGetValue(type, out var listData))
            {
                listData = new List<Entity>();
                _typeToentitys.Add(type, listData);
            }
            listData.Add(entity);

#if UNITY_EDITOR
            var entityContent = GoViewer.transform.Find("Entitys");
            if (entityContent == null)
            {
                entityContent = new GameObject("Entitys").transform;
                entityContent.SetParent(GoViewer.transform);
                entityContent.SetAsLastSibling();
            }
            entity.GoViewer.transform.SetParent(entityContent);
#endif
            return true;
        }

        Entity AddChild(Entity entity)
        {
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem();
            return entity;
        }

        public TEntity AddChild<TEntity>(bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(typeof(TEntity), isFromPool);
        }

        public TEntity AddChild<TEntity, A>(A a, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(typeof(TEntity), a, isFromPool);
        }

        public TEntity AddChild<TEntity, A, B>(A a, B b, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(typeof(TEntity), a, b, isFromPool);
        }

        public TEntity AddChild<TEntity, A, B, C>(A a, B b, C c, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(typeof(TEntity), a, b, c, isFromPool);
        }

        public TEntity AddChild<TEntity, A, B, C, D>(A a, B b, C c, D d, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(typeof(TEntity), a, b, c, d, isFromPool);
        }

        public TEntity AddChild<TEntity, A, B, C, D, E>(A a, B b, C c, D d, E e, bool isFromPool = true)
            where TEntity : Entity
        {
            return (TEntity)AddChild(typeof(TEntity), a, b, c, d, e, isFromPool);
        }
        public TEntity AddChild<TEntity>(long id, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(id, typeof(TEntity), isFromPool);
        }

        public TEntity AddChild<TEntity, A>(long id, A a, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(id, typeof(TEntity), a, isFromPool);
        }

        public TEntity AddChild<TEntity, A, B>(long id, A a, B b, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(id, typeof(TEntity), a, b, isFromPool);
        }

        public TEntity AddChild<TEntity, A, B, C>(long id, A a, B b, C c, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(id, typeof(TEntity), a, b, c, isFromPool);
        }

        public TEntity AddChild<TEntity, A, B, C, D>(long id, A a, B b, C c, D d, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)AddChild(id, typeof(TEntity), a, b, c, d, isFromPool);
        }

        public TEntity AddChild<TEntity, A, B, C, D, E>(long id, A a, B b, C c, D d, E e, bool isFromPool = true)
            where TEntity : Entity
        {
            return (TEntity)AddChild(id, typeof(TEntity), a, b, c, d, e, isFromPool);
        }
        public Entity AddChild(long id, Type type, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem();
            return entity;
        }

        public Entity AddChild<A>(long id, Type type, A a, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a);
            return entity;
        }

        public Entity AddChild<A, B>(long id, Type type, A a, B b, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a, b);
            return entity;
        }

        public Entity AddChild<A, B, C>(long id, Type type, A a, B b, C c, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c);
            return entity;
        }

        public Entity AddChild<A, B, C, D>(long id, Type type, A a, B b, C c, D d, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c, d);
            return entity;
        }

        public Entity AddChild<A, B, C, D, E>(long id, Type type, A a, B b, C c, D d, E e, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c, d, e);
            return entity;
        }
        public Entity AddChild(Type type, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem();
            return entity;
        }

        public Entity AddChild<A>(Type type, A a, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a);
            return entity;
        }

        public Entity AddChild<A, B>(Type type, A a, B b, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a, b);
            return entity;
        }

        public Entity AddChild<A, B, C>(Type type, A a, B b, C c, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c);
            return entity;
        }

        public Entity AddChild<A, B, C, D>(Type type, A a, B b, C c, D d, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c, d);
            return entity;
        }

        public Entity AddChild<A, B, C, D, E>(Type type, A a, B b, C c, D d, E e, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_AddChild(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c, d, e);
            return entity;
        }

        public bool RemoveChild(Entity entity)
        {
            return RemoveChild(entity, true);
        }
        bool RemoveChild(Entity entity, bool isDestroy)
        {
            if (CheckDestroy())
            {
                return false;
            }

            if (entity == null)
            {
                Log.Error("Entity为空");
                return false;
            }

            if (_entitys.Remove(entity.ID))
            {
                var type = entity.GetType();
                if (_typeToentitys.TryGetValue(type, out var entities))
                {
                    if (entities.Remove(entity) && entities.Count == 0)
                    {
                        _typeToentitys.Remove(type);
                    }
                }

                entity._Destroy(isDestroy);
                return true;
            }

            return false;
        }
        public T GetChild<T>(long id) where T : Entity
        {
            return GetChild(id) as T;
        }
        public Entity GetChild(long id)
        {
            if (IsComponent)
            {
                return Parent?.GetChild(id);
            }
            if (_entitys.TryGetValue(id, out var entity))
            {
                return entity;
            }
            return null;
        }
        public List<Entity> GetChilds(bool isIncludeNested = false)
        {
            if (IsComponent)
            {
                return Parent?.GetChilds(isIncludeNested);
            }
            if (CheckDestroy())
            {
                return null;
            }
            var listData = new List<Entity>();
            _GetChilds(listData, isIncludeNested);
            return listData;
        }
        public List<Entity> GetChilds(Type type, bool isIncludeNested = false)
        {
            if (IsComponent)
            {
                return Parent?.GetChilds(type, isIncludeNested);
            }
            if (CheckDestroy())
            {
                return null;
            }
            var listData = new List<Entity>();
            _GetChilds(type, listData, isIncludeNested);
            return listData;
        }
        public List<T> GetChilds<T>(bool isIncludeNested = false) where T : Entity
        {
            if (IsComponent)
            {
                return Parent?.GetChilds<T>(isIncludeNested);
            }
            if (CheckDestroy())
            {
                return null;
            }
            var listData = new List<T>();
            _GetChilds(listData, isIncludeNested);
            return listData;
        }
        void _GetChilds<T>(List<T> listData, bool isIncludeNested) where T : Entity
        {
            if (_typeToentitys.TryGetValue(typeof(T), out var _temList))
            {
                foreach (var entity in _temList)
                {
                    listData.Add(entity as T);
                    if (isIncludeNested)
                    {
                        entity._GetChilds<T>(listData, isIncludeNested);
                    }
                }
            }
        }
        void _GetChilds(Type type, List<Entity> listData, bool isIncludeNested)
        {
            if (_typeToentitys.TryGetValue(type, out var _temList))
            {
                listData.AddRange(_temList);
                if (isIncludeNested)
                {
                    foreach (var entity in _temList)
                    {
                        entity._GetChilds(type, listData, isIncludeNested);
                    }
                }
            }
        }
        void _GetChilds(List<Entity> listData, bool isIncludeNested)
        {
            foreach (var entityKv in _typeToentitys)
            {
                listData.AddRange(entityKv.Value);
                if (isIncludeNested)
                {
                    foreach (var entity in entityKv.Value)
                    {
                        entity._GetChilds(listData, isIncludeNested);
                    }
                }
            }
        }
        #endregion

        bool CheckDestroy()
        {
            if (IsDestroy)
            {
                Log.Error(GetType().FullName + "已销毁");
                return true;
            }
            if (IsComponent)
            {
                var temParent = Parent;
                if (temParent != null && temParent.IsDestroy)
                {
                    Log.Error(temParent.GetType().FullName + "已销毁");
                    return true;
                }
            }
            return false;
        }

        public void Destroy()
        {
            if (IsDestroy) return;
            if (_parent != null)
            {
                Parent = null;
            }
            else
            {
                _Destroy(true);
            }
        }

        void _Destroy(bool isDestroy)
        {
#if UNITY_EDITOR
            GoViewer.transform.SetParent(null);
#endif
            _RemoveSystem();
            if (!isDestroy)
            {
                _parent = null;
                return;
            }
            if (IsDestroy) return;
            _isDestroying = true;
            TimeMgr.Ins.RemoveTag(this);
            EventMgr.Ins.OffTag(this);
            if (EntityMono != null)
            {
#if UNITY_EDITOR
                EntityMono.SetEntity(null, null);
#else
                EntityMono.SetEntity(null);
#endif
                EntityMono = null;
            }
            foreach (var entity in _entitys)
            {
                entity.Value._Destroy(true);
            }

            foreach (var component in _components)
            {
                component.Value._Destroy(true);
            }

            if (_event != null)
            {
                _event.Clear();
                Pool.Push(_event);
                _event = null;
            }

            _delayFn.Enqueue(_DelayDestroy);
        }

        void _DelayDestroy()
        {
            _isDestroyed = true;
            _isDestroying = false;
            OnDestroy();
            _parent = null;
            _entitys.Clear();
            _typeToentitys.Clear();
            _components.Clear();
            ID = 0;
            _is_init = false;
#if UNITY_EDITOR
            GameObject.Destroy(GoViewer);
#endif
            if (IsFromPool)
            {
                Pool.Push(this);
            }
        }

        protected virtual void OnDestroy()
        {
        }
    }
}