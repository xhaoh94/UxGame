using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.Time
{
    public partial class TimeDebuggerItem:TemplateContainer{}
    public partial class TimeDebuggerItem<T, V> : TimeDebuggerItem, IDebuggerListItem<TimeList>
        where T : TemplateContainer, IDebuggerListItem<V>, new()
    {
        DebuggerObjectListView<T, V> _list;        
        public TimeDebuggerItem()
        {            
            CreateChildren();
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