using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.Build.State
{
    public class StateConditinItem : TemplateContainer
    {
        EnumField _conditionType;
        VisualElement _content;
        public StateConditinItem()
        {
            var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/State/Item/StateConditinItem.uxml");
            visualAsset.CloneTree(this);

            _conditionType = this.Q<EnumField>("conditionType");
            _conditionType.Init(StateConditionBase.ConditionType.State);
            _conditionType.RegisterValueChangedCallback(evt =>
            {
                data.Type = (StateConditionBase.ConditionType)evt.newValue;
                cb?.Invoke();
            });
            _content = this.Q<VisualElement>("content");

        }
        System.Action cb;
        StateSettingData.StateCondition data;
        public void SetData(StateSettingData.StateCondition data, System.Action cb)
        {
            this.cb = cb;
            this.data = data;
            _conditionType.SetValueWithoutNotify(data.Type);
        }

    }

}
