using System;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.Event
{
    public class EventDebuggerItem : TemplateContainer, IDebuggerListItem<EventList>
    {
        private VisualTreeAsset _visualAsset;

        Label _txtID;
        DebuggerStringListView _list;
        public EventDebuggerItem()
        {
            // ���ز����ļ�		
            _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Event/EventDebuggerItem.uxml");
            if (_visualAsset == null)
                return;

            var _root = _visualAsset.CloneTree();
            _root.style.flexGrow = 1f;
            style.flexGrow = 1f;
            Add(_root);
            CreateView();
        }

        /// <summary>
        /// ��ʼ��ҳ��
        /// </summary>
        void CreateView()
        {
            _txtID = this.Q<Label>("txtID");
            _list = new DebuggerStringListView(this.Q<ListView>("listEvt"));
        }

        public void SetData(EventList data)
        {
            _txtID.text = data._eventType;
            _list.SetData(data.events);
        }

        public void SetClickEvt(Action<EventList> action)
        {

        }
    }
}