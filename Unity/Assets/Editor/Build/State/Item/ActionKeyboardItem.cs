using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
namespace Ux.Editor.Build.State
{
    public class ActionKeyboardItem : StateItemBase
    {
        public ActionKeyboardItem()
        {
            var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/State/Item/ActionKeyboardItem.uxml");
            visualAsset.CloneTree(this);
            style.flexGrow = 1f;
            CreateView();
        }
        EnumField _enumKey;
        EnumField _enumType;
        void CreateView()
        {
            _enumKey = this.Q<EnumField>("enumKey");
            _enumKey.Init(Key.Enter);
            _enumKey.RegisterValueChangedCallback(e =>
            {
                data.keyType = (Key)e.newValue;
            });
            _enumType = this.Q<EnumField>("enumType");
            _enumType.Init(StateConditionBase.Trigger.Down);
            _enumType.RegisterValueChangedCallback(e =>
            {
                data.triggerType = (StateConditionBase.Trigger)e.newValue;
            });
        }

        StateSettingData.StateCondition data;
        public override void SetData(StateSettingData.StateCondition data)
        {
            if (data == null) return;
            this.data = data;
            _enumKey.SetValueWithoutNotify(data.keyType);
            _enumType.SetValueWithoutNotify(data.triggerType);
        }


    }

}
