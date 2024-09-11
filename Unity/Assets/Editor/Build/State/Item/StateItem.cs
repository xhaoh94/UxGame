using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Build.State
{
    public class StateItemBase : TemplateContainer
    {
        public virtual void SetData(StateSettingData.StateCondition data) { }
    }
    public class StateItem : StateItemBase
    {
        public StateItem()
        {
            var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/State/Item/StateItem.uxml");
            visualAsset.CloneTree(this);
            style.flexGrow = 1f;
            CreateView();
        }
        VisualElement _content;
        EnumField _validType;
        Button btnAdd;
        void CreateView()
        {

            _validType = this.Q<EnumField>("validType");
            _validType.Init(StateConditionBase.StateType.Any);
            _validType.RegisterValueChangedCallback(e =>
            {
                data.stateType = (StateConditionBase.StateType)e.newValue;
                Refresh();
            });

            _content = this.Q<VisualElement>("content");
            _content.style.unityParagraphSpacing = 10;

            btnAdd = this.Q<Button>("btnAdd");
            btnAdd.clicked += OnBtnAdd;

        }
        void Refresh()
        {
            _content.style.display = data.stateType == StateConditionBase.StateType.Any ?
                DisplayStyle.None : DisplayStyle.Flex;
            btnAdd.style.display = _content.style.display;
        }

        void OnBtnAdd()
        {
            var element = MakeItem();
            this.data.states.Add(string.Empty);
            BindItem(element, _content.childCount, string.Empty);
            _content.Add(element);
        }
        StateSettingData.StateCondition data;
        public override void SetData(StateSettingData.StateCondition data)
        {
            if (data == null) return;
            this.data = data;
            _validType.SetValueWithoutNotify(data.stateType);
            Refresh();

            _content.Clear();
            for (int i = 0; i < this.data.states.Count; i++)
            {
                var element = MakeItem();
                BindItem(element, i, this.data.states[i]);
                _content.Add(element);
            }
        }

        private VisualElement MakeItem()
        {
            VisualElement element = new VisualElement();
            {
                element.style.alignItems = Align.Center;
                element.style.flexDirection = FlexDirection.Row;
                var btn = new Button();
                btn.name = "Sub";
                btn.text = "[-]";
                btn.style.width = 30f;
                btn.style.height = 20;
                element.Add(btn);
                VisualElement element2 = new VisualElement();
                {
                    element2.style.flexGrow = 1f;
                    var label = new TextField("状态");
                    label.name = "Label1";
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    label.style.flexGrow = 1f;
                    label.style.height = 20f;
                    element2.Add(label);
                }
                element.Add(element2);
            }

            return element;
        }
        private void BindItem(VisualElement element, int index, string state)
        {

            var btn = element.Q<Button>("Sub");
            btn.clicked += () =>
            {
                _content.RemoveAt(index);
                data.states.RemoveAt(index);
            };
            var textField = element.Q<TextField>("Label1");
            textField.RegisterValueChangedCallback(evt =>
            {
                data.states[index] = evt.newValue;
            });
            if (!string.IsNullOrEmpty(state))
            {
                textField.value = state;
            }
        }
    }

}
