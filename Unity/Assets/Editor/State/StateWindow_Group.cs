using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.State
{
    public partial class StateWindow
    {
        string selectGroup;
        void OnCreateGroup()
        {            
            groupContent.style.unityParagraphSpacing = 10;
            RefreshGroup();
        }
        partial void _OnBtnAddGroupClick()
        {
            //var element = MakeGroupItem();
            //Setting.groups.Add("Group" + conditionContent.childCount + 1);
            //BindGroupItem(element, conditionContent.childCount, string.Empty);
            //groupContent.Add(element);
        }
        void RefreshGroup()
        {
            groupContent.Clear();
            foreach (var (group,listData) in groupAssets)
            {
                var element = MakeGroupItem();
                BindGroupItem(element, group, listData);
                groupContent.Add(element);
            }
        }
        void SelectGroup(string group)
        {
            selectGroup = group;
            OnUpdateListView();
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
        private void BindGroupItem(VisualElement element, string group, List<StateAsset> listData)
        {            
            var btn = element.Q<Button>("Sub");
            btn.clicked += () =>
            {

            };
            var textField = element.Q<TextField>("Label1");
            textField.RegisterValueChangedCallback(e =>
            {
                foreach (var item in listData)
                {
                    item.group = e.newValue;
                }                
            });
            if (!string.IsNullOrEmpty(group))
            {
                textField.value = group;
            }
        }
    }
}
