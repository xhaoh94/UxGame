using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.State
{
    partial class StateWindow
    {
        bool isAddGroup;
        private int _lastModifyExportIndex = 0;
        List<StateAsset> assets = new();
        PopupField<string> popupGroup;
        void OnCreateListView()
        {
            _lastModifyExportIndex = 0;
            _OnBtnStateCancelClick();

            popupGroup = new PopupField<string>();
            popupGroup.label = "所属组";
            popupGroup.labelElement.style.minWidth = 50;
            popupGroup.style.flexGrow = 1;
            txtStateGroup.labelElement.style.minWidth = 50;
            txtStateCreateName.labelElement.style.minWidth = 50;
            txtStateGroup.parent.Add(popupGroup);
            OnUpdateListView();
        }

        partial void _OnInputSearchChanged(ChangeEvent<string> e)
        {
            OnUpdateListView();
        }
        partial void _OnBtnSearchClearClick()
        {
            inputSearch.SetValueWithoutNotify(string.Empty);
            OnUpdateListView();
        }
        partial void _OnBtnAddClick()
        {
            isAddGroup = true;
            veStateCreate.style.display = DisplayStyle.Flex;
            popupGroup.choices = groupAssets.Keys.ToList();
            if (popupGroup.choices.Count > 0)
            {
                popupGroup.value = popupGroup.choices[0];
                popupGroup.style.display = DisplayStyle.None;
                btnStateAdd.style.display = DisplayStyle.Flex;
            }
            else
            {
                popupGroup.style.display = DisplayStyle.Flex;
                btnStateAdd.style.display = DisplayStyle.None;
            }
            _OnBtnStateAddClick();
        }
        partial void _OnBtnStateAddClick()
        {
            if (isAddGroup)
            {
                txtStateCreateName.isReadOnly = false;
                btnStateCreate.text = "创建";
            }
            else
            {
                txtStateCreateName.SetValueWithoutNotify(StateWindow.Asset.stateName);
                txtStateCreateName.isReadOnly = true;
                btnStateCreate.text = "更改";
            }
            if (popupGroup.style.display == DisplayStyle.None)
            {
                popupGroup.style.display = DisplayStyle.Flex;
                txtStateGroup.style.display = DisplayStyle.None;
                btnStateAdd.text = "+";
            }
            else
            {
                popupGroup.style.display = DisplayStyle.None;
                txtStateGroup.style.display = DisplayStyle.Flex;
                btnStateAdd.text = "←";
            }
        }
        partial void _OnBtnStateCreateClick()
        {
            string newGroup = txtStateGroup.text;
            if (popupGroup.style.display == DisplayStyle.Flex)
            {
                newGroup = popupGroup.value;
            }
            if (isAddGroup)
            {
                var newAsset = ScriptableObject.CreateInstance<StateAsset>();
                newAsset.group = newGroup;
                newAsset.stateName = txtStateCreateName.text;
                var dir = $"{AssetPath}/{newAsset.group}";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                AssetDatabase.CreateAsset(newAsset, $"{dir}/{newAsset.stateName}.asset");
                if (!groupAssets.TryGetValue(newAsset.group, out var temList))
                {
                    temList = new List<StateAsset>();
                    groupAssets.Add(newAsset.group, temList);
                }
                temList.Add(newAsset);
                SelectGroup(newGroup);
                veStateCreate.style.display = DisplayStyle.None;
            }
            else
            {
                _OnTxtGroupChanged(newGroup);
                veStateCreate.style.display = DisplayStyle.None;
            }
        }

        partial void _OnBtnStateCancelClick()
        {
            veStateCreate.style.display = DisplayStyle.None;
        }

        partial void _OnBtnRemoveClick()
        {
            if (StateWindow.Asset == null)
            {
                return;
            }
            if (groupAssets.TryGetValue(StateWindow.Asset.group, out var stateAssets))
            {
                stateAssets.Remove(StateWindow.Asset);
            }
            OnUpdateListView();
        }
        partial void _OnMakeListViewItem(VisualElement element)
        {
            var label = new Label();
            label.name = "Label1";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.flexGrow = 1f;
            label.style.height = 20f;
            element.Add(label);
        }
        partial void _OnBindListViewItem(VisualElement e, int index)
        {
            var stateAsset = assets[index];
            var textField1 = e.Q<Label>("Label1");
            textField1.text = stateAsset.stateName;
            if (!string.IsNullOrEmpty(stateAsset.stateDesc))
            {
                if (string.IsNullOrEmpty(stateAsset.stateName))
                {
                    textField1.text = stateAsset.stateDesc;
                }
                else
                {
                    textField1.text += "@" + stateAsset.stateDesc;
                }
            }
        }
        partial void _OnListViewItemClick(IEnumerable<object> objs)
        {
            if (listView.selectedIndex < 0)
            {
                return;
            }
            _lastModifyExportIndex = listView.selectedIndex;
            StateWindow.Asset = listView.selectedItem as StateAsset;
            RefreshView();
        }


        private void OnUpdateListView()
        {
            listView.Clear();
            listView.ClearSelection();
            assets.Clear();
            foreach (var group in showGroups)
            {
                if (groupAssets.TryGetValue(group, out var stateAssets))
                {
                    if (!string.IsNullOrEmpty(inputSearch.text))
                    {
                        foreach (var asset in stateAssets)
                        {
                            if (asset.stateName.Contains(inputSearch.text))
                            {
                                assets.Add(asset);
                            }
                        }
                    }
                    else
                    {
                        assets.AddRange(stateAssets);
                    }
                }
            }

            listView.itemsSource = assets;
            listView.Rebuild();
            if (assets.Count > 0)
            {
                if (_lastModifyExportIndex >= 0)
                {
                    if (_lastModifyExportIndex >= listView.itemsSource.Count)
                    {
                        _lastModifyExportIndex = 0;
                    }
                    listView.selectedIndex = _lastModifyExportIndex;
                }
            }
            else
            {
                RefreshView();
            }
        }
    }

}
