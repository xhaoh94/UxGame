using System;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.State.Item
{
    public partial class ActionInputItem : StateItemBase<ActionInputCondition>
    {
        public ActionInputItem()
        {
            CreateChildren();
            Add(root);
            CreateView();
        }

        void CreateView()
        {            
            enumKey.Init(StateConditionBase.InputType.Attack);            
            enumType.Init(StateConditionBase.TriggerType.Down);
        }

        partial void _OnEnumKeyChanged(ChangeEvent<Enum> e)
        {
            ConditionData.Input = (StateConditionBase.InputType)e.newValue;
        }
        partial void _OnEnumTypeChanged(ChangeEvent<Enum> e)
        {
            ConditionData.Trigger = (StateConditionBase.TriggerType)e.newValue;
        }        
        protected override void OnData()
        {
            enumKey.SetValueWithoutNotify(ConditionData.Input);
            enumType.SetValueWithoutNotify(ConditionData.Trigger);
        }
    }

}
