using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DebuggerStringListView
{
    private ListView _list;
    public DebuggerStringListView(ListView listView)
    {
        _list = listView;
        _list.makeItem = _MakeListItem;
        _list.bindItem = _BindListItem;
    }

    List<string> _listData;
    public void SetData(List<string> listData)
    {
        _listData = listData;
        _list.Clear();
        _list.ClearSelection();
        _list.itemsSource = listData;
        _list.RefreshItems();
    }

    VisualElement _MakeListItem()
    {
        // 加载视图
        var element = new VisualElement();
        element.style.flexDirection = FlexDirection.Row;
        {
            var label = new Label();
            label.name = "Label0";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.marginBottom = 1f;
            label.style.marginLeft = 1f;
            label.style.marginRight = 1f;
            label.style.marginTop = 1f;
            label.style.backgroundColor = new StyleColor(new Color(71, 71, 71, 0));
            label.style.borderTopColor =
            label.style.borderLeftColor =
            label.style.borderBottomColor =
            label.style.borderRightColor = new StyleColor(new Color(0, 0, 0, 1));

            label.style.borderBottomWidth = 1;
            label.style.borderTopWidth = 1;
            label.style.borderLeftWidth = 1;
            label.style.borderRightWidth = 1;
            label.style.flexGrow = 1f;
            //label.style.width = 200;
            element.Add(label);
        }
        return element;
    }
    void _BindListItem(VisualElement element, int index)
    {
        var data = _listData[index];
        var lb0 = element.Q<Label>("Label0");
        lb0.text = data;
    }
}