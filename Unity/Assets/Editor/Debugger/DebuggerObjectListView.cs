using System.Collections.Generic;
using UnityEngine.UIElements;

public class DebuggerObjectListView<T, V> where T : TemplateContainer, IDebuggerListItem<V>, new()
{
    private ListView _list;

    public DebuggerObjectListView(ListView list)
    {
        this._list = list;
        _list.makeItem = _MakeListItem;
        _list.bindItem = _BindListItem;
    }
    private List<V> _listData;
    public void SetData(List<V> listData)
    {
        _listData = listData;
        _list.Clear();
        _list.ClearSelection();
        _list.itemsSource = listData;
        _list.RefreshItems();
    }
    VisualElement _MakeListItem()
    {
        return new T();
    }
    void _BindListItem(VisualElement element, int index)
    {
        var data = _listData[index];
        (element as IDebuggerListItem<V>).SetData(data);
    }
}