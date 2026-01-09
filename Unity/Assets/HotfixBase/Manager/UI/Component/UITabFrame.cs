using FairyGUI;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ux
{
    public abstract class UITabFrame : UIObject
    {
        public int SelectIndex => GetIndex(SelectItem != null ? SelectItem.ID : 0);
        List<int> _children;
        public IUI SelectItem { get; private set; }

        public UITabFrame(GObject container, UIObject parent)
        {
            Init(container, parent);
            parent?.AddComponent(this);
        }

        #region 组件

        protected virtual UIList __listTab { get; } = null;
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

        public void SetItemRenderer<T>() where T : ItemRenderer, IItemRenderer
        {
            __listTab?.SetItemRenderer<T>();
        }

        public void SetItemProvider(Func<int, Type> itemTypeFunc)
        {
            __listTab?.SetItemProvider(itemTypeFunc);
        }

        protected override void OnInit()
        {
            base.OnInit();
            if (__listTab == null) return;
            var parent = (IUI)Parent;
            var _children = parent.Data.Children;
            if (_children == null || _children.Count == 0) return;
            OnHideCallBack += _Hide;
        }

        public virtual void Refresh(int selectIndex = 0, bool scrollItToView = true)
        {
            if (__listTab == null) return;
            var parent = ParentAs<UIBase>();
            _children = parent.Data.Children;
            __listTab.SetDatas(_children);
            if (selectIndex < 0) return;
            __listTab.List.selectedIndex = selectIndex;
            __listTab.List.AddSelection(selectIndex, scrollItToView);
            OnTabClick(selectIndex);
        }

        protected override void ToShow(bool isAnim, int id, IUIParam param, bool checkStack, CancellationTokenSource token)
        {
            base.ToShow(isAnim, id, param, checkStack, token);
            AddItemClick(__listTab, OnTabClick);
            AddClick(__btnClose, OnBtnCloseClick);
            Refresh(-1);
        }

        protected override void ToHide(bool isAnim, bool checkStack, CancellationTokenSource token)
        {
            SelectItem?.DoHide(isAnim, checkStack);            
            base.ToHide(isAnim, checkStack, token);
        }

        void _Hide()
        {
            SelectItem = null;
            _children = null;
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
            SelectItem?.DoHide(false,false);
            if (index != __listTab.List.selectedIndex) __listTab.List.selectedIndex = index;
            SelectItem = tab;
        }

        private int GetIndex(int id)
        {
            if (id == 0) return -1;
            if (_children == null|| _children.Count == 0) return -1;
            return _children.IndexOf(id);
        }

        private void OnBtnCloseClick()
        {
            ParentAs<UIBase>().Hide();
        }

        private void OnTabClick(IItemRenderer item)
        {
            if (__listTab == null) return;
            int index = item.Index;
            if (index == -1) return;
            if (__listTab.List.numItems < index) return;
            OnTabClick(index);
        }

        private void OnTabClick(int index)
        {
            var id = __listTab.GetData<int>(index);
            if (id == 0) return;
            if (SelectItem != null && SelectItem.ID == id) return;
            UIMgr.Ins.Show(id);
        }
    }
}