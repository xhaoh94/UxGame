using System;
using UnityEditor;
using UnityEngine.UIElements;
using Ux.Editor.State.Item;
namespace Ux.Editor.State
{
    public partial class StateWindow
    {
        void OnCreateCondition()
        {
            _OnBtnConditionCancelClick();
        }

        partial void _OnBtnAddConditionClick()
        {
            enumCondition.Init(StateConditionBase.ConditionType.State);
            veConditionCreate.style.display = DisplayStyle.Flex;
        }
        partial void _OnBtnConditionCreateClick()
        {
            veConditionCreate.style.display = DisplayStyle.None;
            var conditionType = (StateConditionBase.ConditionType)enumCondition.value;
            var addCondition = StateWindow.Asset.conditions.Find(x => x.Condition == conditionType);
            if (addCondition == null)
            {
                addCondition = CreateCondition(conditionType);
                StateWindow.Asset.conditions.Add(addCondition);
            }
            else
            {
                if (addCondition.isMute == false)
                {
                    Log.Error("同一状态机重复创建相同条件");
                    return;
                }
                StateWindow.SetMute(addCondition, false);                
            }
            var element = MakeConditionItem();
            BindConditionItem(element, addCondition);
            conditionContent.Add(element);
        }
        partial void _OnBtnConditionCancelClick()
        {
            veConditionCreate.style.display = DisplayStyle.None;
        }

        void RefreshCondition()
        {
            conditionContent.Clear();
            for (int i = 0; i < StateWindow.Asset.conditions.Count; i++)
            {
                var condition = StateWindow.Asset.conditions[i];
                if (condition.isMute == false)
                {
                    var element = MakeConditionItem();
                    BindConditionItem(element, StateWindow.Asset.conditions[i]);
                    conditionContent.Add(element);
                }
            }
        }

        private StateConditionContent MakeConditionItem()
        {
            var element = new StateConditionContent();
            return element;
        }
        private void BindConditionItem(StateConditionContent element, StateConditionBase condition)
        {
            var btn = element.Q<Button>("Sub");
            btn.clicked += () =>
            {
                element.SetData(null);
            };
            element.SetData(condition);
        }

    }
}
