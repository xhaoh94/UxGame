using System;
using System.Collections.Generic;

namespace Ux
{
    public abstract class UIBaseFactory<TUI> where TUI : IUI
    {
        protected readonly Dictionary<int, IUI> _waitDels = new Dictionary<int, IUI>();
        protected readonly Dictionary<Type, Queue<int>> _pool = new Dictionary<Type, Queue<int>>();
        protected readonly HashSet<int> _showed = new HashSet<int>();
        // A type may have multiple waiting instances, so a FIFO queue is more correct than a single cached id.
        protected readonly Dictionary<Type, Queue<int>> _typeToIdCache = new Dictionary<Type, Queue<int>>();

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

        public void Clear()
        {
            _pool.Clear();
            _typeToIdCache.Clear();
        }

        protected int GetUIID(Type type)
        {
            if (_pool.TryGetValue(type, out var ids) && ids.Count > 0)
            {
                return ids.Dequeue();
            }

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
