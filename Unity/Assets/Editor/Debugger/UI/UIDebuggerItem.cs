using System;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.UI
{
    public partial class UIDebuggerItem : TemplateContainer, IDebuggerListItem<IUIData>
    {
        public UIDebuggerItem()
        {
            CreateChildren();
            Add(root);
        }

        public void SetData(IUIData data)
        {
            if (data == null)
            {
                return;
            }
            var nameParts = data.Name?.Split("_") ?? Array.Empty<string>();
            txtIDStr.text = nameParts.Length > 1 ? $"{nameParts[1]}" : data.Name;
            txtType.SetValueWithoutNotify(data.CType.FullName);


            if (data.Pkgs != null && data.Pkgs.Length > 0)
            {
                txtPkgs.style.display = DisplayStyle.Flex;
                txtPkgs.SetValueWithoutNotify(string.Join(",", data.Pkgs));
            }
            else
            {
                txtPkgs.style.display = DisplayStyle.None;
            }

            if (data.Lazyloads != null && data.Lazyloads.Length > 0)
            {
                txtTags.style.display = DisplayStyle.Flex;
                txtTags.SetValueWithoutNotify(string.Join(",", data.Lazyloads));
            }
            else
            {
                txtTags.style.display = DisplayStyle.None;
            }

            if (data.Children != null && data.Children.Count > 0)
            {
                txtChildrens.style.display = DisplayStyle.Flex;
                txtChildrens.SetValueWithoutNotify(string.Join(",", data.Children));
            }
            else
            {
                txtChildrens.style.display = DisplayStyle.None;
            }

            if (data.TabData != null)
            {
                txtParID.style.display = DisplayStyle.Flex;
                txtParID.SetValueWithoutNotify(data.TabData.PName);
                if (data.TabData.TagType == null)
                {
                    txtParRedPoint.style.display = DisplayStyle.None;
                }
                else
                {
                    txtParRedPoint.style.display = DisplayStyle.Flex;
                    txtParRedPoint.SetValueWithoutNotify(data.TabData.TagType.FullName);
                }

                var title = data.TabData.TitleStr;
                if (string.IsNullOrEmpty(title))
                {
                    txtParTitle.style.display = DisplayStyle.None;
                }
                else
                {
                    txtParTitle.style.display = DisplayStyle.Flex;
                    txtParTitle.SetValueWithoutNotify(title);
                }
            }
            else
            {
                txtParID.style.display = DisplayStyle.None;
                txtParRedPoint.style.display = DisplayStyle.None;
                txtParTitle.style.display = DisplayStyle.None;
            }
        }

        public void SetClickEvt(Action<IUIData> action)
        {

        }
    }
}