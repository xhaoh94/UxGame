using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
public class TempBoolVarItem : StateItemBase
{
    public TempBoolVarItem()
    {
        var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Item/TempBoolVarItem.uxml");
        visualAsset.CloneTree(this);
        style.flexGrow = 1f;
        CreateView();
    }
    TextField _txtVar;
    Toggle _tgVar;
    void CreateView()
    {
        _txtVar = this.Q<TextField>("txtVar");
        _txtVar.RegisterValueChangedCallback(e =>
        {
            data.key = e.newValue;
        });
        _tgVar = this.Q<Toggle>("tgVar");
        _tgVar.RegisterValueChangedCallback(e =>
        {
            data.value = e.newValue;
        });
    }

    StateCondition data;
    public override void SetData(StateCondition data)
    {
        if (data == null) return;
        this.data = data;
        _txtVar.SetValueWithoutNotify(data.key);
        _tgVar.SetValueWithoutNotify(data.value);
    }


}
