using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
public class TempBoolVarItem : StateItemBase
{
    public TempBoolVarItem()
    {
        var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/State/Item/TempBoolVarItem.uxml");
        visualAsset.CloneTree(this);
        style.flexGrow = 1f;
        CreateView();
    }
    TextField _txtVar;    
    void CreateView()
    {
        _txtVar = this.Q<TextField>("txtVar");
        _txtVar.RegisterValueChangedCallback(e =>
        {
            data.key = e.newValue;
        });
    }

    StateSettingData.StateCondition data;
    public override void SetData(StateSettingData.StateCondition data)
    {
        if (data == null) return;
        this.data = data;
        _txtVar.SetValueWithoutNotify(data.key);        
    }


}
