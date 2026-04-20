using System;
using System.Collections.Generic;
using System.Linq;

namespace Ux
{
    public abstract class UIBaseFactory<TUI> where TUI : IUI
    {
        protected readonly Dictionary<int, IUI> _waitDels = new Dictionary<int, IUI>();
        protected readonly Dictionary<Type, Queue<int>> _pool = new Dictionary<Type, Queue<int>>();
        protected readonly HashSet<int> _showed = new HashSet<int>();

        protected void OnShow(TUI ui)
        {
            _showed.Add(ui.ID);
        }

        protected void OnHide(TUI ui)
        {
            _showed.Remove(ui.ID);
            if (ui.HideDestroyTime >= 0)
            {
                _waitDels.Add(ui.ID, ui);
            }
            else
            {
                var type = ui.GetType();
                if (!_pool.TryGetValue(type, out var ids))
                {
                    ids = new Queue<int>();
                    _pool.Add(type, ids);
                }
                ids.Enqueue(ui.ID);
            }
        }
        public void RemoveWaitDelById(int id)
        {
            _waitDels.Remove(id);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        protected int GetUIID(Type type)
        {
            if (_pool.TryGetValue(type, out var ids) && ids.Count > 0)
            {
                return ids.Dequeue();
            }

            if (_waitDels.Count > 0)
            {
                var keys = _waitDels.Keys.ToList();
                foreach (var key in keys)
                {
                    if (!_waitDels.TryGetValue(key, out var ui) || ui.GetType() != type) continue;
                    _waitDels.Remove(key);
                    return key;
                }
            }

            var data = new UIData((int)IDGenerater.GenerateId(), type);
            UIMgr.Ins.AddUIData(data);
            return data.ID;
        }

        protected Type _defaultType;
        public void SetDefaultType<T>() where T : TUI
        {
            _defaultType = typeof(T);
        }

        protected int GetDefaultID()
        {
            Type type = _defaultType;
            if (type == null)
            {
                return 0;
            }
            return GetUIID(type);
        }

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