using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Ux;
public class CustomItem : StateItemBase
{
    public CustomItem()
    {
        var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/State/Item/CustomItem.uxml");
        visualAsset.CloneTree(this);
        style.flexGrow = 1f;
        CreateView();
    }
    TextField _txtName;
    TextField _txtValue;
    void CreateView()
    {
        _txtName = this.Q<TextField>("txtName");
        _txtName.RegisterValueChangedCallback(e =>
        {
            data.customName = e.newValue;
        });
        _txtValue = this.Q<TextField>("txtValue");
        _txtValue.RegisterValueChangedCallback(e =>
        {
            data.customValue = e.newValue;
        });
    }

    StateSettingData.StateCondition data;
    public override void SetData(StateSettingData.StateCondition data)
    {
        if (data == null) return;
        this.data = data;
        _txtName.SetValueWithoutNotify(data.customName);
        _txtValue.SetValueWithoutNotify(data.customValue);
    }


}
