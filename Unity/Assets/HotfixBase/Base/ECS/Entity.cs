using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ux
{
    public abstract partial class Entity
    {
        struct DelayFn
        {
            public Action delayFn;
            public int farme;
        }
        static readonly Queue<DelayFn> _delayFn = new Queue<DelayFn>();
        static bool _isRunning = false;
        static void _DelayInvoke()
        {
            var _exeNum = 0;
            var max = 200;
            while (_delayFn.Count > 0 && (_exeNum++) < max) //一帧最多执行max次
            {
                var fn = _delayFn.Peek();
                //放进来后下一帧再销毁，以免同帧下销毁时，业务逻辑报错
                if (fn.farme >= TimeMgr.Ins.TotalFrame) break;
                _delayFn.Dequeue().delayFn.Invoke();
            }
        }
        static void EnqueueDelay(Action fn)
        {
            _delayFn.Enqueue(new DelayFn() { delayFn = fn, farme = TimeMgr.Ins.TotalFrame });
            if (!_isRunning)
            {
                _isRunning = true;
                GameMain.Ins.AddLateUpdate(_DelayInvoke);
            }
        }



#if UNITY_EDITOR        
        public EntityViewer Viewer { get; private set; }
#endif
        public EntityMono EntityMono { get; private set; }

        public long ID { get; private set; }
        string _name;
        public string Name
        {
            get => _name; set
            {
                if (_name != value)
                {
                    _name = value;
#if UNITY_EDITOR
                    Viewer.name = value;
#endif
                }
            }
        }
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
#if UNITY_EDITOR
            entity.Viewer = EntityViewer.Create(entity);
#endif
            entity._isDestroyed = false;
            entity._isDestroying = false;
            entity.IsFromPool = isFromPool;
            entity.ID = id;
            entity.Name = $"{type.Name}_{id}";
            return entity;
        }

        public void SetMono(GameObject gameObject, bool isSetParent = true)
        {
            EntityMono = gameObject.GetOrAddComponent<EntityMono>();
#if UNITY_EDITOR
            EntityMono.SetEntity(this, Viewer);
            if (isSetParent)
            {
                if (IsComponent)
                {
                    gameObject.SetParent(Parent.Viewer.AssetContent);
                }
                else
                {
                    gameObject.SetParent(Viewer.AssetContent);
                }
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
            var entityID = entity.ID;

            if (IsComponent)
            {
                var temParent = Parent;
                if (temParent == null)
                {
                    Log.Error("父实体为空，无法添加子实体");
                    return false;
                }
                temParent._AddChild(entity);
            }
            else
            {
                if (entity._parent == this)
                {
                    return true;
                }
                entity._parent?.RemoveChild(entity, false);
                entity._parent = this;

#if UNITY_EDITOR
                entity.Viewer.SetParent(Viewer.EntityContent);
#endif
            }

            _entitys.Add(entityID, entity);
            var type = entity.GetType();
            if (!_typeToentitys.TryGetValue(type, out var listData))
            {
                listData = new List<Entity>();
                _typeToentitys.Add(type, listData);
            }
            listData.Add(entity);
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
        public bool RemoveChild(long cid)
        {
            var child = GetChild(cid);
            if (child == null)
            {
                return true;
            }
            return RemoveChild(child);
        }
        public bool RemoveChild(Entity entity)
        {
            return RemoveChild(entity, true);
        }
        bool RemoveChild(Entity entity, bool isDestroy)
        {
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
                if (IsComponent)
                {
                    return Parent.RemoveChild(entity, isDestroy);
                }
                entity._Destroy(isDestroy);
                return true;
            }
            return false;
        }

        void RemoveChilds()
        {
            while (_entitys.Count > 0)
            {
                RemoveChild(_entitys.ElementAt(0).Value, true);
            }
        }
        public T GetChild<T>(long id) where T : Entity
        {
            return GetChild(id) as T;
        }
        public Entity GetChild(long id)
        {
            if (_entitys.TryGetValue(id, out var entity))
            {
                return entity;
            }
            return null;
        }
        public List<Entity> GetChilds(bool isIncludeNested = false)
        {
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
                Log.Error(Name + "已销毁");
                return true;
            }
            if (IsComponent)
            {
                var temParent = Parent;
                if (temParent != null && temParent.IsDestroy)
                {
                    Log.Error(temParent.Name + "已销毁");
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
            if (!isDestroy)
            {
                _RemoveSystem();
                _parent = null;
                return;
            }
            if (IsDestroy) return;
            _RemoveSystem();
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

            RemoveChilds();
            RemoveComponents();

            if (_event != null)
            {
                _event.Clear();
                Pool.Push(_event);
                _event = null;
            }


            EnqueueDelay(_DelayDestroy);
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
            _name = null;
            ID = 0;
            _is_init = false;
#if UNITY_EDITOR
            if (Viewer != null)
            {
                Viewer.Release();
                Viewer = null;
            }
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