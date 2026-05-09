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
            if (ui.HideDestroyTime >= 0) return;
            var type = ui.GetType();
            if (!_pool.TryGetValue(type, out var ids))
            {
                ids = new Queue<int>();
                _pool.Add(type, ids);
            }
            ids.Enqueue(ui.ID);
        }
        
        /// <summary>
        /// 根据ID从等待销毁列表中移除UI
        /// </summary>
        /// <param name="id">要移除的UI ID</param>
        public void RemoveWaitDelById(int id)
        {
        }

        /// <summary>
        /// 清空对象池和类型ID缓存
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
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
