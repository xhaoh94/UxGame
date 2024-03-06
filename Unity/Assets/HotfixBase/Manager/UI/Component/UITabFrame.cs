﻿using FairyGUI;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ux
{
    public abstract class UITabFrame : UIObject
    {
        private List<int> _tabDatas;
        private Type _tabRenderer;

        public int SelectIndex => GetIndex(SelectItem != null ? SelectItem.ID : 0);
        public ITabView SelectItem { get; private set; }

        readonly List<UITabBtn> _frameTabs = new List<UITabBtn>();

        public UITabFrame(GObject container, UIObject parent)
        {
            Init(container, parent);
            parent?.Components?.Add(this);
        }

        #region 组件

        protected virtual GList __listTab { get; } = null;
        protected virtual GComponent __tabContent { get; } = null;
        protected virtual UIButton __btnClose { get; } = null;

        #endregion

        public override UIState State
        {
            get
            {
                if (SelectItem == null) return base.State;
                var state = SelectItem.State;
                return state is UIState.ShowAnim or UIState.HideAnim ? state : base.State;
            }
        }


        public void SetTabRenderer<T>() where T : UITabBtn
        {
            _tabRenderer = typeof(T);
        }

        protected override void OnInit()
        {
            base.OnInit();
            if (__listTab == null) return;
            var parent = (IUI)Parent;
            var _children = parent.Data.Children;
            if (_children == null || _children.Count == 0) return;
            __listTab.itemRenderer = OnItemRenderer;
            _tabDatas = new List<int>();
            OnHideCallBack += _Hide;
        }

        public virtual void Refresh(int selectIndex = 0, bool scrollItToView = true)
        {
            if (_tabDatas == null) return;
            if (__listTab == null) return;
            _tabDatas.Clear();
            var parent = ParentAs<UIBase>();
            var _children = parent.Data.Children;
            foreach (var data in _children)
            {
                _tabDatas.Add(data);
            }

            ClearFrameTabs();
            __listTab.numItems = _tabDatas.Count;
            if (selectIndex < 0) return;
            __listTab.selectedIndex = selectIndex;
            __listTab.AddSelection(selectIndex, scrollItToView);
            OnTabClick(selectIndex);
        }

        protected override void ToShow(bool isAnim, int id, object param, bool isStack, CancellationTokenSource token)
        {
            base.ToShow(isAnim, id, param, isStack, token);
            if (__listTab != null && _tabDatas != null)
            {
                AddItemClick(__listTab, OnTabClick);
            }

            if (__btnClose != null)
            {
                AddClick(__btnClose, OnBtnCloseClick);
            }

            Refresh(-1);
        }

        protected override void ToHide(bool isAnim, bool isStack, CancellationTokenSource token)
        {            
            SelectItem?.HideByParent(isAnim, isStack, token);
            base.ToHide(isAnim, isStack, token);
        }

        void _Hide()
        {
            _tabRenderer = null;
            _tabDatas.Clear();
            SelectItem = null;
        }


        public void AddChild(UITabView tab)
        {
            if (__tabContent == null) return;
            if (__listTab == null) return;
            if (tab.GObject == null) return;
            if (tab == SelectItem) return;
            int index = GetIndex(tab.ID);
            if (index < 0)
            {
                Log.Error("找不到对应的CHILD");
                return;
            }
            __tabContent.AddChild(tab.GObject);
            SelectItem?.HideByTab();
            if (index != __listTab.selectedIndex) __listTab.selectedIndex = index;
            SelectItem = tab;
        }

        private int GetIndex(int id)
        {
            if (id == 0) return -1;
            if (_tabDatas == null) return -1;
            return _tabDatas.FindIndex(a => a == id);
        }

        private void OnBtnCloseClick()
        {
            ParentAs<UIBase>().Hide();
        }

        private void OnTabClick(EventContext e)
        {
            if (__listTab == null) return;
            if (_tabDatas == null) return;
            int index = __listTab.GetChildIndex((GObject)e.data);
            if (index == -1) return;
            if (_tabDatas.Count < index) return;
            OnTabClick(index);
        }

        private void OnTabClick(int index)
        {
            var id = _tabDatas[index];
            if (id == 0) return;
            if (SelectItem != null && SelectItem.ID == id) return;
            UIMgr.Ins.Show(id);
        }

        private void OnItemRenderer(int index, GObject item)
        {
            if (_tabDatas == null) return;
            var id = _tabDatas[index];
            if (id == 0) return;
            var data = UIMgr.Ins.GetUIData(id);
            if (data == null) return;
            _tabRenderer ??= typeof(CommonTabBtn);

            var tab = Pool.Get<UITabBtn>(_tabRenderer);

            tab.InitData(data, item, this);
            _frameTabs.Add(tab);
        }

        private void ClearFrameTabs(bool isDispose = false)
        {
            if (_frameTabs.Count > 0)
            {
                foreach (var tab in _frameTabs)
                {
                    tab.Release(isDispose);
                }

                _frameTabs.Clear();
            }

            if (__listTab != null)
            {
                if (__listTab.numItems > 0) __listTab.numItems = 0;
                __listTab.ClearSelection();
            }
        }

        protected override void OnDispose()
        {
            ClearFrameTabs(true);
        }
    }

    public class CommonTabBtn : UITabBtn
    {
        private GButton _item;

        protected override void OnInit()
        {
            _item = ObjAs<GButton>();
        }

        protected override void OnShow(object param)
        {
            base.OnShow(param);
            if (Data.TabData.Title is string title)
            {
                if (string.IsNullOrEmpty(title)) return;
                _item.title = title;
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _item = null;
        }
    }
}