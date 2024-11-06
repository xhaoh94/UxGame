using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.State
{
    partial class StateWindow
    {
        partial void _OnBtnChangeGroupClick()
        {
            isAddGroup = false;
            veStateCreate.style.display = DisplayStyle.Flex;
            popupGroup.choices = groupAssets.Keys.ToList();
            popupGroup.value = txtGroup.text;
            popupGroup.style.display = DisplayStyle.None;
            btnStateAdd.style.display = DisplayStyle.Flex;            
            _OnBtnStateAddClick();
        }
        void _OnTxtGroupChanged(string newGroup)
        {            
            if (newGroup != StateWindow.Asset.group)
            {
                txtGroup.SetValueWithoutNotify(newGroup);
                if (groupAssets.TryGetValue(StateWindow.Asset.group, out var oldAssets))
                {
                    oldAssets.Remove(StateWindow.Asset);
                    if (oldAssets.Count == 0)
                    {
                        groupAssets.Remove(StateWindow.Asset.group);
                        UnSelectGroup(StateWindow.Asset.group);
                    }
                }
                var oldPath = $"{AssetPath}/{StateWindow.Asset.group}/{StateWindow.Asset.name}.asset";
                var dir = $"{AssetPath}/{newGroup}";
                var newPath = $"{dir}/{StateWindow.Asset.name}.asset";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                AssetDatabase.MoveAsset(oldPath, newPath);
                StateWindow.Asset.group = newGroup;
                if (!groupAssets.TryGetValue(newGroup, out var newAssets))
                {
                    newAssets = new();
                    groupAssets.Add(newGroup, newAssets);                    
                }
                newAssets.Add(StateWindow.Asset);
                RefreshGroup();
                SelectGroup(newGroup);
            }
        }
        partial void _OnTxtPriChanged(ChangeEvent<int> e)
        {
            StateWindow.Asset.priority = e.newValue;
        }
        partial void _OnTgMuteChanged(ChangeEvent<bool> e)
        {
            StateWindow.Asset.isMute = e.newValue;
        }

        partial void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            StateWindow.Asset.stateName = e.newValue;
            OnUpdateListView();
        }
        partial void _OnTxtDescChanged(ChangeEvent<string> e)
        {
            StateWindow.Asset.stateDesc = e.newValue;
            OnUpdateListView();
        }


        void RefreshView()
        {
            if (StateWindow.Asset == null)
            {
                infoView.style.display = DisplayStyle.None;
                return;
            }
            infoView.style.display = DisplayStyle.Flex;
            txtGroup.SetValueWithoutNotify(StateWindow.Asset.group);
            txtPri.SetValueWithoutNotify(StateWindow.Asset.priority);
            tgMute.SetValueWithoutNotify(StateWindow.Asset.isMute);
            txtName.SetValueWithoutNotify(StateWindow.Asset.stateName);
            txtDesc.SetValueWithoutNotify(StateWindow.Asset.stateDesc);
            RefreshCondition();
        }
    }

}
