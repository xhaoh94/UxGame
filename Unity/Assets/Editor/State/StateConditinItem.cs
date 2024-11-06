using System;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.State
{
    public partial class StateConditinItem : TemplateContainer
    {
        Action<StateConditionBase.ConditionType> _changed;
        public StateConditinItem(Action<StateConditionBase.ConditionType> changed)
        {
            CreateChildren();
            Add(root);      
            conditionType.Init(StateConditionBase.ConditionType.State);
            _changed = changed;
        }

        partial void _OnConditionTypeChanged(ChangeEvent<Enum> e)
        {
            _changed?.Invoke((StateConditionBase.ConditionType)e.newValue);
        }          
        public void SetConditionType(StateConditionBase.ConditionType type)
        {                
            conditionType.SetValueWithoutNotify(type);
        }

    }

}
