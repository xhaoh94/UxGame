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
        List<object> _datas = new List<object>();

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
            _datas.Clear();
        }
        protected override void OnDispose()
        {
            _ClearRenderers();
            _datas.Clear();
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
            if (_itemType != null && _itemType != newType)
            {
                _ClearRenderers();
            }
            _itemType = newType;
        }
        /// <summary>
        /// 设置渲染ItemRenderer 供应器，可以使用不同的资源
        /// 页面布局不支持
        /// </summary>
        /// <param name="itemTypeFunc"></param>
        public void SetItemProvider(Func<int, Type> itemTypeFunc)
        {
            if (_itemTypeFunc != null && _itemTypeFunc != itemTypeFunc)
            {
                _ClearRenderers();
            }
            _itemTypeFunc = itemTypeFunc;
            List.itemProvider = _itemTypeFunc != null ? _OnItemProvider : null;
        }

        public int FindIndex(object data)
        {
            for (int i = 0; i < _datas.Count; i++)
            {
                if (data.Equals(_datas[i])) return i;
            }
            return -1;
        }
        public T GetData<T>(int index)
        {
            if (index < _datas.Count)
            {
                return (T)_datas[index];
            }
            return default(T);
        }
        public List<object> GetDatas()
        {
            return _datas;
        }
        public IEnumerable<T> GetDatas<T>()
        {
            return _datas.Cast<T>();
        }
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
            _datas.Clear();
            _datas.AddRange(datas.Cast<object>());
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
            return List.defaultItem;
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
            if (_renderers.TryGetValue(item, out renderer) && (renderer.Index != index || renderer.GetType() != temType))
            {
                renderer.Release();
                renderer = null;
            }

            if (renderer == null)
            {
                renderer = Pool.Get(temType) as IItemRenderer;
                renderer.Init(index, this);
                _renderers[item] = renderer;
            }

            renderer.Set(item, _datas[index], _fristShow);

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


    }
}