using System;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.Event
{
    public partial class EventDebuggerItem : TemplateContainer, IDebuggerListItem<EventList>
    {
        private VisualTreeAsset _visualAsset;
        
        DebuggerStringListView _list;
        public EventDebuggerItem()
        {
            CreateChildren();
            style.flexGrow = 1f;
            Add(root);
            CreateView();
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