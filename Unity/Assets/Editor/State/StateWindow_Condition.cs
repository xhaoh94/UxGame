using System;
using UnityEngine.UIElements;
using Ux.Editor.State.Item;
namespace Ux.Editor.State
{
    public partial class StateWindow
    {
        StateConditionBase CreateCondition(StateConditionBase.ConditionType condition)
        {
            Type type = null;
            switch (condition)
            {
                case StateConditionBase.ConditionType.State:
                    type = typeof(StateCondition);
                    break;
                case StateConditionBase.ConditionType.TempBoolVar:
                    type = typeof(TemBoolVarCondition);
                    break;
                case StateConditionBase.ConditionType.Action_Keyboard:
                    type = typeof(ActionKeyboardCondition);
                    break;
                case StateConditionBase.ConditionType.Action_Input:
                    type = typeof(ActionInputCondition);
                    break;
            }
            if (type == null)
            {
                Log.Error($"没有找到名字为{condition}的条件");
                return null;
            }
            return (StateConditionBase)Activator.CreateInstance(type);
        }

        partial void _OnBtnAddConditionClick()
        {
            enumCondition.Init(StateConditionBase.ConditionType.State);
            veConditionCreate.style.display = DisplayStyle.Flex;
        }
        partial void _OnBtnConditionCreateClick()
        {
            var element = MakeConditionItem();
            var data = CreateCondition((StateConditionBase.ConditionType)enumCondition.value);
            SelectItem.conditions.Add(data);            
            BindConditionItem(element, conditionContent.childCount, data);
            conditionContent.Add(element);
            veConditionCreate.style.display = DisplayStyle.None;
        }
        partial void _OnBtnConditionCancelClick()
        {
            veConditionCreate.style.display = DisplayStyle.None;
        }

        void RefreshCondition()
        {
            conditionContent.Clear();
            for (int i = 0; i < SelectItem.conditions.Count; i++)
            {
                var element = MakeConditionItem();
                BindConditionItem(element, i, SelectItem.conditions[i]);
                conditionContent.Add(element);
            }
        }

        private StateConditionContent MakeConditionItem()
        {
            var element = new StateConditionContent();
            return element;
        }
        private void BindConditionItem(StateConditionContent element, int index, StateConditionBase condition)
        {
            var btn = element.Q<Button>("Sub");
            btn.clicked += () =>
            {
                conditionContent.RemoveAt(index);
                SelectItem.conditions.RemoveAt(index);
            };
            element.SetData(condition);
        }

    }
}
