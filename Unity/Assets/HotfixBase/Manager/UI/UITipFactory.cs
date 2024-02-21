using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ux
{
    public class UITipFactory
    {
        public struct TipData
        {
            public TipData(Action<UITip> _showFn, Action<UITip> _hideFn, string content)
            {
                ShowCallBack = _showFn;
                HideCallBack = _hideFn;
                Content = content;
            }
            public Action<UITip> HideCallBack { get; }
            public Action<UITip> ShowCallBack { get; }
            public string Content { get; }
        }

        public readonly Dictionary<int, IUI> _waitDels = new Dictionary<int, IUI>();
        readonly Dictionary<Type, Queue<int>> _pool = new Dictionary<Type, Queue<int>>();
        readonly HashSet<int> _showed = new HashSet<int>();
        void _Show(UITip tip)
        {
            _showed.Add(tip.ID);
        }
        void _Hide(UITip tip)
        {
            _showed.Remove(tip.ID);
            if (tip.IsDestroy)
            {
                _waitDels.Add(tip.ID, tip);
            }
            else
            {
                var type = tip.GetType();
                if (!_pool.TryGetValue(type, out var ids))
                {
                    ids = new Queue<int>();
                    _pool.Add(type, ids);
                }
                ids.Enqueue(tip.ID);
            }
        }
        public void Clear()
        {
            _pool.Clear();
        }
        int _GetTypeUIID(Type type)
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

        Type _defalutType;
        public void SetDefalutType<T>() where T : UITip
        {
            _defalutType = typeof(T);
        }
        int _GetDefalutID()
        {
            Type type = _defalutType;
            if (type == null)
            {
                return 0;
            }
            var id = _GetTypeUIID(type);
            return id;
        }
        bool _CheckDefalut(int id)
        {
            if (id == 0)
            {
                Log.Error("没有指定Tip面板,请检查是否已初始化SetDefalutType");
                return false;
            }
            return true;
        }
        public void Show(string content)
        {
            _Show(_GetDefalutID(), content);
        }
        public void Show<T>(string content) where T : UITip
        {
            var id = _GetTypeUIID(typeof(T));
            _Show(id, content);
        }
        void _Show(int id, string content)
        {
            if (!_CheckDefalut(id))
            {
                Log.Error("没有指定Dialog面板,请检查是否已初始化SetDefalutType");
                return;
            }
            _CheckPos();
            UIMgr.Ins.Show(id, new TipData(_Show, _Hide, content));
        }

        void _CheckPos()
        {
            foreach (var id in _showed)
            {
                var ui = UIMgr.Ins.GetUI<UITip>(id);
                if (ui == null) continue;
                var gobj = ui.GObject;
                gobj.SetPosition(gobj.x, gobj.y - 30, 0);
            }
        }
    }
}