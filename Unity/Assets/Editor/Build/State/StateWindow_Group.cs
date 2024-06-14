using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Build.State
{
    public partial class StateWindow
    {
        VisualElement _groupContent;
        Button _btnAddGroup;

        void OnCreateGroup()
        {
            VisualElement root = rootVisualElement;
            _btnAddGroup = root.Q<Button>("btnAddGroup");
            _btnAddGroup.clicked += OnBtnAddGroup;

            _groupContent = root.Q<VisualElement>("groupContent");
            _groupContent.style.unityParagraphSpacing = 10;
            RefreshGroup();
        }
        void RefreshGroup()
        {
            _groupContent.Clear();
            for (int i = 0; i < Setting.groups.Count; i++)
            {
                var element = MakeGroupItem();
                BindGroupItem(element, i, Setting.groups[i]);
                _groupContent.Add(element);
            }
        }
        void OnBtnAddGroup()
        {
            var element = MakeGroupItem();
            Setting.groups.Add("Group" + _conditionContent.childCount + 1);
            BindGroupItem(element, _conditionContent.childCount, string.Empty);
            _groupContent.Add(element);
        }

        private VisualElement MakeGroupItem()
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
                    var label = new TextField(null);
                    label.name = "Label1";
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    label.style.flexGrow = 1f;
                    label.style.height = 20f;
                    label.style.minWidth = 100f;
                    element2.Add(label);
                }
                element.Add(element2);
            }

            return element;
        }
        private void BindGroupItem(VisualElement element, int index, string state)
        {

            var btn = element.Q<Button>("Sub");
            btn.clicked += () =>
            {
                _groupContent.RemoveAt(index);
                Setting.groups.RemoveAt(index);
            };
            var textField = element.Q<TextField>("Label1");
            textField.RegisterValueChangedCallback(evt =>
            {
                Setting.groups[index] = evt.newValue;
            });
            if (!string.IsNullOrEmpty(state))
            {
                textField.value = state;
            }
        }
    }
}
