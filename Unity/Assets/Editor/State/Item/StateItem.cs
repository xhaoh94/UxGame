using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
public class StateConditionContent : TemplateContainer
{
    StateEnterCondition stateEnter;
    StateItemBase item;
    StateConditinItem content;
    public StateConditionContent()
    {
        VisualElement element = new VisualElement();
        {
            element.style.alignItems = Align.Center;
            element.style.flexDirection = FlexDirection.Row;
            var btn = new Button();
            btn.name = "Sub";
            btn.text = "[-]";
            btn.style.width = 30f;
            btn.style.height = 20;
            element.Add(btn);
            content = new StateConditinItem();
            {
                content.name = "Content";
                content.style.flexGrow = 1f;
            }
            element.Add(content);
        }
        Add(element);
    }
    public void SetData(StateEnterCondition enterCondition)
    {
        content.SetData(enterCondition, UpdateView);
        stateEnter = enterCondition;
        UpdateView();
    }
    public void UpdateView()
    {
        if (item != null)
        {
            content.Remove(item);
            item = null;
        }
        switch (stateEnter.Type)
        {
            case StateEnterConditionType.State:
                item = new StateItem();                
                content.Add(item);
                break;
        }        
        item?.SetData(stateEnter.ItemData);
    }
}
public class StateItemBase : TemplateContainer
{
    public virtual void SetData(StateItemData data) { }
}
public class StateItem : StateItemBase
{
    public StateItem()
    {
        var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Item/StateItem.uxml");
        visualAsset.CloneTree(this);
        style.flexGrow = 1f;
        CreateView();
    }
    VisualElement _content;
    EnumField _validType;
    void CreateView()
    {

        _validType = this.Q<EnumField>("validType");

        _content = this.Q<VisualElement>("content");
        _content.style.unityParagraphSpacing = 10;

        var btnAdd = this.Q<Button>("btnAdd");
        btnAdd.clicked += OnBtnAdd;


    }

    void OnBtnAdd()
    {
        var element = MakeItem();
        this.data.states.Add(string.Empty);
        BindItem(element, _content.childCount, string.Empty);
        _content.Add(element);
    }
    StateItemData data;
    public override void SetData(StateItemData data)
    {
        if (data == null) return;
        this.data = data;
        _content.Clear();
        for (int i = 0; i < this.data.states.Count; i++)
        {
            var element = MakeItem();
            BindItem(element, i, this.data.states[i]);
            _content.Add(element);
        }
    }

    private VisualElement MakeItem()
    {
        VisualElement element = new VisualElement();
        {
            element.style.alignItems = Align.Center;
            element.style.flexDirection = FlexDirection.Row;
            var btn = new Button();
            btn.name = "Sub";
            btn.text = "[-]";
            btn.style.width = 30f;
            btn.style.height = 20;
            element.Add(btn);
            VisualElement element2 = new VisualElement();
            {
                element2.style.flexGrow = 1f;
                var label = new TextField("状态");
                label.name = "Label1";
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.flexGrow = 1f;
                label.style.height = 20f;
                element2.Add(label);
            }
            element.Add(element2);
        }

        return element;
    }
    private void BindItem(VisualElement element, int index, string state)
    {

        var btn = element.Q<Button>("Sub");
        btn.clicked += () =>
        {
            _content.RemoveAt(index);
            data.states.RemoveAt(index);
        };
        var textField = element.Q<TextField>("Label1");
        textField.RegisterValueChangedCallback(evt =>
        {            
            data.states[index] = evt.newValue;
        });
        if (!string.IsNullOrEmpty(state))
        {
            textField.value = state;
        }
    }
}
