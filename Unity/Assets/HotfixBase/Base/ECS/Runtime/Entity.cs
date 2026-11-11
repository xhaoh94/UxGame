using System;
using System.Collections.Generic;
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
        static bool _inited;
        public static void Init()
        {
            // 幂等：状态机可能反复进出游戏态，重复订阅会导致每帧多跑一次且 Release 减不干净。
            if (_inited) return;
            _inited = true;

            EntityScheduler.Init();
        }
        public static void Release()
        {
            if (!_inited) return;
            _inited = false;

            EntityScheduler.Release();
        }

        /// <summary>销毁延迟队列的每帧驱动，由 EntityScheduler 的 LateUpdate 入口调用。</summary>
        internal static void TickDelay() => _DequeueDelay();
        static void _EnqueueDelay(Action fn)
        {
            _delayFn.Enqueue(new DelayFn() { delayFn = fn, farme = TimeMgr.Ins.TotalFrame });
        }    
        static void _DequeueDelay()
        {
            if (_delayFn.Count == 0) return;

            var exeNum = 0;
            var max = 200;
            var frame = TimeMgr.Ins.TotalFrame;
            while (_delayFn.Count > 0 && exeNum++ < max) //一帧最多执行max次
            {
                var fn = _delayFn.Peek();
                //放进来后下一帧再销毁，以免同帧下销毁时，业务逻辑报错
                if (fn.farme >= frame) break;
                _delayFn.Dequeue().delayFn.Invoke();
            }
        }


        public EntityViewer Viewer { get; private set; }
        public long ID { get; private set; }
        string _name;
        public string Name
        {
            // 懒计算：Name 主要给日志和编辑器层级用，大多数实体创建时并不需要它。
            get => _name ??= $"{GetType().Name}_{ID}";
            set
            {
                if (ReferenceEquals(_name, value)) return;
                _name = value;
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
 
        // 叶子实体占多数，两个索引表按需创建，省掉每个实体的空字典开销。
        Dictionary<long, Entity> _entitys;
        Dictionary<Type, List<Entity>> _typeToentitys;
        Entity _parent;        

        public Entity Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (_parent == value)
                {
                    return;
                }

                if (value == null)
                {
                    _parent?.Remove(this);
                }
                else
                {
                    value.Add(this);
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
            var entity = isFromPool ? Pool.Get<Entity>(type) : Activator.CreateInstance(type) as Entity;
            if (entity == null) return null;
#if UNITY_EDITOR
            EditorEntityRegistry.Register(entity);
#endif
            entity._isDestroyed = false;
            entity._isDestroying = false;
            entity.IsFromPool = isFromPool;
            entity.ID = id;
            return entity;
        }
        /// <summary>
        /// 关联显示对象
        /// </summary>
        /// <param name="gameObject">显示对象</param>        
        public void Link(GameObject gameObject)
        {
            Viewer = gameObject.GetOrAddComponent<EntityViewer>();
            Viewer.SetEntity(this);

            // 没有自定义过名字时，用视图对象的名字，方便在调试窗口里对应到场景对象。
            if (_name == null && gameObject != null)
            {
                _name = gameObject.name;
            }
        }
        bool _Add(Entity entity)
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


            if (entity == this)
            {
                Log.Error("不可添加自己");
                return false;
            }


            var entityID = entity.ID;
            if (_entitys != null && _entitys.ContainsKey(entityID))
            {
                Log.Error($"重复添加实体,ID:{entityID}");
                return false;
            }

            if (entity._parent == this)
            {
                return true;
            }
            entity._parent?.Remove(entity, false);
            entity._parent = this;


            _entitys ??= new Dictionary<long, Entity>();
            _entitys.Add(entityID, entity);

            _typeToentitys ??= new Dictionary<Type, List<Entity>>();
            var type = entity.GetType();
            if (!_typeToentitys.TryGetValue(type, out var listData))
            {
                listData = new List<Entity>();
                _typeToentitys.Add(type, listData);
            }
            listData.Add(entity);
            return true;
        }

        Entity Add(Entity entity)
        {
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem();
            return entity;
        }

        public TEntity Add<TEntity>(bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add(typeof(TEntity), isFromPool);
        }

        public TEntity Add<TEntity, A>(A a, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add(typeof(TEntity), a, isFromPool);
        }

        public TEntity Add<TEntity, A, B>(A a, B b, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add(typeof(TEntity), a, b, isFromPool);
        }

        public TEntity Add<TEntity, A, B, C>(A a, B b, C c, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add(typeof(TEntity), a, b, c, isFromPool);
        }

        public TEntity Add<TEntity, A, B, C, D>(A a, B b, C c, D d, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add(typeof(TEntity), a, b, c, d, isFromPool);
        }

        public TEntity Add<TEntity, A, B, C, D, E>(A a, B b, C c, D d, E e, bool isFromPool = true)
            where TEntity : Entity
        {
            return (TEntity)Add(typeof(TEntity), a, b, c, d, e, isFromPool);
        }
        public TEntity Add<TEntity>(long id, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add(id, typeof(TEntity), isFromPool);
        }

        public TEntity Add<TEntity, A>(long id, A a, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add(id, typeof(TEntity), a, isFromPool);
        }

        public TEntity Add<TEntity, A, B>(long id, A a, B b, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add(id, typeof(TEntity), a, b, isFromPool);
        }

        public TEntity Add<TEntity, A, B, C>(long id, A a, B b, C c, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add(id, typeof(TEntity), a, b, c, isFromPool);
        }

        public TEntity Add<TEntity, A, B, C, D>(long id, A a, B b, C c, D d, bool isFromPool = true) where TEntity : Entity
        {
            return (TEntity)Add<A, B, C, D, bool>(id, typeof(TEntity), a, b, c, d, isFromPool);
        }

        public TEntity Add<TEntity, A, B, C, D, E>(long id, A a, B b, C c, D d, E e, bool isFromPool = true)
            where TEntity : Entity
        {
            return (TEntity)Add(id, typeof(TEntity), a, b, c, d, e, isFromPool);
        }
        public Entity Add(long id, Type type, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem();
            return entity;
        }

        public Entity Add<A>(long id, Type type, A a, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a);
            return entity;
        }

        public Entity Add<A, B>(long id, Type type, A a, B b, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a, b);
            return entity;
        }

        public Entity Add<A, B, C>(long id, Type type, A a, B b, C c, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c);
            return entity;
        }

        public Entity Add<A, B, C, D>(long id, Type type, A a, B b, C c, D d, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c, d);
            return entity;
        }

        public Entity Add<A, B, C, D, E>(long id, Type type, A a, B b, C c, D d, E e, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(id, type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c, d, e);
            return entity;
        }
        public Entity Add(Type type, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem();
            return entity;
        }

        public Entity Add<A>(Type type, A a, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a);
            return entity;
        }

        public Entity Add<A, B>(Type type, A a, B b, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a, b);
            return entity;
        }

        public Entity Add<A, B, C>(Type type, A a, B b, C c, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c);
            return entity;
        }

        public Entity Add<A, B, C, D>(Type type, A a, B b, C c, D d, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c, d);
            return entity;
        }

        public Entity Add<A, B, C, D, E>(Type type, A a, B b, C c, D d, E e, bool isFromPool = true)
        {
            if (CheckDestroy())
            {
                return null;
            }

            var entity = Entity.Create(IDGenerater.GenerateId(), type, isFromPool);
            if (!_Add(entity))
            {
                return null;
            }

            entity._InitSystem(a, b, c, d, e);
            return entity;
        }
        public bool Remove(long cid)
        {
            var child = Get(cid);
            if (child == null)
            {
                return true;
            }
            return Remove(child);
        }
        public bool Remove<T>() where T : Entity
        {
            var child = Get<T>();
            if (child == null)
            {
                return true;
            }
            return Remove(child);
        }
        public bool Remove(Entity entity)
        {
            return Remove(entity, true);
        }
        bool Remove(Entity entity, bool isDestroy)
        {
            if (entity == null)
            {
                Log.Error("Entity为空");
                return false;
            }
            var b = _Remove(entity);
            entity._Destroy(isDestroy);
            return b;            
        }
        bool _Remove(Entity entity)
        {
            if (_entitys != null && _entitys.Remove(entity.ID))
            {
                var type = entity.GetType();
                if (_typeToentitys != null && _typeToentitys.TryGetValue(type, out var entities))
                {
                    if (entities.Remove(entity) && entities.Count == 0)
                    {
                        _typeToentitys.Remove(type);
                    }
                }
                return true;
            }
            return false;
        }

        readonly List<long> _removingIds = new List<long>();

        void RemoveAll()
        {
            if (_entitys == null || _entitys.Count == 0) return;

            // 先快照 ID 再删：Remove 过程中子实体会递归 RemoveAll 改 _entitys，
            // 直接遍历会破坏迭代；ElementAt(0) 每次枚举到第一个元素是 O(n)，整体会退化成 O(n²)。
            _removingIds.Clear();
            foreach (var kv in _entitys)
            {
                _removingIds.Add(kv.Key);
            }

            for (int i = 0; i < _removingIds.Count; i++)
            {
                if (i >= 1000)
                {
                    Log.Error("RemoveAll 删除超出循环，请检测");
                    break;
                }
                Remove(_removingIds[i]);
            }

            _removingIds.Clear();

            if (_entitys.Count > 0)
            {
                Log.Error("RemoveChilds 存在无法删除的Child！检查是否存在已被Destroy却没有从父类清除的Child");
            }
        }

        public T Get<T>(long id) where T : Entity
        {
            return Get(id) as T;
        }
        public Entity Get(long id)
        {
            if (_entitys != null && _entitys.TryGetValue(id, out var entity))
            {
                return entity;
            }
            return null;
        }
        public T GetOrAdd<T>() where T : Entity
        {
            var entity = Get<T>();
            entity ??= Add<T>();            
            return entity;
        }
        public T Get<T>() where T : Entity
        {
            if (_typeToentitys != null && _typeToentitys.TryGetValue(typeof(T), out var _temList) && _temList.Count > 0)
            {
                return _temList[0] as T;
            }
            return null;
        }
        public Entity Get(Type type)
        {
            if (_typeToentitys != null && _typeToentitys.TryGetValue(type, out var _temList) && _temList.Count > 0)
            {
                return _temList[0];
            }
            return null;
        }

        /// <summary>遍历直接子实体（供编辑器调试窗口构建树，不分配）。</summary>
        public void GetChildren(List<Entity> buffer)
        {
            buffer.Clear();
            if (_entitys == null) return;

            foreach (var pair in _entitys)
            {
                buffer.Add(pair.Value);
            }
        }

        public List<Entity> GetAll(bool includeNested = false)
        {
            if (CheckDestroy())
            {
                return null;
            }
            var listData = new List<Entity>();
            _Get(listData, includeNested);
            return listData;
        }
        public List<Entity> GetAll(Type type, bool isIncludeNested = false)
        {
            if (CheckDestroy())
            {
                return null;
            }
            var listData = new List<Entity>();
            _Get(type, listData, isIncludeNested);
            return listData;
        }
        public List<T> GetAll<T>(bool isIncludeNested = false) where T : Entity
        {
            if (CheckDestroy())
            {
                return null;
            }
            var listData = new List<T>();
            _Get(listData, isIncludeNested);
            return listData;
        }
        /// <summary>复用调用方列表的版本，避免每帧查询都分配 List。</summary>
        public void GetAll<T>(List<T> buffer, bool isIncludeNested = false) where T : Entity
        {
            buffer.Clear();
            if (CheckDestroy()) return;
            _Get(buffer, isIncludeNested);
        }

        void _Get<T>(List<T> listData, bool isIncludeNested) where T : Entity
        {
            if (_typeToentitys == null) return;
            if (_typeToentitys.TryGetValue(typeof(T), out var _temList))
            {
                foreach (var entity in _temList)
                {
                    listData.Add(entity as T);
                    if (isIncludeNested)
                    {
                        entity._Get(listData, isIncludeNested);
                    }
                }
            }
        }
        void _Get(Type type, List<Entity> listData, bool isIncludeNested)
        {
            if (_typeToentitys == null) return;
            if (_typeToentitys.TryGetValue(type, out var _temList))
            {
                listData.AddRange(_temList);
                if (isIncludeNested)
                {
                    foreach (var entity in _temList)
                    {
                        entity._Get(type, listData, isIncludeNested);
                    }
                }
            }
        }
        void _Get(List<Entity> listData, bool isIncludeNested)
        {
            if (_typeToentitys == null) return;
            foreach (var entityKv in _typeToentitys)
            {
                listData.AddRange(entityKv.Value);
                if (isIncludeNested)
                {
                    foreach (var entity in entityKv.Value)
                    {
                        entity._Get(listData, isIncludeNested);
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
            if (Application.isPlaying)
            {
                TimeMgr.Ins.RemoveTag(this);
                EventMgr.Ins.OffTag(this);
            }

            RemoveAll();            

            if (Application.isPlaying)
            {
                _EnqueueDelay(_DelayDestroy);
            }
            else
            {
                _DelayDestroy();
            }
        }

        void _DelayDestroy()
        {
            _isDestroyed = true;
            _isDestroying = false;
            OnDestroy();
            
            _parent = null;
            _entitys?.Clear();
            _typeToentitys?.Clear();
            // 池复用：清掉自己作为父实体登记的监听表，以及自己挂出去的订阅记录。
            _childAdded?.Clear();
            _childRemoved?.Clear();
            _childListeners?.Clear();
            _name = null;
            _is_init = false;
            ID = 0;
            if (Viewer != null)
            {
                Viewer.Release();
                Viewer = null;
            }
#if UNITY_EDITOR
            EditorEntityRegistry.Unregister(this);

            if (IsFromPool && UnityEngine.Application.isPlaying)
            {
                Pool.Push(this);
            }
#else
            if (IsFromPool)
            {
                Pool.Push(this);
            }
#endif

        }

        protected virtual void OnDestroy()
        {
        }
    }
}