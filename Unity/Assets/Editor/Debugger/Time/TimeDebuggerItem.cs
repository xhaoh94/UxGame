using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.Time
{
    public partial class TimeDebuggerItem : TemplateContainer
    {
        protected VisualElement root;
        public Label txtID;
        public ListView list;

        /// <summary>原 TimeDebuggerItem.uxml 的手搭等价版本。</summary>
        protected void BuildUI()
        {
            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1f;
            root.style.flexShrink = 1f;
            root.SetBorder(Color.black);
            root.SetMargin(1f);

            txtID = DebuggerUiUtil.IdLabel("txtID");
            root.Add(DebuggerUiUtil.IdBox(txtID));

            list = DebuggerUiUtil.DynamicList("list");
            list.style.flexGrow = 1f;
            root.Add(list);
        }
    }
    public partial class TimeDebuggerItem<T, V> : TimeDebuggerItem, IDebuggerListItem<TimeList>
        where T : TemplateContainer, IDebuggerListItem<V>, new()
    {
        DebuggerObjectListView<T, V> _list;        
        public TimeDebuggerItem()
        {            
            BuildUI();
            Add(root);
            _list = new DebuggerObjectListView<T, V>(list);
        }

        public void SetData(TimeList data)
        {            
            txtID.text = data.ExeDesc;
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