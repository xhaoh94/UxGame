using System;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
namespace Ux.Editor.State.Item
{
    public partial class ActionKeyboardItem : StateItemBase<ActionKeyboardCondition>
    {
        public ActionKeyboardItem()
        {
            CreateChildren();
            Add(root);            
            CreateView();
        }
        void CreateView()
        {

            enumKey.Init(Key.Enter);
            enumType.Init(StateConditionBase.TriggerType.Down);            
        }
        partial void _OnEnumKeyChanged(ChangeEvent<Enum> e)
        {
            ConditionData.Key = (Key)e.newValue;
        }
        partial void _OnEnumTypeChanged(ChangeEvent<Enum> e)
        {
            ConditionData.Trigger = (StateConditionBase.TriggerType)e.newValue;
        }

        protected override void OnData()
        {
            enumKey.SetValueWithoutNotify(ConditionData.Key);
            enumType.SetValueWithoutNotify(ConditionData.Trigger);
        }


    }

}
