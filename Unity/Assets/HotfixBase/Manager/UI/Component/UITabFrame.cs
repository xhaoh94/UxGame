using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public abstract class UITabFrame : UIObject
    {
        public int SelectIndex => GetIndex(SelectItem != null ? SelectItem.ID : 0);
        private List<int> _children;
        public IUI SelectItem { get; private set; }

        protected virtual UIList __listTab { get; } = null;
        protected virtual RectTransform __tabContent { get; } = null;
        protected virtual GameObject __btnClose { get; } = null;

        public UITabFrame(GameObject container, UIObject parent)
        {
            Init(container, parent);
            parent?.AddComponent(this);
        }

        public override UIState State
        {
            get
            {
                if (SelectItem == null)
                    return base.State;
                var state = SelectItem.State;
                return state is UIState.ShowAnim or UIState.HideAnim ? state : base.State;
            }
        }

        public void SetItemProvider(Func<int, int> templateIdProvider)
        {
            __listTab?.SetItemProvider(templateIdProvider);
        }

        protected override void OnInit()
        {
            base.OnInit();
            if (__listTab == null)
                return;

            var parent = Parent as IUI;
            _children = parent?.Data?.Children;
        }

        public virtual void Refresh(int selectIndex = 0, bool scrollItToView = true)
        {
            // Common2TabFrame 这类没有页签列表的容器，Refresh 不应报错。
            if (__listTab == null)
                return;

            var parent = ParentAs<UIBase>();
            _children = parent?.Data?.Children;

            if (_children == null || _children.Count == 0)
            {
                Log.Warning("TabFrame 没有子界面数据，页签不会生成。Parent[{0}]", parent?.Name);
            }

            __listTab.SetDatas(_children);
            if (selectIndex < 0)
                return;

            __listTab.SetSelectedIndex(selectIndex, scrollItToView);
            OnTabClick(selectIndex);
        }

        protected override void ToHide(bool isAnim, bool checkStack, int hideVersion)
        {
            SelectItem = null;
            _children = null;
            base.ToHide(isAnim, checkStack, hideVersion);
        }

        protected override void ToShow(bool isAnim, int id, bool checkStack, int showVersion)
        {
            base.ToShow(isAnim, id, checkStack, showVersion);
            AddItemClick(__listTab, OnTabClick);
            if (__btnClose != null)
                AddClick(__btnClose, OnBtnCloseClick);

            // 没有页签列表的容器（如 Common2TabFrame）不需要刷新列表。
            if (__listTab != null)
                Refresh(-1);
        }

        public void AddChild(UITabView tab)
        {
            if (__listTab == null || __tabContent == null || tab?.GameObject == null)
                return;

            if (SelectItem == tab)
                return;

            var index = GetIndex(tab.TypeId);
            if (index < 0)
            {
                Log.Error("找不到对应的CHILD");
                return;
            }

            tab.GameObject.transform.SetParent(__tabContent.transform, false);
            tab.GameObject.SetActive(true);
            __listTab.SetSelectedIndex(index);
            SelectItem = tab;
        }

        private int GetIndex(int id)
        {
            if (id == 0 || _children == null || _children.Count == 0)
                return -1;
            return _children.IndexOf(id);
        }

        private void OnBtnCloseClick()
        {
            ParentAs<UIBase>()?.Hide();
        }

        private void OnTabClick(IItemRenderer item)
        {
            if (__listTab == null || item == null || item.Index < 0)
                return;
            OnTabClick(item.Index);
        }

        private void OnTabClick(int index)
        {
            var id = __listTab.GetData<int>(index);
            if (id == 0)
                return;
            if (SelectItem != null && SelectItem.TypeId == id)
                return;
            UIMgr.Ins.Request(id).Show();
        }
    }
}
