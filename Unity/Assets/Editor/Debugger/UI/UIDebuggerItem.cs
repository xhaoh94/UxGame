using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.UI
{
    public partial class UIDebuggerItem : TemplateContainer, IDebuggerListItem<IUIData>
    {
        protected VisualElement root;
        public Label txtIDStr;
        public TextField txtID;
        public TextField txtType;
        public TextField txtPkgs;
        public TextField txtTags;
        public TextField txtChildrens;
        public TextField txtParID;
        public TextField txtParRedPoint;
        public TextField txtParTitle;

        public UIDebuggerItem()
        {
            BuildUI();
            Add(root);
        }

        /// <summary>原 UIDebuggerItem.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1f;
            root.style.flexShrink = 1f;
            root.SetBorder(Color.black);
            root.SetMargin(1f);

            txtIDStr = DebuggerUiUtil.IdLabel("txtIDStr");
            root.Add(DebuggerUiUtil.IdBox(txtIDStr));

            var content = new VisualElement();
            content.style.flexGrow = 1f;
            root.Add(content);

            txtID = DebuggerUiUtil.LockedField("txtID", "ID");
            txtID.style.display = DisplayStyle.None;
            txtID.style.visibility = Visibility.Hidden;
            content.Add(txtID);

            txtType = DebuggerUiUtil.LockedField("txtType", "注册类");
            content.Add(txtType);
            txtPkgs = DebuggerUiUtil.LockedField("txtPkgs", "依赖资源包");
            content.Add(txtPkgs);
            txtTags = DebuggerUiUtil.LockedField("txtTags", "懒加载标签");
            content.Add(txtTags);
            txtChildrens = DebuggerUiUtil.LockedField("txtChildrens", "子界面");
            content.Add(txtChildrens);
            txtParID = DebuggerUiUtil.LockedField("txtParID", "父界面");
            content.Add(txtParID);
            txtParRedPoint = DebuggerUiUtil.LockedField("txtParRedPoint", "红点ID");
            content.Add(txtParRedPoint);
            txtParTitle = DebuggerUiUtil.LockedField("txtParTitle", "界面标题");
            content.Add(txtParTitle);
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