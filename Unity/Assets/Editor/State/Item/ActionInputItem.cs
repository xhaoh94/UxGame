using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Ux;
public class ActionInputItem : StateItemBase
{
    public ActionInputItem()
    {
        var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Item/ActionInputItem.uxml");
        visualAsset.CloneTree(this);
        style.flexGrow = 1f;
        CreateView();
    }
    EnumField _enumKey;
    EnumField _enumType;
    void CreateView()
    {
        _enumKey = this.Q<EnumField>("enumKey");
        _enumKey.Init(StateCondition.InputType.Attack);
        _enumKey.RegisterValueChangedCallback(e =>
        {
            data.inputType = (StateCondition.InputType)e.newValue;
        });
        _enumType = this.Q<EnumField>("enumType");
        _enumType.Init(StateCondition.TriggerTimeType.Click);
        _enumType.RegisterValueChangedCallback(e =>
        {
            data.triggerType = (StateCondition.TriggerTimeType)e.newValue;
        });
    }

    StateCondition data;
    public override void SetData(StateCondition data)
    {
        if (data == null) return;
        this.data = data;
        _enumKey.SetValueWithoutNotify(data.inputType);
        _enumType.SetValueWithoutNotify(data.triggerType);
    }


}
