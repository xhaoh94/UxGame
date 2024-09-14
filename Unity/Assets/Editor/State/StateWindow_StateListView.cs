using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Ux.Editor.State.StateSettingData;
namespace Ux.Editor.State
{
    partial class StateWindow
    {
        private int _lastModifyExportIndex = 0;
        List<StateAsset> assets = new();
        void OnCreateListView()
        {
            _lastModifyExportIndex = 0;
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
            veStateCreate.style.display = DisplayStyle.Flex;            
        }
        partial void _OnBtnStateCreateClick()
        {
            var newAsset = ScriptableObject.CreateInstance<StateAsset>();
            newAsset.group = selectGroup;
            newAsset.name = txtStateCreateName.text;
            AssetDatabase.CreateAsset(newAsset, $"{AssetPath}/{selectGroup}/{newAsset.name}.asset");

            //var item = new StateSettingData.StateData();
            //Setting.StateSettings.Add(item);
            OnUpdateListView();
            veStateCreate.style.display = DisplayStyle.None;
        }

        partial void _OnBtnStateCancelClick()
        {
            veStateCreate.style.display = DisplayStyle.None;
        }
        
        partial void _OnBtnRemoveClick()
        {
            if (SelectItem == null)
            {
                return;
            }
            if (groupAssets.TryGetValue(selectGroup, out var stateAssets))
            {
                stateAssets.Remove(SelectItem);
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
            RefreshView();
        }


        private void OnUpdateListView()
        {
            listView.Clear();
            listView.ClearSelection();
            assets.Clear();
            if (groupAssets.TryGetValue(selectGroup, out var stateAssets))
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
