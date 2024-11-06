using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.State
{
    public partial class StateWindow
    {
        Dictionary<string, List<StateAsset>> groupAssets = new();
        Dictionary<string, VisualElement> groupElement = new();
        HashSet<string> showGroups = new();

        partial void _OnBtnAllSelectClick()
        {
            foreach(var group in groupAssets.Keys)
            {
                showGroups.Add(group);
                if(groupElement.TryGetValue(group,out var element))
                {
                    var tg = element.Q<Toggle>("Tg");
                    tg.SetValueWithoutNotify(true);
                }
            }
            OnUpdateListView();
        }
        partial void _OnBtnNoSelectClick()
        {
            showGroups.Clear();            
            foreach (var (_, element) in groupElement)
            {
                var tg = element.Q<Toggle>("Tg");
                tg.SetValueWithoutNotify(false);
            }
            OnUpdateListView();
        }
        void OnCreateGroup()
        {
            groupContent.style.unityParagraphSpacing = 10;
            RefreshGroup();
        }

        void RefreshGroup()
        {
            groupContent.Clear();
            groupElement.Clear();
            foreach (var (group, listData) in groupAssets)
            {
                var element = MakeGroupItem();
                BindGroupItem(element, group, listData);
                groupContent.Add(element);
            }
        }
        void SelectGroup(string group)
        {
            showGroups.Add(group);
            OnUpdateListView();
        }

        void UnSelectGroup(string group)
        {
            showGroups.Remove(group);
        }
        private VisualElement MakeGroupItem()
        {
            VisualElement element = new VisualElement();
            {
                element.style.alignItems = Align.Center;
                element.style.flexDirection = FlexDirection.Row;
                var tg = new Toggle();
                tg.name = "Tg";       
                tg.style.width = 20f;
                tg.style.height = 20;
                element.Add(tg);
                //var btn = new Button();
                //btn.name = "Sub";
                //btn.text = "[-]";
                //btn.style.width = 30f;
                //btn.style.height = 20;
                //element.Add(btn);
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
            groupElement.Add(group,element);
            var tg = element.Q<Toggle>("Tg");
            tg.SetValueWithoutNotify(showGroups.Contains(group));
            tg.RegisterValueChangedCallback(e =>
            {
                if (e.newValue)
                {
                    showGroups.Add(group);
                }
                else
                {
                    showGroups.Remove(group);
                }
                OnUpdateListView();
            });
            var textField = element.Q<TextField>("Label1");
            textField.RegisterValueChangedCallback(e =>
            {
                foreach (var item in listData)
                {
                    item.group = e.newValue;
                }
                groupElement.Remove(group);
                groupElement.Add(e.newValue, element);
            });
            if (!string.IsNullOrEmpty(group))
            {
                textField.value = group;
            }
        }
    }
}
