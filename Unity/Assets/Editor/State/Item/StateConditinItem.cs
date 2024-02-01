using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;

public class StateConditinItem : TemplateContainer
{    
    EnumField _conditionType;
    VisualElement _content;
    public StateConditinItem()
    {   
        var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Item/StateConditinItem.uxml");
        visualAsset.CloneTree(this);

        _conditionType = this.Q<EnumField>("conditionType");
        _conditionType.Init(StateEnterConditionType.State);
        _conditionType.RegisterValueChangedCallback(evt =>
        {
            enterCondition.Type = (StateEnterConditionType)evt.newValue;
            cb?.Invoke();
        });
        _content = this.Q<VisualElement>("content");

    }
    System.Action cb;
    StateCondition enterCondition;
    public void SetData(StateCondition enterCondition, System.Action cb)
    {        
        this.cb = cb;
        this.enterCondition = enterCondition;
        _conditionType.SetValueWithoutNotify(enterCondition.Type);
    }

}
