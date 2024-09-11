using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.Build.State
{
    public class ActionInputItem : StateItemBase
    {
        public ActionInputItem()
        {
            var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/State/Item/ActionInputItem.uxml");
            visualAsset.CloneTree(this);
            style.flexGrow = 1f;
            CreateView();
        }
        EnumField _enumKey;
        EnumField _enumType;
        void CreateView()
        {
            _enumKey = this.Q<EnumField>("enumKey");
            _enumKey.Init(StateConditionBase.InputType.Attack);
            _enumKey.RegisterValueChangedCallback(e =>
            {
                data.inputType = (StateConditionBase.InputType)e.newValue;
            });
            _enumType = this.Q<EnumField>("enumType");
            _enumType.Init(StateConditionBase.TriggerType.Down);
            _enumType.RegisterValueChangedCallback(e =>
            {
                data.triggerType = (StateConditionBase.TriggerType)e.newValue;
            });
        }

        StateSettingData.StateCondition data;
        public override void SetData(StateSettingData.StateCondition data)
        {
            if (data == null) return;
            this.data = data;
            _enumKey.SetValueWithoutNotify(data.inputType);
            _enumType.SetValueWithoutNotify(data.triggerType);
        }


    }

}
