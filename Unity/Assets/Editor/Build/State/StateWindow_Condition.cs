using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Ux;

public partial class StateWindow
{

    Button _btnAddCondition;
    VisualElement _conditionContent;

    void OnCreateCondition()
    {
        VisualElement root = rootVisualElement;
        _btnAddCondition = root.Q<Button>("btnAddCondition");
        _btnAddCondition.clicked += OnBtnAddState;

        _conditionContent = root.Q<VisualElement>("conditionContent");
    }
    void RefreshCondition()
    {
        _conditionContent.Clear();
        for (int i = 0; i < SelectItem.Conditions.Count; i++)
        {
            var element = MakeConditionItem();
            BindConditionItem(element, i, SelectItem.Conditions[i]);
            _conditionContent.Add(element);
        }
    }

    private StateConditionContent MakeConditionItem()
    {
        var element = new StateConditionContent();
        return element;
    }
    private void BindConditionItem(StateConditionContent element, int index, StateSettingData.StateCondition condition)
    {
        var btn = element.Q<Button>("Sub");
        btn.clicked += () =>
        {
            _conditionContent.RemoveAt(index);
            SelectItem.Conditions.RemoveAt(index);
        };
        element.SetData(condition);
    }
    void OnBtnAddState()
    {
        var element = MakeConditionItem();
        var data = new StateSettingData.StateCondition();
        SelectItem.Conditions.Add(data);
        BindConditionItem(element, _conditionContent.childCount, data);
        _conditionContent.Add(element);
    }
}