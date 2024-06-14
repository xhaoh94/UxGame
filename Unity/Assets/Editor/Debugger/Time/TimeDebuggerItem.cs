using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.Time
{
    public class TimeDebuggerItem<T, V> : TemplateContainer, IDebuggerListItem<TimeList>
        where T : TemplateContainer, IDebuggerListItem<V>, new()
    {
        private VisualTreeAsset _visualAsset;

        Label _txtID;
        DebuggerObjectListView<T, V> _list;
        public TimeDebuggerItem()
        {
            // 加载布局文件		
            _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Time/TimeDebuggerItem.uxml");
            if (_visualAsset == null)
                return;

            var _root = _visualAsset.CloneTree();
            _root.style.flexGrow = 1f;
            style.flexGrow = 1f;
            Add(_root);
            CreateView();
        }
        /// <summary>
        /// 初始化页面
        /// </summary>
        void CreateView()
        {
            _txtID = this.Q<Label>("txtID");
            _list = new DebuggerObjectListView<T, V>(this.Q<ListView>("list"));
        }

        public void SetData(TimeList data)
        {
            _txtID.text = data.ExeDesc;
            var listData = new List<V>();
            foreach (var handle in data.Handles)
            {
                listData.Add((V)handle);
            }
            _list.SetData(listData);
        }

        public void SetClickEvt(Action<TimeList> action)
        {

        }
    }
}