using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>
    /// UI基础工厂类，用于管理UI实例的创建、回收和复用
    /// 这是一个泛型抽象类，TUI必须是实现了IUI接口的类型
    /// </summary>
    /// <typeparam name="TUI">UI类型，必须是IUI的子类</typeparam>
    public abstract class UIBaseFactory<TUI> where TUI : IUI
    {
        /// <summary>
        /// 等待销毁的UI字典，存储需要延迟销毁的UI实例
        /// key: UI的ID
        /// value: UI实例
        /// </summary>
        protected readonly Dictionary<int, IUI> _waitDels = new Dictionary<int, IUI>();
        
        /// <summary>
        /// UI对象池，按类型分类存储可复用的UI ID
        /// key: UI类型
        /// value: 可复用的UI ID队列
        /// </summary>
        protected readonly Dictionary<Type, Queue<int>> _pool = new Dictionary<Type, Queue<int>>();
        
        /// <summary>
        /// 当前正在显示的UI集合
        /// value: UI的ID
        /// </summary>
        protected readonly HashSet<int> _showed = new HashSet<int>();
        
        /// <summary>
        /// 类型到等待销毁的UI ID队列的映射
        /// 注意：一个类型可能有多个等待销毁的UI实例，所以使用FIFO队列比单个缓存ID更正确
        /// </summary>
        protected readonly Dictionary<Type, Queue<int>> _typeToIdCache = new Dictionary<Type, Queue<int>>();

        /// <summary>
        /// 当UI显示时调用
        /// </summary>
        /// <param name="ui">显示的UI实例</param>
        protected void OnShow(TUI ui)
        {
            _showed.Add(ui.ID);
        }

        /// <summary>
        /// 当UI隐藏时调用
        /// </summary>
        /// <param name="ui">隐藏的UI实例</param>
        protected void OnHide(TUI ui)
        {
            _showed.Remove(ui.ID);
            if (ui.HideDestroyTime >= 0)
            {
                // 如果设置了延迟销毁时间，将UI加入等待销毁列表
                _waitDels.Add(ui.ID, ui);
                var type = ui.GetType();
                if (!_typeToIdCache.TryGetValue(type, out var ids))
                {
                    ids = new Queue<int>();
                    _typeToIdCache.Add(type, ids);
                }
                ids.Enqueue(ui.ID);
            }
            else
            {
                // 如果不延迟销毁，将UI ID加入对象池以供复用
                var type = ui.GetType();
                if (!_pool.TryGetValue(type, out var ids))
                {
                    ids = new Queue<int>();
                    _pool.Add(type, ids);
                }
                ids.Enqueue(ui.ID);
            }
        }
        
        /// <summary>
        /// 根据ID从等待销毁列表中移除UI
        /// </summary>
        /// <param name="id">要移除的UI ID</param>
        public void RemoveWaitDelById(int id)
        {
            if (_waitDels.TryGetValue(id, out var ui))
            {
                var type = ui.GetType();
                if (_typeToIdCache.TryGetValue(type, out var ids))
                {
                    var keep = new Queue<int>();
                    while (ids.Count > 0)
                    {
                        var cachedId = ids.Dequeue();
                        if (cachedId != id)
                        {
                            keep.Enqueue(cachedId);
                        }
                    }

                    if (keep.Count == 0)
                    {
                        _typeToIdCache.Remove(type);
                    }
                    else
                    {
                        _typeToIdCache[type] = keep;
                    }
                }
            }
            _waitDels.Remove(id);
        }

        /// <summary>
        /// 清空对象池和类型ID缓存
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
            _typeToIdCache.Clear();
        }

        /// <summary>
        /// 获取UI的ID，优先从对象池中获取可复用的ID
        /// </summary>
        /// <param name="type">UI的类型</param>
        /// <returns>UI的ID</returns>
        protected int GetUIID(Type type)
        {
            // 先从对象池中获取可复用的ID
            if (_pool.TryGetValue(type, out var ids) && ids.Count > 0)
            {
                return ids.Dequeue();
            }

            // 然后从等待销毁的缓存中查找可用的ID
            if (_typeToIdCache.TryGetValue(type, out var cachedIds))
            {
                while (cachedIds.Count > 0)
                {
                    var cachedId = cachedIds.Dequeue();
                    if (_waitDels.ContainsKey(cachedId))
                    {
                        _waitDels.Remove(cachedId);
                        if (cachedIds.Count == 0)
                        {
                            _typeToIdCache.Remove(type);
                        }
                        return cachedId;
                    }
                }

                _typeToIdCache.Remove(type);
            }

            // 如果都没有可复用的ID，则创建新的UIData
            var data = new UIData((int)IDGenerater.GenerateId(), type);
            UIMgr.Ins.AddUIData(data);
            return data.ID;
        }

        /// <summary>
        /// 默认的UI类型
        /// </summary>
        protected Type _defaultType;
        
        /// <summary>
        /// 设置默认的UI类型
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        public void SetDefaultType<T>() where T : TUI
        {
            _defaultType = typeof(T);
        }

        /// <summary>
        /// 获取默认类型的UI ID
        /// </summary>
        /// <returns>默认UI的ID，如果没有设置默认类型则返回0</returns>
        protected int GetDefaultID()
        {
            Type type = _defaultType;
            if (type == null)
            {
                return 0;
            }
            return GetUIID(type);
        }

        /// <summary>
        /// 检查是否为默认类型，ID是否为0
        /// </summary>
        /// <param name="id">要检查的UI ID</param>
        /// <returns>如果ID为0返回false，否则返回true</returns>
        protected bool CheckDefault(int id)
        {
            if (id == 0)
            {
                Log.Error("没有指定UI面板,请检查是否已初始化SetDefaultType");
                return false;
            }
            return true;
        }
    }
}
