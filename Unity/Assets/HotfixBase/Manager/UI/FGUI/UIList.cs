using FairyGUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 具体GList使用方式可参考官方教程
    /// https://www.fairygui.com/docs/editor/list
    /// </summary>
    public class UIList : UIObject
    {
        public UIList(GObject gObject, UIObject parent)
        {
            Init(gObject, parent);
            parent?.Components?.Add(this);
        }
        protected override void CreateChildren()
        {
        }
        public GList List => ObjAs<GList>();

        Type _itemType;
        Func<int, Type> _itemTypeFunc;
        Dictionary<GObject, IItemRenderer> _renderers = new Dictionary<GObject, IItemRenderer>();
        List<IUIParam> _datas = new List<IUIParam>();
        Action<IItemRenderer> _itemClickEvt;
        protected override void OnInit()
        {
            List.itemRenderer = _OnItemRenderer;
            List.SetVirtual();
        }

        bool _fristShow;
        protected override void OnShow()
        {
            base.OnShow();
            _fristShow = true;
        }
        protected override void OnHide()
        {
            base.OnHide();
            _ClearRenderers();
            _ClearDatas();
            _itemClickEvt = null;
        }
        protected override void OnDispose()
        {
            _itemType = null;
            _itemTypeFunc = null;
        }


        /// <summary>
        /// 设置渲染ItemRenderer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void SetItemRenderer<T>() where T : ItemRenderer
        {
            var newType = typeof(T);
            _itemType = newType;
        }
        /// <summary>
        /// 设置渲染ItemRenderer 供应器，可以使用不同的资源
        /// 页面布局不支持
        /// </summary>
        /// <param name="itemTypeFunc"></param>
        public void SetItemProvider(Func<int, Type> itemTypeFunc)
        {
            _itemTypeFunc = itemTypeFunc;
            List.itemProvider = _itemTypeFunc != null ? _OnItemProvider : null;
        }

        public void AddItemClick(Action<IItemRenderer> itemClickEvt)
        {
            _itemClickEvt = itemClickEvt;
            AddItemClick(List, _OnItemClickEvt);
        }
        void _OnItemClickEvt(EventContext e)
        {
            var gObj = (GObject)e.data;
            if (_renderers.TryGetValue(gObj, out var renderer))
            {
                _itemClickEvt?.Invoke(renderer);
            }
        }
        public int IndexOf(IUIParam data)
        {
            return _datas.IndexOf(data);
        }
        public int IndexOf<T>(T data, UIParamType type = UIParamType.A)
        {
            for (var i = 0; i < _datas.Count; i++)
            {
                if (_datas[i].TryGet(out T v, type) && data.Equals(v))
                {
                    return i;
                }
            }
            return -1;
        }
        public T GetData<T>(int index, UIParamType type= UIParamType.A)
        {
            if (index < _datas.Count)
            {
                if (_datas[index].TryGet(out T v, type))
                {
                    return v;
                }                
            }
            return default;
        }
        public IUIParam GetData(int index)
        {
            if (index < _datas.Count)
            {
                return _datas[index];
            }
            return null;
        }
        public List<IUIParam> GetDatas() => _datas;
        /// <summary>
        /// 设置数据源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="datas">数据</param>
        /// <param name="scrollToIndex">滚动位置</param>
        public void SetDatas<T>(IEnumerable<T> datas, int scrollToIndex = -1)
        {
            if (_itemType == null && _itemTypeFunc == null)
            {
                Log.Error("需要设置ItemRenderer 或 ItemProvider");
                return;
            }
            if (datas == null)
            {
                List.numItems = 0;
                return;
            }
            _ClearDatas();
            foreach (var _data in datas)
            {
                _datas.Add(IUIParam.Create(_data));
            }
            List.numItems = _datas.Count;
            _fristShow = false;
            if (scrollToIndex != -1)
            {
                List.ScrollToView(scrollToIndex);
            }
        }
        /// <summary>
        /// 设置数据源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="datas">数据</param>
        /// <param name="scrollToIndex">滚动位置</param>
        public void SetDatas(IEnumerable<IUIParam> datas, int scrollToIndex = -1)
        {
            if (_itemType == null && _itemTypeFunc == null)
            {
                Log.Error("需要设置ItemRenderer 或 ItemProvider");
                return;
            }
            if (datas == null)
            {
                List.numItems = 0;
                return;
            }
            _ClearDatas();
            _datas.AddRange(datas);            
            List.numItems = _datas.Count;
            _fristShow = false;
            if (scrollToIndex != -1)
            {
                List.ScrollToView(scrollToIndex);
            }
        }
        /// <summary>
        /// 刷新渲染
        /// </summary>
        public void RefreshVirtualList()
        {
            if (!List.isVirtual)
            {
                Log.Error("虚拟列表才可以调用此函数");
                return;
            }
            List.RefreshVirtualList();
        }

        string _OnItemProvider(int index)
        {
            if (_itemTypeFunc != null)
            {
                var temType = _itemTypeFunc(index);
                var url = UIMgr.Ins.GetItemUrl(temType);
                if (!string.IsNullOrEmpty(url))
                {
                    return url;
                }
            }
            return null;
        }
        void _OnItemRenderer(int index, GObject item)
        {
            var temType = _itemType;
            if (_itemTypeFunc != null)
            {
                var tType = _itemTypeFunc(index);
                if (tType != null)
                {
                    temType = tType;
                }
            }

            IItemRenderer renderer;
            if (_renderers.TryGetValue(item, out renderer) && renderer.GetType() != temType)
            {
                renderer.Release();
                renderer = null;
            }

            if (renderer == null)
            {
                renderer = Pool.Get(temType) as IItemRenderer;
                renderer.Init(item, this);
                _renderers[item] = renderer;
            }

            renderer.Set(index, _datas[index], _fristShow);

        }
        void _ClearRenderers()
        {
            if (_renderers.Count > 0)
            {
                foreach (var (_, renderer) in _renderers)
                {
                    renderer.Release();
                }
                _renderers.Clear();
            }
        }
        void _ClearDatas()
        {
            for (var i = 0; i < _datas.Count; i++)
            {
                _datas[i].Release();
            }
            _datas.Clear();
        }

    }
}