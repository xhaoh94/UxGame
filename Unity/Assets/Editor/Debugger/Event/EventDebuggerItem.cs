using UnityEditor;
using UnityEngine.UIElements;

public class EventDebuggerItem : TemplateContainer, IDebuggerListItem<EventList>
{
    private VisualTreeAsset _visualAsset;

    Label _txtID;
    DebuggerStringListView _list;
    public EventDebuggerItem()
    {
        // 加载布局文件		
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
    /// 初始化页面
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
}