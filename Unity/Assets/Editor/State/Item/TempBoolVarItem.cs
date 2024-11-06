using UnityEngine.UIElements;
namespace Ux.Editor.State.Item
{
    public partial class TempBoolVarItem : StateItemBase<TemBoolVarCondition>
    {
        public TempBoolVarItem()
        {
            CreateChildren();
            Add(root);
        }

        partial void _OnTxtVarChanged(ChangeEvent<string> e)
        {
            ConditionData.Key = e.newValue;
        }

        protected override void OnData()
        {
            txtVar.SetValueWithoutNotify(ConditionData.Key);
        }

    }

}
