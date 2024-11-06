using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.State.Item
{
    public partial class CustomItem : StateItemBase<CustomCondition>
    {
        public CustomItem()
        {
            CreateChildren();
            Add(root);            
        }

        partial void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            //ConditionData.customName = e.newValue;
        }
        partial void _OnTxtValueChanged(ChangeEvent<string> e)
        {
            //ConditionData.customValue = e.newValue;
        }


        protected override void OnData()
        {
            //txtName.SetValueWithoutNotify(ConditionData);
            //txtValue.SetValueWithoutNotify(ConditionData.customValue);
        }

    }

}
