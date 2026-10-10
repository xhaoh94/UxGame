using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.Event
{
    public partial class EventDebuggerItem : TemplateContainer, IDebuggerListItem<EventList>
    {
        protected VisualElement root;
        public Label txtID;
        public ListView listEvt;

        DebuggerStringListView _list;
        public EventDebuggerItem()
        {
            BuildUI();
            style.flexGrow = 1f;
            Add(root);
            CreateView();
        }

        /// <summary>原 EventDebuggerItem.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1f;
            root.style.flexShrink = 1f;
            root.SetBorder(Color.black);
            root.SetMargin(1f);

            txtID = DebuggerUiUtil.IdLabel("txtID");
            root.Add(DebuggerUiUtil.IdBox(txtID));

            listEvt = DebuggerUiUtil.DynamicList("listEvt");
            listEvt.style.flexGrow = 1f;
            root.Add(listEvt);
        }

        /// <summary>
        /// 初始化页面
        /// </summary>
        void CreateView()
        {            
            _list = new DebuggerStringListView(listEvt);
        }

        public void SetData(EventList data)
        {
            txtID.text = data._eventType;
            _list.SetData(data.events);
        }

        public void SetClickEvt(Action<EventList> action)
        {

        }
    }
}