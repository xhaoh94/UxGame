using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.State.Item
{
    public class StateItemBase<T> : TemplateContainer where T : StateConditionBase
    {
        protected T ConditionData { get; private set; }
        public StateItemBase(StateConditionBase data)
        {
            ConditionData = (T)data;
            OnData();
        }        
        protected virtual void OnData()
        {

        }
    }
    public partial class StateItem : StateItemBase<StateCondition>
    {
        public StateItem(StateConditionBase data):base(data)
        {            
            CreateChildren();
            Add(root);

            validType.Init(StateConditionBase.StateType.Any);
            content.style.unityParagraphSpacing = 10;
        }

        partial void _OnValidTypeChanged(ChangeEvent<Enum> e)
        {
            ConditionData.State = (StateConditionBase.StateType)e.newValue;
            Refresh();
        }
        partial void _OnBtnAddClick()
        {
            var element = MakeItem();
            ConditionData.States.Add(string.Empty);
            BindItem(element, content.childCount, string.Empty);
            content.Add(element);
        }
        void Refresh()
        {
            content.style.display = ConditionData.State == StateConditionBase.StateType.Any ?
                DisplayStyle.None : DisplayStyle.Flex;
            btnAdd.style.display = content.style.display;
        }

        protected override void OnData()
        {
            validType.SetValueWithoutNotify(ConditionData.State);
            Refresh();

            content.Clear();
            int i = 0;
            foreach(var item in ConditionData.States)
            {
                var element = MakeItem();
                BindItem(element, i, item);
                content.Add(element);
                i++;
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
                content.RemoveAt(index);
                ConditionData.States.RemoveAt(index);
            };
            var textField = element.Q<TextField>("Label1");
            textField.RegisterValueChangedCallback(evt =>
            {
                ConditionData.States[index] = evt.newValue;
            });
            if (!string.IsNullOrEmpty(state))
            {
                textField.value = state;
            }
        }
    }

}
