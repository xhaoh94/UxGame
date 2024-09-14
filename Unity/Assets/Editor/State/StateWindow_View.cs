using UnityEngine.UIElements;
namespace Ux.Editor.State
{
    partial class StateWindow
    {
        void OnCreateView()
        {
            viewType.Init(StateViewType.None);
        }
        partial void _OnTxtGroupChanged(ChangeEvent<string> e)
        {
            var newGroup = e.newValue;
            if (newGroup != SelectItem.group)
            {
                if (groupAssets.TryGetValue(SelectItem.group, out var oldAssets))
                {
                    oldAssets.Remove(SelectItem);
                    //if(stateAssets.Count == 0)
                    //{
                    //    groupAssets.Remove(SelectItem.group);
                    //}
                }
                SelectItem.group = newGroup;
                if (!groupAssets.TryGetValue(newGroup, out var newAssets))
                {
                    newAssets = new();
                    groupAssets.Add(newGroup, newAssets);                    
                }
                newAssets.Add(SelectItem);
                RefreshGroup();
                SelectGroup(newGroup);
            }
        }
        partial void _OnTxtPriChanged(ChangeEvent<int> e)
        {
            SelectItem.priority = e.newValue;
        }
        partial void _OnTgMuteChanged(ChangeEvent<bool> e)
        {
            SelectItem.isMute = e.newValue;
        }

        partial void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            SelectItem.stateName = e.newValue;
            OnUpdateListView();
        }
        partial void _OnTxtDescChanged(ChangeEvent<string> e)
        {
            SelectItem.stateDesc = e.newValue;
            OnUpdateListView();
        }


        void RefreshView()
        {
            if (SelectItem == null)
            {
                infoView.style.display = DisplayStyle.None;
                return;
            }
            infoView.style.display = DisplayStyle.Flex;
            txtGroup.SetValueWithoutNotify(SelectItem.group);
            txtPri.SetValueWithoutNotify(SelectItem.priority);
            tgMute.SetValueWithoutNotify(SelectItem.isMute);
            txtName.SetValueWithoutNotify(SelectItem.stateName);
            txtDesc.SetValueWithoutNotify(SelectItem.stateDesc);
            RefreshCondition();
        }
    }

}
