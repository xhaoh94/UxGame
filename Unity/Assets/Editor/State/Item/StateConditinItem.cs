using System;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.State.Item
{
    public partial class StateConditinItem : TemplateContainer
    {
        public StateConditinItem()
        {
            CreateChildren();
            Add(root);      
            conditionType.Init(StateConditionBase.ConditionType.State);            
        }

        partial void _OnConditionTypeChanged(ChangeEvent<Enum> e)
        {
            //data.Condition = (StateConditionBase.ConditionType)e.newValue;
            cb?.Invoke();
        }
        System.Action cb;
        StateConditionBase data;
        public void SetData(StateConditionBase data, System.Action cb)
        {
            this.cb = cb;
            this.data = data;
            conditionType.SetValueWithoutNotify(data.Condition);
        }

    }

}
